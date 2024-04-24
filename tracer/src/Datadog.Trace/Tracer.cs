// <copyright file="Tracer.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.Agent;
using Datadog.Trace.Agent.DiscoveryService;
using Datadog.Trace.ClrProfiler;
using Datadog.Trace.Configuration;
using Datadog.Trace.Debugger.Configurations.Models;
using Datadog.Trace.Debugger.Expressions;
using Datadog.Trace.Debugger.PInvoke;
using Datadog.Trace.Debugger.Snapshots;
using Datadog.Trace.Debugger.TimeTravel;
using Datadog.Trace.Logging;
using Datadog.Trace.Logging.TracerFlare;
using Datadog.Trace.Pdb;
using Datadog.Trace.Sampling;
using Datadog.Trace.SourceGenerators;
using Datadog.Trace.Tagging;
using Datadog.Trace.Telemetry;
using Datadog.Trace.Telemetry.Metrics;
using Datadog.Trace.Util;
using Datadog.Trace.Vendors.dnlib.DotNet;
using Datadog.Trace.Vendors.dnlib.DotNet.Emit;
using Datadog.Trace.Vendors.StatsdClient;

namespace Datadog.Trace
{
    /// <summary>
    /// The tracer is responsible for creating spans and flushing them to the Datadog agent
    /// </summary>
    public class Tracer : ITracer, IDatadogTracer, IDatadogOpenTracingTracer
    {
        private static readonly object GlobalInstanceLock = new();

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<Span>();

        /// <summary>
        /// The number of Tracer instances that have been created and not yet destroyed.
        /// This is used in the heartbeat metrics to estimate the number of
        /// "live" Tracers that could potentially be sending traces to the Agent.
        /// </summary>
        private static int _liveTracerCount;

        private static Tracer _instance;
        private static volatile bool _globalInstanceInitialized;

