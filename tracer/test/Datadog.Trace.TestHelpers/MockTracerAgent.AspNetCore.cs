﻿// <copyright file="MockTracerAgent.AspNetCore.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
#if NET8_0_OR_GREATER

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Datadog.Trace.Telemetry;
using Datadog.Trace.TestHelpers.DataStreamsMonitoring;
using Datadog.Trace.TestHelpers.Stats;
using Datadog.Trace.Util;
using Google.Protobuf;
using Google.Protobuf.Collections;
using MartinCostello.Logging.XUnit;
using MessagePack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Newtonsoft.Json;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace Datadog.Trace.TestHelpers;

/// <summary>
/// A mock agent that can be used to test the tracer.
/// </summary>
public abstract partial class MockTracerAgent
{
    public static TcpUdpAgent Create(ITestOutputHelper output, int? port = null, int retries = 5, bool useStatsd = false, bool doNotBindPorts = false, int? requestedStatsDPort = null, bool useTelemetry = false, AgentConfiguration agentConfiguration = null)
        => new(port, retries, useStatsd, doNotBindPorts, requestedStatsDPort, useTelemetry) { Output = output, Configuration = agentConfiguration ?? new() };

    public static UdsAgent Create(ITestOutputHelper output, UnixDomainSocketConfig config, AgentConfiguration agentConfiguration = null)
        => new(config) { Output = output, Configuration = agentConfiguration ?? new() };

    public static NamedPipeAgent Create(ITestOutputHelper output, WindowsPipesConfig config, AgentConfiguration agentConfiguration = null)
        => new(config) { Output = output, Configuration = agentConfiguration ?? new() };

    public abstract class AspNetCoreMockAgent : MockTracerAgent, ITestOutputHelperAccessor
    {
        private readonly Task _appTask;

        protected AspNetCoreMockAgent(bool telemetryEnabled, TestTransports transport, IEnumerable<KeyValuePair<string, string>> config)
            : base(telemetryEnabled, transport)
        {
            // this is a horrible hack, because I can't get UseStaticWebAssets() working
            var wwwroot = Path.Combine(
                EnvironmentTools.GetSolutionDirectory(),
                "tracer",
                "test",
                "Datadog.Trace.TestHelpers",
                "wwwroot");

            var builder = WebApplication.CreateBuilder(
                new WebApplicationOptions()
                {
                    ApplicationName = nameof(AspNetCoreMockAgent),
                    EnvironmentName = Environments.Development, // so we get detailed error messages
                    Args = [$"--{WebHostDefaults.PreventHostingStartupKey}=1"], // don't try to load as a hosting assembly
                    WebRootPath = wwwroot,
                });

            // clear out existing configuration so we don't get any cross-talk
            // with configuration we're setting up for the test
            builder.Configuration.Sources.Clear();

            // add our own configuration source
            builder.Configuration.AddInMemoryCollection(config);

            // Configure logging to write to the output helper only
            // builder.Logging.ClearProviders();
            builder.Logging.AddXUnit(this);

            builder.Services.AddRequestDecompression();
            builder.Services.AddSingleton<MockTracerAgent>(this);

            // Configure services required by .NET Aspire
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();
            builder.Services.AddSingleton<TelemetryRepository>();
            builder.Services.AddTransient<StructuredLogsViewModel>();
            builder.Services.AddTransient<TracesViewModel>();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IOutgoingPeerResolver, ResourceOutgoingPeerResolver>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IOutgoingPeerResolver, BrowserLinkOutgoingPeerResolver>());
            builder.Services.AddFluentUIComponents();
            builder.Services.AddSingleton<ThemeManager>();
            builder.Services.AddSingleton<IDashboardViewModelService, DashboardViewModelService>();

            App = builder.Build();

            // Aspire config
            App.UseStaticFiles(new StaticFileOptions()
            {
                OnPrepareResponse = (context) =>
                {
                    // If Cache-Control isn't already set to something, set it to 'no-cache' so that the
                    // ETag and Last-Modified headers will be respected by the browser.
                    // This may be able to be removed if https://github.com/dotnet/aspnetcore/issues/44153
                    // is fixed to make this the default
                    if (context.Context.Response.Headers.CacheControl.Count == 0)
                    {
                        context.Context.Response.Headers.CacheControl = "no-cache";
                    }
                }
            });
            App.UseAuthorization();
            App.UseAntiforgery();
            App.MapRazorComponents<Aspire.Dashboard.Components.App>().AddInteractiveServerRenderMode();
            // End Aspire config

