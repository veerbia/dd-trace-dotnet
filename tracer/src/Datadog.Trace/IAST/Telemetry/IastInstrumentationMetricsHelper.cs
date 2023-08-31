// <copyright file="IastInstrumentationMetricsHelper.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Collections.Generic;
using Datadog.Trace.ClrProfiler;
using Datadog.Trace.ClrProfiler.AutoInstrumentation.AspNetCore;
using Datadog.Trace.ClrProfiler.AutoInstrumentation.CryptographyAlgorithm;
using Datadog.Trace.ClrProfiler.AutoInstrumentation.HashAlgorithm;
using Datadog.Trace.ClrProfiler.AutoInstrumentation.Process;
using Datadog.Trace.Configuration;
using Datadog.Trace.Iast.Dataflow;
using Datadog.Trace.Telemetry;
using static Datadog.Trace.Telemetry.Metrics.MetricTags;

namespace Datadog.Trace.Iast.Telemetry;

internal static class IastInstrumentationMetricsHelper
{
    private static int _instrumentedPropagations = 0;
    private static int _sinksCount = Enum.GetValues(typeof(IastInstrumentedSinks)).Length;
    private static int _sourceTypesCount = Enum.GetValues(typeof(IastInstrumentedSources)).Length;
    private static int[] _instrumentedSources = new int[_sourceTypesCount];
    private static int[] _instrumentedSinks = new int[_sinksCount];
    private static IastMetricsVerbosityLevel _verbosityLevel = Iast.Instance.Settings.IastTelemetryVerbosity;
    private static bool _iastEnabled = Iast.Instance.Settings.Enabled;
    private static Dictionary<string, IastUsage[]>? _iastIntegrations = GetTracerInstrumentations();

    private static Dictionary<string, IastUsage[]>? GetTracerInstrumentations()
    {
        if (!_iastEnabled || _verbosityLevel == IastMetricsVerbosityLevel.Off)
        {
            return null;
        }

        return new Dictionary<string, IastUsage[]>()
        {
            { nameof(IntegrationId.Process), new IastUsage[] { new IastUsage(GetInstrumentedMethodsCounter(typeof(ProcessStartIntegration)), AspectType.Sink, new IastInstrumentedSinks[] { IastInstrumentedSinks.CommandInjection }) } },
#if !NETFRAMEWORK
            { nameof(IntegrationId.HashAlgorithm), new IastUsage[] { new IastUsage(GetInstrumentedMethodsCounter(typeof(HashAlgorithmIntegrationBis)) + GetInstrumentedMethodsCounter(typeof(HashAlgorithmIntegration)) + GetInstrumentedMethodsCounter(typeof(HashAlgorithmIntegrationTer)), AspectType.Sink, new IastInstrumentedSinks[] { IastInstrumentedSinks.WeakHash }) } },
            { nameof(IntegrationId.SymmetricAlgorithm), new IastUsage[] { new IastUsage(GetInstrumentedMethodsCounter(typeof(SymmetricAlgorithmIntegration)), AspectType.Sink, new IastInstrumentedSinks[] { IastInstrumentedSinks.WeakCipher }) } },
            { nameof(IntegrationId.AspNetCore), new IastUsage[] { new IastUsage(GetInstrumentedMethodsCounter(typeof(FireOnStartCommon)), AspectType.Sink, new IastInstrumentedSinks[] { IastInstrumentedSinks.InsecureCookie, IastInstrumentedSinks.NoHttpOnlyCookie, IastInstrumentedSinks.NoSameSiteCookie }), new IastUsage(GetInstrumentedMethodsCounter(typeof(DefaultModelBindingContext_SetResult_Integration)), AspectType.Source, sources: new IastInstrumentedSources[] { IastInstrumentedSources.RequestBody }) } },
#endif
        };
    }