        private readonly TracerManager _tracerManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tracer"/> class with default settings. Replaces the
        /// settings for all tracers in the application with the default settings.
        /// </summary>
        [Obsolete("This API is deprecated. Use Tracer.Instance to obtain a Tracer instance to create spans.")]
        [PublicApi]
        public Tracer()
        {
            TelemetryFactory.Metrics.Record(PublicApiUsage.Tracer_Ctor);
            // Don't call Configure because it will call Start on the TracerManager
            // before this new instance of Tracer is assigned to Tracer.Instance
            TracerManager.ReplaceGlobalManager(null, TracerManagerFactory.Instance);

            // update the count of Tracer instances
            Interlocked.Increment(ref _liveTracerCount);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tracer"/>
        /// class using the specified <see cref="IConfigurationSource"/>. This constructor updates the global settings
        /// for all <see cref="Tracer"/> instances in the application.
        /// </summary>
        /// <param name="settings">
        /// A <see cref="TracerSettings"/> instance with the desired settings,
        /// or null to use the default configuration sources. This is used to configure global settings
        /// </param>
        [Obsolete("This API is deprecated, as it replaces the global settings for all Tracer instances in the application. " +
                  "If you were using this API to configure the global Tracer.Instance in code, use the static "
                + nameof(Tracer) + "." + nameof(Configure) + "() to replace the global Tracer settings for the application")]
        [PublicApi]
        public Tracer(TracerSettings settings)
        {
            TelemetryFactory.Metrics.Record(PublicApiUsage.Tracer_Ctor_Settings);
            // Don't call Configure because it will call Start on the TracerManager
            // before this new instance of Tracer is assigned to Tracer.Instance
            TracerManager.ReplaceGlobalManager(settings is null ? null : new ImmutableTracerSettings(settings, true), TracerManagerFactory.Instance);

            // update the count of Tracer instances
            Interlocked.Increment(ref _liveTracerCount);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tracer"/> class.
        /// For testing only.
        /// Note that this API does NOT replace the global Tracer instance.
        /// The <see cref="TracerManager"/> created will be scoped specifically to this instance.
        /// </summary>
        internal Tracer(TracerSettings settings, IAgentWriter agentWriter, ITraceSampler sampler, IScopeManager scopeManager, IDogStatsd statsd, ITelemetryController telemetry = null, IDiscoveryService discoveryService = null)
            : this(TracerManagerFactory.Instance.CreateTracerManager(settings is null ? null : new ImmutableTracerSettings(settings, true), agentWriter, sampler, scopeManager, statsd, runtimeMetrics: null, logSubmissionManager: null, telemetry: telemetry ?? NullTelemetryController.Instance, discoveryService ?? NullDiscoveryService.Instance, dataStreamsManager: null, remoteConfigurationManager: null, dynamicConfigurationManager: null, tracerFlareManager: null))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tracer"/> class.
        /// Should only be called DIRECTLY for testing purposes.
        /// If non-null the provided <see cref="TracerManager"/> will be tied to this TracerInstance (for testing purposes only)
        /// If null, the global <see cref="TracerManager"/> will be fetched or created, but will not be modified.
        /// </summary>
        private protected Tracer(TracerManager tracerManager)
        {
            _tracerManager = tracerManager;
            if (tracerManager is null)
            {
                // Ensure the global TracerManager instance has been created
                // to kick start background processes etc
                _ = TracerManager.Instance;
            }

            // update the count of Tracer instances
            Interlocked.Increment(ref _liveTracerCount);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Tracer"/> class.
        /// </summary>
        ~Tracer()
        {
            // update the count of Tracer instances
            Interlocked.Decrement(ref _liveTracerCount);
        }

        /// <summary>
        /// Gets or sets the global <see cref="Tracer"/> instance.
        /// Used by all automatic instrumentation and recommended
        /// as the entry point for manual instrumentation.
        /// </summary>
        public static Tracer Instance
        {
            get
            {
                if (_globalInstanceInitialized)
                {
                    return _instance;
                }

                Tracer instance;
                lock (GlobalInstanceLock)
                {
                    if (_globalInstanceInitialized)
                    {
                        return _instance;
                    }

                    instance = new Tracer(tracerManager: null); // don't replace settings, use existing
                    _instance = instance;
                    _globalInstanceInitialized = true;
                }

                instance.TracerManager.Start();
                return instance;
            }

            // TODO: Make this API internal
            [Obsolete("Use " + nameof(Tracer) + "." + nameof(Configure) + " to configure the global Tracer" +
                      " instance in code.")]
            [PublicApi]
            set
            {
                TelemetryFactory.Metrics.Record(PublicApiUsage.Tracer_Instance_Set);
                if (value is null)
                {
                    ThrowHelper.ThrowArgumentNullException("The tracer instance shouldn't be set to null as this will cause issues with automatic instrumentation.");
                }

                lock (GlobalInstanceLock)
                {
                    // This check is probably no longer necessary, as it's the TracerManager we really care about
                    // Kept for safety reasons
                    if (_instance is { TracerManager: ILockedTracer })
                    {
                        ThrowHelper.ThrowInvalidOperationException("The current tracer instance cannot be replaced.");
                    }

                    _instance = value;
                    _globalInstanceInitialized = true;
                }

                value?.TracerManager.Start();
            }
        }

        /// <summary>
        /// Gets the active scope
        /// </summary>
        public IScope ActiveScope
        {
            get
            {
                return DistributedTracer.Instance.GetActiveScope() ?? InternalActiveScope;
            }
        }

        /// <summary>
        /// Gets the active span context dictionary by consulting DistributedTracer.Instance
        /// </summary>
        internal IReadOnlyDictionary<string, string> DistributedSpanContext => DistributedTracer.Instance.GetSpanContextRaw() ?? InternalActiveScope?.Span?.Context;

        /// <summary>
        /// Gets the active scope
        /// </summary>
        internal Scope InternalActiveScope => TracerManager.ScopeManager.Active;

        /// <summary>
        /// Gets the tracer's scope manager, which determines which span is currently active, if any.
        /// </summary>
        internal IScopeManager ScopeManager => TracerManager.ScopeManager;

        /// <summary>
        /// Gets the default service name for traces where a service name is not specified.
        /// </summary>
        public string DefaultServiceName => TracerManager.DefaultServiceName;

        /// <summary>
        /// Gets the git metadata provider.
        /// </summary>
        IGitMetadataTagsProvider IDatadogTracer.GitMetadataTagsProvider => TracerManager.GitMetadataTagsProvider;

        /// <summary>
        /// Gets this tracer's settings.
        /// </summary>
        public ImmutableTracerSettings Settings => TracerManager.Settings;

        /// <summary>
        /// Gets the tracer's settings for the current trace.
        /// </summary>
        PerTraceSettings IDatadogTracer.PerTraceSettings => TracerManager.PerTraceSettings;

        /// <summary>
        /// Gets the active scope
        /// </summary>
        IScope ITracer.ActiveScope => ActiveScope;

        /// <summary>
        /// Gets this tracer's settings.
        /// </summary>
        ImmutableTracerSettings ITracer.Settings => Settings;

        internal static string RuntimeId => DistributedTracer.Instance.GetRuntimeId();

        internal static int LiveTracerCount => _liveTracerCount;

        internal TracerManager TracerManager => _tracerManager ?? TracerManager.Instance;

        internal PerTraceSettings CurrentTraceSettings
        {
            get
            {
                if (InternalActiveScope?.Span?.Context?.TraceContext is { } context)
                {
                    return context.CurrentTraceSettings;
                }

                return TracerManager.PerTraceSettings;
            }
        }

        /// <summary>
        /// Replaces the global Tracer settings used by all <see cref="Tracer"/> instances,
        /// including automatic instrumentation
        /// </summary>
        /// <param name="settings"> A <see cref="TracerSettings"/> instance with the desired settings,
        /// or null to use the default configuration sources. This is used to configure global settings</param>
        [PublicApi]
        public static void Configure(TracerSettings settings)
        {
            TelemetryFactory.Metrics.Record(PublicApiUsage.Tracer_Configure);
            ConfigureInternal(settings is null ? null : new ImmutableTracerSettings(settings, true));
        }

        internal static void ConfigureInternal(ImmutableTracerSettings settings)
        {
            TracerManager.ReplaceGlobalManager(settings, TracerManagerFactory.Instance);
            Tracer.Instance.TracerManager.Start();
        }

        /// <summary>
        /// Sets the global tracer instance without any validation.
        /// Intended use is for unit testing
        /// </summary>
        /// <param name="instance">Tracer instance</param>
        internal static void UnsafeSetTracerInstance(Tracer instance)
        {
            lock (GlobalInstanceLock)
            {
                _instance = instance;
                _globalInstanceInitialized = true;
            }

            instance?.TracerManager.Start();
        }

        /// <inheritdoc cref="ITracer" />
        [PublicApi]
        IScope ITracer.StartActive(string operationName)
        {
            TelemetryFactory.Metrics.Record(PublicApiUsage.ITracer_StartActive);
            TelemetryFactory.Metrics.RecordCountSpanCreated(MetricTags.IntegrationName.Manual);
            return StartActiveInternal(operationName);
        }

        /// <inheritdoc cref="ITracer" />
        [PublicApi]
        IScope ITracer.StartActive(string operationName, SpanCreationSettings settings)
        {
            TelemetryFactory.Metrics.Record(PublicApiUsage.ITracer_StartActive_Settings);
            TelemetryFactory.Metrics.RecordCountSpanCreated(MetricTags.IntegrationName.Manual);
            var finishOnClose = settings.FinishOnClose ?? true;
            return StartActiveInternal(operationName, settings.Parent, serviceName: null, settings.StartTime, finishOnClose);
        }

        /// <summary>
        /// This creates a new span with the given parameters and makes it active.
        /// </summary>
        /// <param name="operationName">The span's operation name</param>
        /// <returns>A scope wrapping the newly created span</returns>
        [PublicApi]
        public IScope StartActive(string operationName)
        {
            TelemetryFactory.Metrics.Record(PublicApiUsage.Tracer_StartActive);
            TelemetryFactory.Metrics.RecordCountSpanCreated(MetricTags.IntegrationName.Manual);
            return StartActiveInternal(operationName);
        }

        /// <summary>
        /// This creates a new span with the given parameters and makes it active.
        /// </summary>
        /// <param name="operationName">The span's operation name</param>
        /// <param name="settings">Settings for the new <see cref="IScope"/></param>
        /// <returns>A scope wrapping the newly created span</returns>
        [PublicApi]
        public IScope StartActive(string operationName, SpanCreationSettings settings)
        {
            TelemetryFactory.Metrics.Record(PublicApiUsage.Tracer_StartActive_Settings);
            TelemetryFactory.Metrics.RecordCountSpanCreated(MetricTags.IntegrationName.Manual);
            var finishOnClose = settings.FinishOnClose ?? true;
            return StartActiveInternal(operationName, settings.Parent, serviceName: null, settings.StartTime, finishOnClose);
        }

        /// <summary>
        /// Creates a new <see cref="ISpan"/> with the specified parameters.
        /// </summary>
        /// <param name="operationName">The span's operation name</param>
        /// <param name="parent">The span's parent</param>
        /// <param name="serviceName">The span's service name</param>
        /// <param name="startTime">An explicit start time for that span</param>
        /// <param name="ignoreActiveScope">If set the span will not be a child of the currently active span</param>
        /// <returns>The newly created span</returns>
        ISpan IDatadogOpenTracingTracer.StartSpan(string operationName, ISpanContext parent, string serviceName, DateTimeOffset? startTime, bool ignoreActiveScope)
        {
            if (ignoreActiveScope && parent == null)
            {
                // don't set the span's parent,
                // even if there is an active span
                parent = SpanContext.None;
            }

            var span = StartSpan(operationName, tags: null, parent, serviceName: null, startTime);

            if (serviceName != null)
            {
                // if specified, override the default service name
                span.ServiceName = serviceName;
            }

            return span;
        }

        /// <summary>
        /// Forces the tracer to immediately flush pending traces and send them to the agent.
        /// To be called when the appdomain or the process is about to be killed in a non-graceful way.
        /// </summary>
        /// <returns>Task used to track the async flush operation</returns>
        [PublicApi]
        public Task ForceFlushAsync()
        {
            TelemetryFactory.Metrics.Record(PublicApiUsage.Tracer_ForceFlushAsync);
            return FlushAsync();
        }

        /// <summary>
        /// Writes the specified <see cref="Span"/> collection to the agent writer.
        /// </summary>
        /// <param name="trace">The <see cref="Span"/> collection to write.</param>
        void IDatadogTracer.Write(ArraySegment<Span> trace)
        {
            if (Settings.TraceEnabledInternal || Settings.AzureAppServiceMetadata?.CustomTracingEnabled is true)
            {
                TracerManager.WriteTrace(trace);
            }
        }

        /// <summary>
        /// Make a span the active span and return its new scope.
        /// </summary>
        /// <param name="span">The span to activate.</param>
        /// <param name="finishOnClose">Determines whether closing the returned scope will also finish the span.</param>
        /// <returns>A Scope object wrapping this span.</returns>
        internal Scope ActivateSpan(Span span, bool finishOnClose = true)
        {
            return TracerManager.ScopeManager.Activate(span, finishOnClose);
        }

        internal SpanContext CreateSpanContext(ISpanContext parent = null, string serviceName = null, TraceId traceId = default, ulong spanId = 0, string rawTraceId = null, string rawSpanId = null)
        {
            // null parent means use the currently active span
            parent ??= DistributedTracer.Instance.GetSpanContext() ?? TracerManager.ScopeManager.Active?.Span?.Context;

            TraceContext traceContext;

            if (parent is SpanContext parentSpanContext)
            {
                // if the parent's TraceContext is not null, parent is a local span
                // and the new span we are creating belongs in the same TraceContext
                traceContext = parentSpanContext.TraceContext;

                if (traceContext == null)
                {
                    // If parent is SpanContext but its TraceContext is null, then it was extracted from
                    // propagation headers. Create a new TraceContext (this will start a new trace) and initialize
                    // it with the propagated values (sampling priority, origin, tags, W3C trace state, etc).
                    traceContext = new TraceContext(this, parentSpanContext.PropagatedTags);
                    TelemetryFactory.Metrics.RecordCountTraceSegmentCreated(MetricTags.TraceContinuation.Continued);

                    var samplingPriority = parentSpanContext.SamplingPriority ?? DistributedTracer.Instance.GetSamplingPriority();
                    traceContext.SetSamplingPriority(samplingPriority);
                    traceContext.Origin = parentSpanContext.Origin;
                    traceContext.AdditionalW3CTraceState = parentSpanContext.AdditionalW3CTraceState;
                }
            }
            else
            {
                // if parent is not a SpanContext, it must be either a ReadOnlySpanContext,
                // a user-defined ISpanContext implementation, or null (no parent). we don't have a TraceContext,
                // so create a new one (this will start a new trace).
                traceContext = new TraceContext(this, tags: null);
                TelemetryFactory.Metrics.RecordCountTraceSegmentCreated(MetricTags.TraceContinuation.New);

                // in a version-mismatch scenario, try to get the sampling priority from the "other" tracer
                var samplingPriority = DistributedTracer.Instance.GetSamplingPriority();
                traceContext.SetSamplingPriority(samplingPriority);

                if (traceId == TraceId.Zero &&
                    Activity.ActivityListener.GetCurrentActivity() is Activity.DuckTypes.IW3CActivity { TraceId: { } activityTraceId })
                {
                    // if there's an existing Activity we try to use its TraceId,
                    // but if Activity.IdFormat is not ActivityIdFormat.W3C, it may be null or unparsable
                    rawTraceId = activityTraceId;
                    HexString.TryParseTraceId(activityTraceId, out traceId);
                }
            }

            var finalServiceName = serviceName ?? DefaultServiceName;

            if (traceId == TraceId.Zero)
            {
                // generate the trace id here using the 128-bit setting
                // instead of letting the SpanContext generate it in its ctor
                var useAllBits = Settings?.TraceId128BitGenerationEnabled ?? true;
                traceId = RandomIdGenerator.Shared.NextTraceId(useAllBits);
            }

            return new SpanContext(parent, traceContext, finalServiceName, traceId: traceId, spanId: spanId, rawTraceId: rawTraceId, rawSpanId: rawSpanId);
        }

        /// <summary>
        /// Remarks
        /// When calling this method from an integration, ensure you call
        /// Tracer.Instance.TracerManager.Telemetry.IntegrationGenerateSpan so that the integration is recorded,
        /// and the span count metric is incremented. Alternatively, if this is not being called from an
        /// automatic integration, call TelemetryFactory.Metrics.RecordCountSpanCreated() directory instead.
        /// </summary>
        internal Scope StartActiveInternal(string operationName, ISpanContext parent = null, string serviceName = null, DateTimeOffset? startTime = null, bool finishOnClose = true, ITags tags = null)
        {
            var span = StartSpan(operationName, tags, parent, serviceName, startTime);
            return TracerManager.ScopeManager.Activate(span, finishOnClose);
        }

        /// <summary>
        /// Remarks
        /// When calling this method from an integration, and _not_ discarding the span, ensure you call
        /// Tracer.Instance.TracerManager.Telemetry.IntegrationGenerateSpan so that the integration is recorded,
        /// and the span count metric is incremented. Alternatively, if this is not being called from an
        /// automatic integration, call TelemetryFactory.Metrics.RecordCountSpanCreated() directly instead.
        /// </summary>
        internal Span StartSpan(string operationName, ITags tags = null, ISpanContext parent = null, string serviceName = null, DateTimeOffset? startTime = null, TraceId traceId = default, ulong spanId = 0, string rawTraceId = null, string rawSpanId = null, bool addToTraceContext = true)
        {
            var spanContext = CreateSpanContext(parent, serviceName, traceId, spanId, rawTraceId, rawSpanId);

            var span = new Span(spanContext, startTime, tags)
            {
                OperationName = operationName,
            };

            try
            {
                SpanOriginResolution(span);
            }
            catch(Exception e)
            {
                Log.Warning(e, "SpanOriginResolution - failed to resolve span origin");
            }

            // Apply any global tags
            if (Settings.GlobalTagsInternal.Count > 0)
            {
                // if DD_TAGS contained "env", "version", "git.commit.sha", or "git.repository.url",  they were used to set
                // ImmutableTracerSettings.Environment, ImmutableTracerSettings.ServiceVersion, ImmutableTracerSettings.GitCommitSha, and ImmutableTracerSettings.GitRepositoryUrl
                // and removed from Settings.GlobalTags
                foreach (var entry in Settings.GlobalTagsInternal)
                {
                    span.SetTag(entry.Key, entry.Value);
                }
            }

            if (addToTraceContext)
            {
                spanContext.TraceContext.AddSpan(span);
            }

            // Extract the Git metadata. This is done here because we may only be able to do it in the context of a request.
            // However, to reduce memory consumption, we don't actually add the result as tags on the span, and instead
            // write them directly to the <see cref="TraceChunkModel"/>.
            TracerManager.GitMetadataTagsProvider.TryExtractGitMetadata(out _);

            return span;
        }

        internal Task FlushAsync()
        {
            return TracerManager.AgentWriter.FlushTracesAsync();
        }

        // This method attempts to find the local span origin method by examining the stack trace.
        // It differentiates between user code and non-user code based on predefined criteria.
        // Note: This method may not accurately identify the user method for async methods with compiler-generated names or virtual methods.
        private static bool LocalSpanOriginMethod(out MethodBase nonUserMethod, out MethodBase userMethod)
        {
            var stackFrames = new System.Diagnostics.StackTrace();

            var encounteredUserCode = false;
            System.Reflection.MethodBase firstNonUserCodeMethod = null;
            System.Reflection.MethodBase firstUserCodeMethod = null;

            foreach (var frame in stackFrames.GetFrames()!)
            {
                var method = frame.GetMethod();
                if (method?.DeclaringType == null)
                {
                    continue;
                }

                Log.Information("SpanOriginResolution - frame {0}", $"{method.DeclaringType?.FullName} {method.Name}- file {frame.GetFileName()} line {frame.GetFileLineNumber()}");

                static bool IsUserCode(string methodFullName, MethodBase method)
                {
                    // Check for compiler-generated methods and other non-user code patterns
                    if (method.GetCustomAttributes(typeof(CompilerGeneratedAttribute), inherit: true).Any() ||
                        methodFullName.StartsWith("System.") ||
                        methodFullName.StartsWith("Microsoft.") ||
                        methodFullName.StartsWith("Datadog.Trace.") ||
                        methodFullName.StartsWith("Serilog.") ||
                        methodFullName.StartsWith("NHibernate.") ||
                        methodFullName.StartsWith("Swashbuckle.") ||
                        methodFullName.StartsWith("MySql.Data."))
                    {
                        return false;
                    }

                    // Additional checks can be added here if necessary

                    return true;
                }

                if (IsUserCode(method.DeclaringType.FullName, method))
                {
                    encounteredUserCode = true;
                    firstUserCodeMethod = method;
                    break;
                }

                if (!encounteredUserCode)
                {
                    firstNonUserCodeMethod = method;
                }
            }

            if (firstNonUserCodeMethod == null || firstUserCodeMethod == null)
            {
                // TODO LOG
                nonUserMethod = null;
                userMethod = null;
                return true;
            }

            nonUserMethod = firstNonUserCodeMethod;
            userMethod = firstUserCodeMethod;
            return false;
        }

        // This method resolves the origin of a span by finding the associated sequence point in the method instructions.
        // If the matching call instruction is not found, it falls back to the first sequence point with a non-zero start line.
        // This fallback is a temporary measure and may not accurately represent the true span origin in complex scenarios such as async methods.
        private static void SpanOriginResolution(Span span)
        {
            Log.Information("SpanOriginResolution - started for span {0} {1}", span.OperationName, span.ResourceName);
            if (LocalSpanOriginMethod(out var nonUserMethod, out var userMethod))
            {
                Log.Information("SpanOriginResolution - failed to find local span origin method");
                return;
            }

            Log.Information("SpanOriginResolution - found local span origin method {0} {1}", userMethod.DeclaringType?.FullName, userMethod.Name);

            var userModule = ModuleDefMD.Load(userMethod.Module.Assembly.ManifestModule);
            var userRid = MDToken.ToRID(userMethod.MetadataToken);
            var userMdMethod = userModule.ResolveMethod(userRid);

            if (!userMdMethod.Body.HasInstructions)
            {
                Log.Warning("SpanOriginResolution - Method {0} has no instructions", userMdMethod.FullName);
                return;
            }

            var nonUserModule = ModuleDefMD.Load(nonUserMethod.Module.Assembly.ManifestModule);
            var nonUserRid = MDToken.ToRID(nonUserMethod.MetadataToken);
            var nonUserMdMethod = nonUserModule.ResolveMethod(nonUserRid);

            // We have to assign the module context to be able to resolve memberRef to memberdef.
            userModule.Context = ModuleDef.CreateModuleContext();
            var nonUserMethodFullName = nonUserMdMethod.Name;

            var callsToInstrument = userMdMethod.Body.Instructions.Where(
                instruction => instruction.OpCode.FlowControl == FlowControl.Call &&
                               (instruction.Operand as IMethod != null &&
                                (instruction.Operand as IMethod)!.Name == nonUserMethodFullName));

            Instruction matchingCall = callsToInstrument.FirstOrDefault(); // this doesn't work in all cases, doesn't work for async methods with weird compiler generated names, doesn't take into account virtual methods, etc.
            if (matchingCall == null)
            {
                Log.Warning("SpanOriginResolution - No calls to {0} found in {1}. Attempting to find the closest sequence point before span creation.", nonUserMethodFullName, userMethod.Module.Assembly.FullName);
                var sequencePoints = userMdMethod.Body.Instructions
                    .Select(i => i.SequencePoint)
                    .Where(sp => sp != null && sp.StartLine != 0)
                    .OrderByDescending(sp => sp.Offset)
                    .ToList();

                var closestSequencePoint = sequencePoints.FirstOrDefault();
                if (closestSequencePoint == null)
                {
                    Log.Warning("SpanOriginResolution - no sequence points found in {0}", userMdMethod);
                    return;
                }
                matchingCall = userMdMethod.Body.Instructions.First(i => i.SequencePoint == closestSequencePoint);
            }

            uint offsetOfSpanOrigin = matchingCall.Offset;
            var instructions = TimeTravelInitiator.FindMethod((MethodInfo)userMethod).Body.Instructions;
            var sequencePoint = instructions.Reverse().First(instruction => instruction.SequencePoint != null &&
                                                                            instruction.Offset <= offsetOfSpanOrigin).SequencePoint;

            span.Tags.SetTag("_dd.exit_location.file", sequencePoint.Document.Url);
            span.Tags.SetTag("_dd.exit_location.line", sequencePoint.StartLine.ToString());
            span.Tags.SetTag("_dd.exit_location.snapshot_id", DebuggerSnapshotCreator.LastSnapshotId.ToString());
            Log.Information("SpanOriginResolution - success - {0} {1} {2}", sequencePoint.Document.Url, sequencePoint.StartLine, DebuggerSnapshotCreator.LastSnapshotId);

            FakeProbeCreator.CreateAndInstallLineProbe("SpanExit", new NativeLineProbeDefinition(
                $"{userMethod.DeclaringType?.FullName}_{userMethod.Name}",
                userMethod.Module.ModuleVersionId,
                userMethod.MetadataToken,
                (int)offsetOfSpanOrigin,
                sequencePoint.StartLine,
                sequencePoint.Document.Url));

        }
    }
}