            // Automatically decompress gzip requests
            App.UseRequestDecompression();

            App.UseMiddleware<CustomHeaderMiddleware>();

            // Configure endpoints
            // TODO: add better support for events?

            // Create a group so we can add cross-cutting behaviour
            var endpoints = App
                           .MapGroup("/")
                           .AddEndpointFilter(CustomResponseFilter);

            if (telemetryEnabled)
            {
                endpoints.MapPost("/telemetry/proxy/api/v2/apmtelemetry", Handlers.Telemetry)
                         .WithMetadata(new ResponseTypeMetadata(MockTracerResponseType.Telemetry));
            }

            endpoints.MapGet("/info", () => JsonConvert.SerializeObject(Configuration))
                     .WithMetadata(new ResponseTypeMetadata(MockTracerResponseType.Info));

            endpoints.MapPost("/debugger/v1/input", Handlers.Debugger)
                     .WithMetadata(new ResponseTypeMetadata(MockTracerResponseType.Debugger));

            endpoints.MapPost("/v0.6/stats", Handlers.Stats)
                     .WithMetadata(new ResponseTypeMetadata(MockTracerResponseType.Stats));

            endpoints.MapPost("/v0.7/config", ([FromBody] string rc) => RemoteConfigRequests.Enqueue(rc))
                     .WithMetadata(new ResponseTypeMetadata(MockTracerResponseType.RemoteConfig));

            endpoints.MapPost("/v0.1/pipeline_stats", Handlers.DataStreams)
                     .WithMetadata(new ResponseTypeMetadata(MockTracerResponseType.DataStreams));

            endpoints.MapPost("/evp_proxy/v{proxyVersion:int}/", Handlers.EvpProxy)
                     .WithMetadata(new ResponseTypeMetadata(MockTracerResponseType.EvpProxy));

            endpoints.MapPost("/tracer_flare/v1", Handlers.TracerFlare)
                     .WithMetadata(new ResponseTypeMetadata(MockTracerResponseType.TracerFlare))
                     .DisableAntiforgery();

            endpoints.MapPost("/v0.4/traces", Handlers.Traces)
                     .WithMetadata(new ResponseTypeMetadata(MockTracerResponseType.Traces));

            // Use this to stop the agent after viewing the dashboard
            endpoints.MapGet("/stop", (IHostApplicationLifetime lifetime) =>
            {
                CancellationTokenSource.Cancel();
                return "Terminating...";
            });

            _appTask = App.RunAsync(CancellationTokenSource.Token);

            using var scope = App.Services.CreateScope();
            ListeningAddresses = scope.ServiceProvider
                               .GetRequiredService<IServer>()
                               .Features
                               .GetRequiredFeature<IServerAddressesFeature>()
                               .Addresses;
        }

        ITestOutputHelper ITestOutputHelperAccessor.OutputHelper
        {
            get => Output;
            set => Output = value;
        }

        protected WebApplication App { get; }

        protected CancellationTokenSource CancellationTokenSource { get; } = new();

        protected ICollection<string> ListeningAddresses { get; }