    public static void ReportMetrics()
    {
        if (_iastEnabled && _verbosityLevel != IastMetricsVerbosityLevel.Off)
        {
            int[] instrumentedSinks = new int[_sinksCount];
            NativeMethods.GetIastMetrics(out int callsiteInstrumentedSources, out int callsiteInstrumentedPropagations, instrumentedSinks);

            for (int i = 0; i < _sinksCount; i++)
            {
                ReportSink(((IastInstrumentedSinks)i), instrumentedSinks[i]);
                instrumentedSinks[i] = 0;
            }

            if (callsiteInstrumentedSources > 0)
            {
                // We only have callsite calls for cookie sources
                ReportSource(IastInstrumentedSources.CookieValue, callsiteInstrumentedSources);
            }

            for (int i = 0; i < _sourceTypesCount; i++)
            {
                ReportSource(((IastInstrumentedSources)i));
                _instrumentedSources[i] = 0;
            }

            if (_instrumentedPropagations + callsiteInstrumentedPropagations > 0)
            {
                TelemetryFactory.Metrics.RecordCountIastInstrumentedPropagations(_instrumentedPropagations + callsiteInstrumentedPropagations);
                _instrumentedPropagations = 0;
            }
        }
    }

    private static void ReportSink(IastInstrumentedSinks tag, int callsiteHits = 0)
    {
        var totalHits = _instrumentedSinks[(int)tag] + callsiteHits;
        if (totalHits > 0)
        {
            TelemetryFactory.Metrics.RecordCountIastInstrumentedSinks(tag, totalHits);
        }
    }

    private static void ReportSource(IastInstrumentedSources tag, int callsiteHits = 0)
    {
        var totalHits = _instrumentedSources[(int)tag] + callsiteHits;
        if (totalHits > 0)
        {
            TelemetryFactory.Metrics.RecordCountIastInstrumentedSources(tag, totalHits);
        }
    }

    internal static void ReportIntegrations(ICollection<IntegrationTelemetryData>? integrations)
    {
        if ((_iastEnabled && _verbosityLevel != IastMetricsVerbosityLevel.Off) && (integrations != null))
        {
            foreach (var integration in integrations)
            {
                if (integration.Enabled || (integration.AutoEnabled == true))
                {
                    if (_iastIntegrations?.TryGetValue(integration.Name, out var usage) == true)
                    {
                        ProcessIastIntegration(usage);
                    }
                }
            }
        }
    }

    private static void ProcessIastIntegration(IastUsage[] usages)
    {
        foreach (var usage in usages)
        {
            switch (usage.Type)
            {
                case AspectType.Propagation:
                    _instrumentedPropagations += usage.Counter;
                    break;
                case AspectType.Sink:
                    if (usage.VulnerabilityTypes is not null)
                    {
                        foreach (var vulnerabilityType in usage.VulnerabilityTypes)
                        {
                            _instrumentedSinks[(int)vulnerabilityType] += usage.Counter;
                        }
                    }

                    break;
                case AspectType.Source:
                    if (usage.Sources is not null)
                    {
                        foreach (var source in usage.Sources)
                        {
                            _instrumentedSources[(int)source] += usage.Counter;
                        }
                    }

                    break;
            }
        }
    }

    private static int GetInstrumentedMethodsCounter(Type type)
    {
        int counter = 0;
        var attributes = type.GetCustomAttributes(typeof(InstrumentMethodAttribute), false);

        foreach (var attribute in attributes)
        {
            if ((attribute as InstrumentMethodAttribute)?.InstrumentationCategory.HasFlag(InstrumentationCategory.Iast) == true)
            {
                counter++;
            }
        }

        return counter;
    }

    private readonly struct IastUsage
    {
        public IastUsage(int counter, AspectType type, IastInstrumentedSinks[]? vulnerabilityTypes = null, IastInstrumentedSources[]? sources = null)
        {
            this.Counter = counter;
            this.Type = type;
            this.VulnerabilityTypes = vulnerabilityTypes;
            this.Sources = sources;
        }

        public int Counter { get; }

        public IastInstrumentedSources[]? Sources { get; }

        public IastInstrumentedSinks[]? VulnerabilityTypes { get; }

        public AspectType Type { get; }
    }
}