        public override async Task OpenDashboard()
        {
            // open the aspire dashboard
            var url = ListeningAddresses.First();
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

            try
            {
                await Task.Delay(Timeout.Infinite, CancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                // expected
            }
        }

        public override void Dispose()
        {
            // TODO: DisposeAsync
            CancellationTokenSource.Cancel();
            _appTask.GetAwaiter().GetResult();
        }

        private async ValueTask<object> CustomResponseFilter(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            // if we shouldn't deserialize then we just short circuit;
            if (!ShouldDeserializeTraces)
            {
                return Results.Content(
                    content: "{}",
                    contentType: MediaTypeNames.Application.Json,
                    statusCode: 200);
            }

            var endpointMetadataCollection = context.HttpContext.GetEndpoint()?.Metadata;
            var responseType = endpointMetadataCollection?.GetMetadata<ResponseTypeMetadata>()?.ResponseType;

            // we always run the endpoint, we just change the response if needs be
            var result = await next(context);

            if (responseType is not null
                && CustomResponses.TryGetValue(responseType.Value, out var custom))
            {
                return Results.Content(
                    content: custom.Response,
                    contentType: MediaTypeNames.Application.Json,
                    statusCode: custom.StatusCode);
            }

            return result;
        }

        private record ResponseTypeMetadata(MockTracerResponseType ResponseType);

        public class CustomHeaderMiddleware(RequestDelegate next, MockTracerAgent agent)
        {
            private readonly RequestDelegate _next = next;
            private readonly MockTracerAgent _agent = agent;

            public Task Invoke(HttpContext context)
            {
                if (!string.IsNullOrEmpty(_agent.Version))
                {
                    context.Response.OnStarting(() =>
                    {
                        context.Response.Headers["Datadog-Agent-Version"] = _agent.Version;
                        return Task.CompletedTask;
                    });
                }

                return _next(context);
            }
        }

        private static class Handlers
        {
            public static async Task Telemetry(
                [FromHeader(Name = TelemetryConstants.ApiVersionHeader)] string apiVersion,
                [FromHeader(Name = TelemetryConstants.RequestTypeHeader)] string requestType,
                Stream body,
                [FromServices] MockTracerAgent agent)
            {
                var telemetry = await MockTelemetryAgent.DeserializeResponseAsync(body, apiVersion, requestType);
                agent.Telemetry.Push(telemetry);
            }

            public static void Debugger([FromServices] MockTracerAgent agent, [FromBody] string batch)
            {
                // TODO: do this properly using model binding
                agent.ReceiveDebuggerBatch(batch);
            }

            public static async Task Stats([FromServices] MockTracerAgent agent, Stream body)
            {
                var payload = await MessagePackSerializer.DeserializeAsync<MockClientStatsPayload>(body);

                lock (agent)
                {
                    agent.Stats = agent.Stats.Add(payload);
                }
            }

            public static async Task DataStreams([FromServices] MockTracerAgent agent, HttpRequest request)
            {
                var payload = await MessagePackSerializer.DeserializeAsync<MockDataStreamsPayload>(request.Body);
                var headerCollection = new NameValueCollection();
                foreach (var header in request.Headers)
                {
                    headerCollection.Add(header.Key, header.Value);
                }

                lock (agent)
                {
                    agent.DataStreams = agent.DataStreams.Add(payload);
                    agent.DataStreamsRequestHeaders = agent.DataStreamsRequestHeaders.Add(headerCollection);
                }
            }

            public static void EvpProxy(
                [FromServices] MockTracerAgent agent,
                HttpRequest request)
            {
                // TODO: fix this properly
                agent.EventPlatformProxyPayloadReceived?.Invoke(agent, new EventArgs<EvpProxyPayload>(null));
                // This one makes me itchy so leaving for later
                throw new NotImplementedException();
            }

            public static async Task TracerFlare(
                [FromServices] MockTracerAgent agent,
                HttpRequest request)
            {
                // TODO: parse this properly
                var form = await request.ReadFormAsync();
            }

            public static async Task Traces(
                [FromServices] MockTracerAgent agent,
                [FromServices] TelemetryRepository repo,
                HttpRequest request)
            {
                var spans = await MessagePackSerializer.DeserializeAsync<IList<IList<MockSpan>>>(request.Body);

                var addContext = new AddContext();
                repo.AddTraces(addContext, OtlpConverter.GetTraces(spans));

                var headerCollection = new NameValueCollection();
                foreach (var header in request.Headers)
                {
                    headerCollection.Add(header.Key, header.Value);
                }

                lock (agent)
                {
                    // we only need to lock when replacing the span collection,
                    // not when reading it because it is immutable
                    agent.Spans = agent.Spans.AddRange(spans.SelectMany(trace => trace));

                    agent.TraceRequestHeaders = agent.TraceRequestHeaders.Add(headerCollection);
                }
            }
        }

        private static class OtlpConverter
        {
            public static RepeatedField<ResourceSpans> GetTraces(IList<IList<MockSpan>> traces)
            {
                var otelTraces = new RepeatedField<ResourceSpans>();
                otelTraces.AddRange(traces.Select(GetTrace));
                return otelTraces;
            }

            private static ResourceSpans GetTrace(IList<MockSpan> trace)
            {
                var resourceSpan = new ResourceSpans
                {
                    Resource = new Resource()
                    {
                        Attributes =
                        {
                            new KeyValue { Key = "service.name", Value = new AnyValue { StringValue = nameof(AspNetCoreMockAgent) } },
                            new KeyValue { Key = "service.instance.id", Value = new AnyValue { StringValue = nameof(AspNetCoreMockAgent) } }
                        }
                    },
                    ScopeSpans =
                    {
                        new ScopeSpans
                        {
                            Scope = new InstrumentationScope { },
                            Spans = { }
                        }
                    }
                };

                resourceSpan.ScopeSpans[0].Spans.AddRange(trace.Select(GetSpan));

                return resourceSpan;
            }

            private static OpenTelemetry.Proto.Trace.V1.Span GetSpan(MockSpan span)
            {
                var otelSpan = new OpenTelemetry.Proto.Trace.V1.Span
                        {
                            TraceId = ByteString.CopyFrom(Encoding.UTF8.GetBytes(span.TraceId.ToString())),
                            SpanId = ByteString.CopyFrom(Encoding.UTF8.GetBytes(span.SpanId.ToString())),
                            ParentSpanId = span.ParentId is null ? ByteString.Empty : ByteString.CopyFrom(Encoding.UTF8.GetBytes(span.ParentId.ToString())),
                            StartTimeUnixNano = (ulong)span.Start,
                            EndTimeUnixNano = (ulong)(span.Start + span.Duration),
                            Name = span.Resource,
                        };
                // TODO: add the rest
                return otelSpan;
            }
        }

        private class DashboardViewModelService : IDashboardViewModelService,
                                                  IAsyncEnumerator<ResourceChanged<ResourceViewModel>>,
                                                  IAsyncEnumerable<ResourceChanged<ResourceViewModel>>,
                                                  ILogSource
        {
            public ResourceChanged<ResourceViewModel> Current => default!;

            public string ApplicationName => nameof(AspNetCoreMockAgent);

            public ViewModelMonitor<ResourceViewModel> GetResources()
            {
                return new ViewModelMonitor<ResourceViewModel>(
                    Snapshot:
                    [
                        new ProjectViewModel
                        {
                            ProjectPath = "dont/care",
                            Name = nameof(AspNetCoreMockAgent),
                            DisplayName = nameof(AspNetCoreMockAgent),
                            Uid = nameof(AspNetCoreMockAgent),
                            NamespacedName = new(nameof(AspNetCoreMockAgent), null),
                            LogSource = this
                        }
                    ],
                    Watch: this);
            }

            public ValueTask DisposeAsync() => default;

            public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(false);

            public IAsyncEnumerator<ResourceChanged<ResourceViewModel>> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken()) => this;

            public ValueTask<bool> StartAsync(CancellationToken cancellationToken) => ValueTask.FromResult(true);

            public async IAsyncEnumerable<string[]> WatchOutputLogAsync([EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.Yield();
                yield break;
            }

            public async IAsyncEnumerable<string[]> WatchErrorLogAsync([EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.Yield();
                yield break;
            }

            public ValueTask StopAsync(CancellationToken cancellationToken = new CancellationToken()) => ValueTask.CompletedTask;
        }
    }

    public class TcpUdpAgent : AspNetCoreMockAgent
    {
        private readonly StatsdUdpAgent _statsd;

        public TcpUdpAgent(int? port, int retries, bool useStatsd, bool doNotBindPorts, int? requestedStatsDPort, bool useTelemetry)
            : base(telemetryEnabled: useTelemetry, TestTransports.Tcp, GetConfig(port))
        {
            var listeners = new List<string>(ListeningAddresses.Count + 1);
            var tracesPort = port;
            foreach (var address in ListeningAddresses)
            {
                listeners.Add($"Traces at {address}");
                if (tracesPort is null && Uri.TryCreate(address, UriKind.Absolute, out var uri))
                {
                    tracesPort = uri.Port;
                }
            }

            if (useStatsd)
            {
                listeners.Add($"Statsd at port {StatsdPort}");
                _statsd = new StatsdUdpAgent(retries, requestedStatsDPort, CancellationTokenSource);
            }

            ListenerInfo = string.Join(", ", listeners);
            Port = tracesPort ?? 0;
        }

        /// <summary>
        /// Gets the TCP port that this Agent is listening on.
        /// Can be different from the request port if listening on that port fails.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Gets the UDP port for statsd
        /// </summary>
        public int StatsdPort => _statsd?.StatsdPort ?? 0;

        public override ConcurrentQueue<string> StatsdRequests => _statsd?.StatsdRequests ?? [];

        public override ConcurrentQueue<Exception> StatsdExceptions => _statsd?.StatsdExceptions ?? [];

        public override void Dispose()
        {
            base.Dispose();
            _statsd?.Dispose();
        }

        private static Dictionary<string, string> GetConfig(int? port)
        {
            // if we specify a _specific_ port, then require that we can bind to it
            // if not, then use a randomly chosen available port
            // Do we care about CONTAINER_HOSTNAME? Should we listen on any IP? I don't think it's necessary
            var url = port.HasValue
                ?  $"http://127.0.0.1:{port};http://localhost:{port};"
                :  "http://127.0.0.1:0;"; // can't use localhost:0, not allowed

            return new() { { "URLS", url } };
        }
    }

    public class UdsAgent : AspNetCoreMockAgent
    {
        private readonly StatsdUdsAgent _statsd;

        public UdsAgent(UnixDomainSocketConfig config)
            : base(telemetryEnabled: config.UseTelemetry, TestTransports.Uds, GetConfig(config.Traces))
        {
            var listeners = ListeningAddresses.Select(address => $"Traces at UDS path {address}");

            if (config.UseDogstatsD)
            {
                _statsd = new StatsdUdsAgent(config.Metrics, CancellationTokenSource);
                listeners = listeners.Concat([$"Statsd at UDS path {_statsd.StatsUdsPath}"]);
            }

            TracesUdsPath = config.Traces;
            ListenerInfo = string.Join(", ", listeners);
        }

        public string TracesUdsPath { get; }

        public string StatsUdsPath => _statsd?.StatsUdsPath;

        public override ConcurrentQueue<string> StatsdRequests => _statsd?.StatsdRequests ?? [];

        public override ConcurrentQueue<Exception> StatsdExceptions => _statsd?.StatsdExceptions ?? [];

        public override void Dispose()
        {
            base.Dispose();
            _statsd?.Dispose();
        }

        private static Dictionary<string, string> GetConfig(string udsPath)
        {
            var url = $"http://unix:{udsPath}";

            return new() { { "Kestrel:Endpoints:NamedPipeEndpoint:Url", url } };
        }
    }

    public class NamedPipeAgent : AspNetCoreMockAgent
    {
        private readonly StatsdNamedPipeAgent _statsd;

        public NamedPipeAgent(WindowsPipesConfig config)
            : base(telemetryEnabled: config.UseTelemetry, TestTransports.WindowsNamedPipe, GetConfig(config.Traces))
        {
            var listeners = ListeningAddresses.Select(address => $"Traces at pipe path {address}");

            if (config.UseDogstatsD)
            {
                _statsd = new StatsdNamedPipeAgent(config.Metrics, CancellationTokenSource);
                listeners = listeners.Concat([$"Statsd at pipe path {_statsd.StatsWindowsPipeName}"]);
            }

            TracesWindowsPipeName = config.Traces;
            ListenerInfo = string.Join(", ", listeners);
        }

        public string TracesWindowsPipeName { get; }

        public string StatsWindowsPipeName => _statsd?.StatsWindowsPipeName;

        public override ConcurrentQueue<string> StatsdRequests => _statsd?.StatsdRequests ?? [];

        public override ConcurrentQueue<Exception> StatsdExceptions => _statsd?.StatsdExceptions ?? [];

        public override void Dispose()
        {
            base.Dispose();
            _statsd?.Dispose();
        }

        private static Dictionary<string, string> GetConfig(string udsPath)
        {
            var url = udsPath.StartsWith('/')
                          ? $"http://pipe:{udsPath}"
                          : $"http://pipe:/{udsPath}";

            return new() { { "Kestrel:Endpoints:NamedPipeEndpoint:Url", url } };
        }
    }
}
#endif
