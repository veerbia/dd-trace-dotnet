﻿// <auto-generated/>
#nullable enable

using System.Threading;

namespace Datadog.Trace.Telemetry;
internal partial class MetricsTelemetryCollector
{
    // These can technically overflow, but it's _very_ unlikely as we reset every minute
    // Negative values are normalized during polling
    public void RecordCountLogCreated(Datadog.Trace.Telemetry.Metrics.MetricTags.LogLevel tag, int increment = 1)
    {
        var index = 0 + (int)tag;
        Interlocked.Add(ref _buffer.Counts[index].Value, increment);
    }

    public void RecordCountIntegrationsError(Datadog.Trace.Telemetry.Metrics.MetricTags.IntegrationName tag1, Datadog.Trace.Telemetry.Metrics.MetricTags.InstrumentationError tag2, int increment = 1)
    {
        var index = 4 + ((int)tag1 * 3) + (int)tag2;
        Interlocked.Add(ref _buffer.Counts[index].Value, increment);
    }

    public void RecordCountSpanCreated(Datadog.Trace.Telemetry.Metrics.MetricTags.IntegrationName tag, int increment = 1)
    {
        var index = 145 + (int)tag;
        Interlocked.Add(ref _buffer.Counts[index].Value, increment);
    }

    public void RecordCountSpanFinished(int increment = 1)
    {
        Interlocked.Add(ref _buffer.Counts[192].Value, increment);
    }

    public void RecordCountSpanSampled(int increment = 1)
    {
        Interlocked.Add(ref _buffer.Counts[193].Value, increment);
    }

    public void RecordCountSpanDropped(Datadog.Trace.Telemetry.Metrics.MetricTags.DropReason tag, int increment = 1)
    {
        var index = 194 + (int)tag;
        Interlocked.Add(ref _buffer.Counts[index].Value, increment);
    }

    public void RecordCountTraceCreated(Datadog.Trace.Telemetry.Metrics.MetricTags.TraceContinuation tag, int increment = 1)
    {
        var index = 199 + (int)tag;
        Interlocked.Add(ref _buffer.Counts[index].Value, increment);
    }

    public void RecordCountTraceEnqueued(int increment = 1)
    {
        Interlocked.Add(ref _buffer.Counts[201].Value, increment);
    }

    public void RecordCountTraceSampled(int increment = 1)
    {
        Interlocked.Add(ref _buffer.Counts[202].Value, increment);
    }

    public void RecordCountTraceDropped(Datadog.Trace.Telemetry.Metrics.MetricTags.DropReason tag, int increment = 1)
    {
        var index = 203 + (int)tag;
        Interlocked.Add(ref _buffer.Counts[index].Value, increment);
    }

    public void RecordCountTraceSent(int increment = 1)
    {
        Interlocked.Add(ref _buffer.Counts[208].Value, increment);
    }

    public void RecordCountTraceApiRequests(int increment = 1)
    {
        Interlocked.Add(ref _buffer.Counts[209].Value, increment);
    }

    public void RecordCountTraceApiResponses(Datadog.Trace.Telemetry.Metrics.MetricTags.StatusCode tag, int increment = 1)
    {
        var index = 210 + (int)tag;
        Interlocked.Add(ref _buffer.Counts[index].Value, increment);
    }

    public void RecordCountTraceApiErrors(Datadog.Trace.Telemetry.Metrics.MetricTags.ApiError tag, int increment = 1)
    {
        var index = 232 + (int)tag;
        Interlocked.Add(ref _buffer.Counts[index].Value, increment);
    }

    public void RecordCountTracePartialFlush(Datadog.Trace.Telemetry.Metrics.MetricTags.PartialFlushReason tag, int increment = 1)
    {
        var index = 235 + (int)tag;
        Interlocked.Add(ref _buffer.Counts[index].Value, increment);
    }

    public void RecordCountContextHeaderStyleInjected(Datadog.Trace.Telemetry.Metrics.MetricTags.ContextHeaderStyle tag, int increment = 1)
    {
        var index = 237 + (int)tag;
        Interlocked.Add(ref _buffer.Counts[index].Value, increment);
    }

    public void RecordCountContextHeaderStyleExtracted(Datadog.Trace.Telemetry.Metrics.MetricTags.ContextHeaderStyle tag, int increment = 1)
    {
        var index = 241 + (int)tag;
        Interlocked.Add(ref _buffer.Counts[index].Value, increment);
    }

    public void RecordCountStatsApiRequests(int increment = 1)
    {
        Interlocked.Add(ref _buffer.Counts[245].Value, increment);
    }

    public void RecordCountStatsApiResponses(Datadog.Trace.Telemetry.Metrics.MetricTags.StatusCode tag, int increment = 1)
    {
        var index = 246 + (int)tag;
        Interlocked.Add(ref _buffer.Counts[index].Value, increment);
    }

    public void RecordCountStatsApiErrors(Datadog.Trace.Telemetry.Metrics.MetricTags.ApiError tag, int increment = 1)
    {
        var index = 268 + (int)tag;
        Interlocked.Add(ref _buffer.Counts[index].Value, increment);
    }

    public void RecordCountTelemetryApiRequests(Datadog.Trace.Telemetry.Metrics.MetricTags.TelemetryEndpoint tag, int increment = 1)
    {
        var index = 271 + (int)tag;
        Interlocked.Add(ref _buffer.Counts[index].Value, increment);
    }

    public void RecordCountTelemetryApiResponses(Datadog.Trace.Telemetry.Metrics.MetricTags.TelemetryEndpoint tag1, Datadog.Trace.Telemetry.Metrics.MetricTags.StatusCode tag2, int increment = 1)
    {
        var index = 273 + ((int)tag1 * 22) + (int)tag2;
        Interlocked.Add(ref _buffer.Counts[index].Value, increment);
    }

    public void RecordCountTelemetryApiErrors(Datadog.Trace.Telemetry.Metrics.MetricTags.TelemetryEndpoint tag1, Datadog.Trace.Telemetry.Metrics.MetricTags.ApiError tag2, int increment = 1)
    {
        var index = 317 + ((int)tag1 * 3) + (int)tag2;
        Interlocked.Add(ref _buffer.Counts[index].Value, increment);
    }

    public void RecordCountVersionConflictTracerCreated(int increment = 1)
    {
        Interlocked.Add(ref _buffer.Counts[323].Value, increment);
    }

    public void RecordCountDirectLogLogs(Datadog.Trace.Telemetry.Metrics.MetricTags.IntegrationName tag, int increment = 1)
    {
        var index = 324 + (int)tag;
        Interlocked.Add(ref _buffer.Counts[index].Value, increment);
    }

    public void RecordCountDirectLogApiRequests(int increment = 1)
    {
        Interlocked.Add(ref _buffer.Counts[371].Value, increment);
    }

    public void RecordCountDirectLogApiResponses(Datadog.Trace.Telemetry.Metrics.MetricTags.StatusCode tag, int increment = 1)
    {
        var index = 372 + (int)tag;
        Interlocked.Add(ref _buffer.Counts[index].Value, increment);
    }

    public void RecordCountDirectLogApiErrors(Datadog.Trace.Telemetry.Metrics.MetricTags.ApiError tag, int increment = 1)
    {
        var index = 394 + (int)tag;
        Interlocked.Add(ref _buffer.Counts[index].Value, increment);
    }

    /// <summary>
    /// Creates the buffer for the <see cref="Datadog.Trace.Telemetry.Metrics.Count" /> values.
    /// </summary>
    private static MetricKey[] GetCountBuffer()
        => new MetricKey[]
        {
            // log_created, index = 0
            new(new[] { "level:debug" }),
            new(new[] { "level:info" }),
            new(new[] { "level:warn" }),
            new(new[] { "level:error" }),
            // integrations_error, index = 4
            new(new[] { "integrations_name:httpmessagehandler", "error_type:duck_typing" }),
            new(new[] { "integrations_name:httpmessagehandler", "error_type:invoker" }),
            new(new[] { "integrations_name:httpmessagehandler", "error_type:execution" }),
            new(new[] { "integrations_name:httpsocketshandler", "error_type:duck_typing" }),
            new(new[] { "integrations_name:httpsocketshandler", "error_type:invoker" }),
            new(new[] { "integrations_name:httpsocketshandler", "error_type:execution" }),
            new(new[] { "integrations_name:winhttphandler", "error_type:duck_typing" }),
            new(new[] { "integrations_name:winhttphandler", "error_type:invoker" }),
            new(new[] { "integrations_name:winhttphandler", "error_type:execution" }),
            new(new[] { "integrations_name:curlhandler", "error_type:duck_typing" }),
            new(new[] { "integrations_name:curlhandler", "error_type:invoker" }),
            new(new[] { "integrations_name:curlhandler", "error_type:execution" }),
            new(new[] { "integrations_name:aspnetcore", "error_type:duck_typing" }),
            new(new[] { "integrations_name:aspnetcore", "error_type:invoker" }),
            new(new[] { "integrations_name:aspnetcore", "error_type:execution" }),
            new(new[] { "integrations_name:adonet", "error_type:duck_typing" }),
            new(new[] { "integrations_name:adonet", "error_type:invoker" }),
            new(new[] { "integrations_name:adonet", "error_type:execution" }),
            new(new[] { "integrations_name:aspnet", "error_type:duck_typing" }),
            new(new[] { "integrations_name:aspnet", "error_type:invoker" }),
            new(new[] { "integrations_name:aspnet", "error_type:execution" }),
            new(new[] { "integrations_name:aspnetmvc", "error_type:duck_typing" }),
            new(new[] { "integrations_name:aspnetmvc", "error_type:invoker" }),
            new(new[] { "integrations_name:aspnetmvc", "error_type:execution" }),
            new(new[] { "integrations_name:aspnetwebapi2", "error_type:duck_typing" }),
            new(new[] { "integrations_name:aspnetwebapi2", "error_type:invoker" }),
            new(new[] { "integrations_name:aspnetwebapi2", "error_type:execution" }),
            new(new[] { "integrations_name:graphql", "error_type:duck_typing" }),
            new(new[] { "integrations_name:graphql", "error_type:invoker" }),
            new(new[] { "integrations_name:graphql", "error_type:execution" }),
            new(new[] { "integrations_name:hotchocolate", "error_type:duck_typing" }),
            new(new[] { "integrations_name:hotchocolate", "error_type:invoker" }),
            new(new[] { "integrations_name:hotchocolate", "error_type:execution" }),
            new(new[] { "integrations_name:mongodb", "error_type:duck_typing" }),
            new(new[] { "integrations_name:mongodb", "error_type:invoker" }),
            new(new[] { "integrations_name:mongodb", "error_type:execution" }),
            new(new[] { "integrations_name:xunit", "error_type:duck_typing" }),
            new(new[] { "integrations_name:xunit", "error_type:invoker" }),
            new(new[] { "integrations_name:xunit", "error_type:execution" }),
            new(new[] { "integrations_name:nunit", "error_type:duck_typing" }),
            new(new[] { "integrations_name:nunit", "error_type:invoker" }),
            new(new[] { "integrations_name:nunit", "error_type:execution" }),
            new(new[] { "integrations_name:mstestv2", "error_type:duck_typing" }),
            new(new[] { "integrations_name:mstestv2", "error_type:invoker" }),
            new(new[] { "integrations_name:mstestv2", "error_type:execution" }),
            new(new[] { "integrations_name:wcf", "error_type:duck_typing" }),
            new(new[] { "integrations_name:wcf", "error_type:invoker" }),
            new(new[] { "integrations_name:wcf", "error_type:execution" }),
            new(new[] { "integrations_name:webrequest", "error_type:duck_typing" }),
            new(new[] { "integrations_name:webrequest", "error_type:invoker" }),
            new(new[] { "integrations_name:webrequest", "error_type:execution" }),
            new(new[] { "integrations_name:elasticsearchnet", "error_type:duck_typing" }),
            new(new[] { "integrations_name:elasticsearchnet", "error_type:invoker" }),
            new(new[] { "integrations_name:elasticsearchnet", "error_type:execution" }),
            new(new[] { "integrations_name:servicestackredis", "error_type:duck_typing" }),
            new(new[] { "integrations_name:servicestackredis", "error_type:invoker" }),
            new(new[] { "integrations_name:servicestackredis", "error_type:execution" }),
            new(new[] { "integrations_name:stackexchangeredis", "error_type:duck_typing" }),
            new(new[] { "integrations_name:stackexchangeredis", "error_type:invoker" }),
            new(new[] { "integrations_name:stackexchangeredis", "error_type:execution" }),
            new(new[] { "integrations_name:serviceremoting", "error_type:duck_typing" }),
            new(new[] { "integrations_name:serviceremoting", "error_type:invoker" }),
            new(new[] { "integrations_name:serviceremoting", "error_type:execution" }),
            new(new[] { "integrations_name:rabbitmq", "error_type:duck_typing" }),
            new(new[] { "integrations_name:rabbitmq", "error_type:invoker" }),
            new(new[] { "integrations_name:rabbitmq", "error_type:execution" }),
            new(new[] { "integrations_name:msmq", "error_type:duck_typing" }),
            new(new[] { "integrations_name:msmq", "error_type:invoker" }),
            new(new[] { "integrations_name:msmq", "error_type:execution" }),
            new(new[] { "integrations_name:kafka", "error_type:duck_typing" }),
            new(new[] { "integrations_name:kafka", "error_type:invoker" }),
            new(new[] { "integrations_name:kafka", "error_type:execution" }),
            new(new[] { "integrations_name:cosmosdb", "error_type:duck_typing" }),
            new(new[] { "integrations_name:cosmosdb", "error_type:invoker" }),
            new(new[] { "integrations_name:cosmosdb", "error_type:execution" }),
            new(new[] { "integrations_name:awssdk", "error_type:duck_typing" }),
            new(new[] { "integrations_name:awssdk", "error_type:invoker" }),
            new(new[] { "integrations_name:awssdk", "error_type:execution" }),
            new(new[] { "integrations_name:awssqs", "error_type:duck_typing" }),
            new(new[] { "integrations_name:awssqs", "error_type:invoker" }),
            new(new[] { "integrations_name:awssqs", "error_type:execution" }),
            new(new[] { "integrations_name:ilogger", "error_type:duck_typing" }),
            new(new[] { "integrations_name:ilogger", "error_type:invoker" }),
            new(new[] { "integrations_name:ilogger", "error_type:execution" }),
            new(new[] { "integrations_name:aerospike", "error_type:duck_typing" }),
            new(new[] { "integrations_name:aerospike", "error_type:invoker" }),
            new(new[] { "integrations_name:aerospike", "error_type:execution" }),
            new(new[] { "integrations_name:azurefunctions", "error_type:duck_typing" }),
            new(new[] { "integrations_name:azurefunctions", "error_type:invoker" }),
            new(new[] { "integrations_name:azurefunctions", "error_type:execution" }),
            new(new[] { "integrations_name:couchbase", "error_type:duck_typing" }),
            new(new[] { "integrations_name:couchbase", "error_type:invoker" }),
            new(new[] { "integrations_name:couchbase", "error_type:execution" }),
            new(new[] { "integrations_name:mysql", "error_type:duck_typing" }),
            new(new[] { "integrations_name:mysql", "error_type:invoker" }),
            new(new[] { "integrations_name:mysql", "error_type:execution" }),
            new(new[] { "integrations_name:npgsql", "error_type:duck_typing" }),
            new(new[] { "integrations_name:npgsql", "error_type:invoker" }),
            new(new[] { "integrations_name:npgsql", "error_type:execution" }),
            new(new[] { "integrations_name:oracle", "error_type:duck_typing" }),
            new(new[] { "integrations_name:oracle", "error_type:invoker" }),
            new(new[] { "integrations_name:oracle", "error_type:execution" }),
            new(new[] { "integrations_name:sqlclient", "error_type:duck_typing" }),
            new(new[] { "integrations_name:sqlclient", "error_type:invoker" }),
            new(new[] { "integrations_name:sqlclient", "error_type:execution" }),
            new(new[] { "integrations_name:sqlite", "error_type:duck_typing" }),
            new(new[] { "integrations_name:sqlite", "error_type:invoker" }),
            new(new[] { "integrations_name:sqlite", "error_type:execution" }),
            new(new[] { "integrations_name:serilog", "error_type:duck_typing" }),
            new(new[] { "integrations_name:serilog", "error_type:invoker" }),
            new(new[] { "integrations_name:serilog", "error_type:execution" }),
            new(new[] { "integrations_name:log4net", "error_type:duck_typing" }),
            new(new[] { "integrations_name:log4net", "error_type:invoker" }),
            new(new[] { "integrations_name:log4net", "error_type:execution" }),
            new(new[] { "integrations_name:nlog", "error_type:duck_typing" }),
            new(new[] { "integrations_name:nlog", "error_type:invoker" }),
            new(new[] { "integrations_name:nlog", "error_type:execution" }),
            new(new[] { "integrations_name:traceannotations", "error_type:duck_typing" }),
            new(new[] { "integrations_name:traceannotations", "error_type:invoker" }),
            new(new[] { "integrations_name:traceannotations", "error_type:execution" }),
            new(new[] { "integrations_name:grpc", "error_type:duck_typing" }),
            new(new[] { "integrations_name:grpc", "error_type:invoker" }),
            new(new[] { "integrations_name:grpc", "error_type:execution" }),
            new(new[] { "integrations_name:process", "error_type:duck_typing" }),
            new(new[] { "integrations_name:process", "error_type:invoker" }),
            new(new[] { "integrations_name:process", "error_type:execution" }),
            new(new[] { "integrations_name:hashalgorithm", "error_type:duck_typing" }),
            new(new[] { "integrations_name:hashalgorithm", "error_type:invoker" }),
            new(new[] { "integrations_name:hashalgorithm", "error_type:execution" }),
            new(new[] { "integrations_name:symmetricalgorithm", "error_type:duck_typing" }),
            new(new[] { "integrations_name:symmetricalgorithm", "error_type:invoker" }),
            new(new[] { "integrations_name:symmetricalgorithm", "error_type:execution" }),
            new(new[] { "integrations_name:opentelemetry", "error_type:duck_typing" }),
            new(new[] { "integrations_name:opentelemetry", "error_type:invoker" }),
            new(new[] { "integrations_name:opentelemetry", "error_type:execution" }),
            new(new[] { "integrations_name:pathtraversal", "error_type:duck_typing" }),
            new(new[] { "integrations_name:pathtraversal", "error_type:invoker" }),
            new(new[] { "integrations_name:pathtraversal", "error_type:execution" }),
            new(new[] { "integrations_name:aws_lambda", "error_type:duck_typing" }),
            new(new[] { "integrations_name:aws_lambda", "error_type:invoker" }),
            new(new[] { "integrations_name:aws_lambda", "error_type:execution" }),
            // span_created, index = 145
            new(new[] { "integrations_name:httpmessagehandler" }),
            new(new[] { "integrations_name:httpsocketshandler" }),
            new(new[] { "integrations_name:winhttphandler" }),
            new(new[] { "integrations_name:curlhandler" }),
            new(new[] { "integrations_name:aspnetcore" }),
            new(new[] { "integrations_name:adonet" }),
            new(new[] { "integrations_name:aspnet" }),
            new(new[] { "integrations_name:aspnetmvc" }),
            new(new[] { "integrations_name:aspnetwebapi2" }),
            new(new[] { "integrations_name:graphql" }),
            new(new[] { "integrations_name:hotchocolate" }),
            new(new[] { "integrations_name:mongodb" }),
            new(new[] { "integrations_name:xunit" }),
            new(new[] { "integrations_name:nunit" }),
            new(new[] { "integrations_name:mstestv2" }),
            new(new[] { "integrations_name:wcf" }),
            new(new[] { "integrations_name:webrequest" }),
            new(new[] { "integrations_name:elasticsearchnet" }),
            new(new[] { "integrations_name:servicestackredis" }),
            new(new[] { "integrations_name:stackexchangeredis" }),
            new(new[] { "integrations_name:serviceremoting" }),
            new(new[] { "integrations_name:rabbitmq" }),
            new(new[] { "integrations_name:msmq" }),
            new(new[] { "integrations_name:kafka" }),
            new(new[] { "integrations_name:cosmosdb" }),
            new(new[] { "integrations_name:awssdk" }),
            new(new[] { "integrations_name:awssqs" }),
            new(new[] { "integrations_name:ilogger" }),
            new(new[] { "integrations_name:aerospike" }),
            new(new[] { "integrations_name:azurefunctions" }),
            new(new[] { "integrations_name:couchbase" }),
            new(new[] { "integrations_name:mysql" }),
            new(new[] { "integrations_name:npgsql" }),
            new(new[] { "integrations_name:oracle" }),
            new(new[] { "integrations_name:sqlclient" }),
            new(new[] { "integrations_name:sqlite" }),
            new(new[] { "integrations_name:serilog" }),
            new(new[] { "integrations_name:log4net" }),
            new(new[] { "integrations_name:nlog" }),
            new(new[] { "integrations_name:traceannotations" }),
            new(new[] { "integrations_name:grpc" }),
            new(new[] { "integrations_name:process" }),
            new(new[] { "integrations_name:hashalgorithm" }),
            new(new[] { "integrations_name:symmetricalgorithm" }),
            new(new[] { "integrations_name:opentelemetry" }),
            new(new[] { "integrations_name:pathtraversal" }),
            new(new[] { "integrations_name:aws_lambda" }),
            // span_finished, index = 192
            new(null),
            // span_sampled, index = 193
            new(null),
            // span_dropped, index = 194
            new(new[] { "reason:sampling_decision" }),
            new(new[] { "reason:single_span_sampling" }),
            new(new[] { "reason:overfull_buffer" }),
            new(new[] { "reason:serialization_error" }),
            new(new[] { "reason:api_error" }),
            // trace_created, index = 199
            new(new[] { "new_continued:new" }),
            new(new[] { "new_continued:continued" }),
            // trace_enqueued, index = 201
            new(null),
            // trace_sampled, index = 202
            new(null),
            // trace_dropped, index = 203
            new(new[] { "reason:sampling_decision" }),
            new(new[] { "reason:single_span_sampling" }),
            new(new[] { "reason:overfull_buffer" }),
            new(new[] { "reason:serialization_error" }),
            new(new[] { "reason:api_error" }),
            // trace_sent, index = 208
            new(null),
            // trace_api.requests, index = 209
            new(null),
            // trace_api.responses, index = 210
            new(new[] { "status_code:200" }),
            new(new[] { "status_code:201" }),
            new(new[] { "status_code:202" }),
            new(new[] { "status_code:204" }),
            new(new[] { "status_code:2xx" }),
            new(new[] { "status_code:301" }),
            new(new[] { "status_code:302" }),
            new(new[] { "status_code:307" }),
            new(new[] { "status_code:308" }),
            new(new[] { "status_code:3xx" }),
            new(new[] { "status_code:400" }),
            new(new[] { "status_code:401" }),
            new(new[] { "status_code:403" }),
            new(new[] { "status_code:404" }),
            new(new[] { "status_code:405" }),
            new(new[] { "status_code:4xx" }),
            new(new[] { "status_code:500" }),
            new(new[] { "status_code:501" }),
            new(new[] { "status_code:502" }),
            new(new[] { "status_code:503" }),
            new(new[] { "status_code:504" }),
            new(new[] { "status_code:5xx" }),
            // trace_api.errors, index = 232
            new(new[] { "type:timeout" }),
            new(new[] { "type:network_error" }),
            new(new[] { "type:status_code" }),
            // trace_partial_flush, index = 235
            new(new[] { "reason:large_trace" }),
            new(new[] { "reason:single_span_ingestion" }),
            // context_header_style.injected, index = 237
            new(new[] { "header_style:tracecontext" }),
            new(new[] { "header_style:datadog" }),
            new(new[] { "header_style:b3multi" }),
            new(new[] { "header_style:b3single" }),
            // context_header_style.extracted, index = 241
            new(new[] { "header_style:tracecontext" }),
            new(new[] { "header_style:datadog" }),
            new(new[] { "header_style:b3multi" }),
            new(new[] { "header_style:b3single" }),
            // stats_api.requests, index = 245
            new(null),
            // stats_api.responses, index = 246
            new(new[] { "status_code:200" }),
            new(new[] { "status_code:201" }),
            new(new[] { "status_code:202" }),
            new(new[] { "status_code:204" }),
            new(new[] { "status_code:2xx" }),
            new(new[] { "status_code:301" }),
            new(new[] { "status_code:302" }),
            new(new[] { "status_code:307" }),
            new(new[] { "status_code:308" }),
            new(new[] { "status_code:3xx" }),
            new(new[] { "status_code:400" }),
            new(new[] { "status_code:401" }),
            new(new[] { "status_code:403" }),
            new(new[] { "status_code:404" }),
            new(new[] { "status_code:405" }),
            new(new[] { "status_code:4xx" }),
            new(new[] { "status_code:500" }),
            new(new[] { "status_code:501" }),
            new(new[] { "status_code:502" }),
            new(new[] { "status_code:503" }),
            new(new[] { "status_code:504" }),
            new(new[] { "status_code:5xx" }),
            // stats_api.errors, index = 268
            new(new[] { "type:timeout" }),
            new(new[] { "type:network_error" }),
            new(new[] { "type:status_code" }),
            // telemetry_api.requests, index = 271
            new(new[] { "endpoint:agent" }),
            new(new[] { "endpoint:agentless" }),
            // telemetry_api.responses, index = 273
            new(new[] { "endpoint:agent", "status_code:200" }),
            new(new[] { "endpoint:agent", "status_code:201" }),
            new(new[] { "endpoint:agent", "status_code:202" }),
            new(new[] { "endpoint:agent", "status_code:204" }),
            new(new[] { "endpoint:agent", "status_code:2xx" }),
            new(new[] { "endpoint:agent", "status_code:301" }),
            new(new[] { "endpoint:agent", "status_code:302" }),
            new(new[] { "endpoint:agent", "status_code:307" }),
            new(new[] { "endpoint:agent", "status_code:308" }),
            new(new[] { "endpoint:agent", "status_code:3xx" }),
            new(new[] { "endpoint:agent", "status_code:400" }),
            new(new[] { "endpoint:agent", "status_code:401" }),
            new(new[] { "endpoint:agent", "status_code:403" }),
            new(new[] { "endpoint:agent", "status_code:404" }),
            new(new[] { "endpoint:agent", "status_code:405" }),
            new(new[] { "endpoint:agent", "status_code:4xx" }),
            new(new[] { "endpoint:agent", "status_code:500" }),
            new(new[] { "endpoint:agent", "status_code:501" }),
            new(new[] { "endpoint:agent", "status_code:502" }),
            new(new[] { "endpoint:agent", "status_code:503" }),
            new(new[] { "endpoint:agent", "status_code:504" }),
            new(new[] { "endpoint:agent", "status_code:5xx" }),
            new(new[] { "endpoint:agentless", "status_code:200" }),
            new(new[] { "endpoint:agentless", "status_code:201" }),
            new(new[] { "endpoint:agentless", "status_code:202" }),
            new(new[] { "endpoint:agentless", "status_code:204" }),
            new(new[] { "endpoint:agentless", "status_code:2xx" }),
            new(new[] { "endpoint:agentless", "status_code:301" }),
            new(new[] { "endpoint:agentless", "status_code:302" }),
            new(new[] { "endpoint:agentless", "status_code:307" }),
            new(new[] { "endpoint:agentless", "status_code:308" }),
            new(new[] { "endpoint:agentless", "status_code:3xx" }),
            new(new[] { "endpoint:agentless", "status_code:400" }),
            new(new[] { "endpoint:agentless", "status_code:401" }),
            new(new[] { "endpoint:agentless", "status_code:403" }),
            new(new[] { "endpoint:agentless", "status_code:404" }),
            new(new[] { "endpoint:agentless", "status_code:405" }),
            new(new[] { "endpoint:agentless", "status_code:4xx" }),
            new(new[] { "endpoint:agentless", "status_code:500" }),
            new(new[] { "endpoint:agentless", "status_code:501" }),
            new(new[] { "endpoint:agentless", "status_code:502" }),
            new(new[] { "endpoint:agentless", "status_code:503" }),
            new(new[] { "endpoint:agentless", "status_code:504" }),
            new(new[] { "endpoint:agentless", "status_code:5xx" }),
            // telemetry_api.errors, index = 317
            new(new[] { "endpoint:agent", "type:timeout" }),
            new(new[] { "endpoint:agent", "type:network_error" }),
            new(new[] { "endpoint:agent", "type:status_code" }),
            new(new[] { "endpoint:agentless", "type:timeout" }),
            new(new[] { "endpoint:agentless", "type:network_error" }),
            new(new[] { "endpoint:agentless", "type:status_code" }),
            // version_conflict_tracer_created, index = 323
            new(null),
            // direct_log_logs, index = 324
            new(new[] { "integrations_name:httpmessagehandler" }),
            new(new[] { "integrations_name:httpsocketshandler" }),
            new(new[] { "integrations_name:winhttphandler" }),
            new(new[] { "integrations_name:curlhandler" }),
            new(new[] { "integrations_name:aspnetcore" }),
            new(new[] { "integrations_name:adonet" }),
            new(new[] { "integrations_name:aspnet" }),
            new(new[] { "integrations_name:aspnetmvc" }),
            new(new[] { "integrations_name:aspnetwebapi2" }),
            new(new[] { "integrations_name:graphql" }),
            new(new[] { "integrations_name:hotchocolate" }),
            new(new[] { "integrations_name:mongodb" }),
            new(new[] { "integrations_name:xunit" }),
            new(new[] { "integrations_name:nunit" }),
            new(new[] { "integrations_name:mstestv2" }),
            new(new[] { "integrations_name:wcf" }),
            new(new[] { "integrations_name:webrequest" }),
            new(new[] { "integrations_name:elasticsearchnet" }),
            new(new[] { "integrations_name:servicestackredis" }),
            new(new[] { "integrations_name:stackexchangeredis" }),
            new(new[] { "integrations_name:serviceremoting" }),
            new(new[] { "integrations_name:rabbitmq" }),
            new(new[] { "integrations_name:msmq" }),
            new(new[] { "integrations_name:kafka" }),
            new(new[] { "integrations_name:cosmosdb" }),
            new(new[] { "integrations_name:awssdk" }),
            new(new[] { "integrations_name:awssqs" }),
            new(new[] { "integrations_name:ilogger" }),
            new(new[] { "integrations_name:aerospike" }),
            new(new[] { "integrations_name:azurefunctions" }),
            new(new[] { "integrations_name:couchbase" }),
            new(new[] { "integrations_name:mysql" }),
            new(new[] { "integrations_name:npgsql" }),
            new(new[] { "integrations_name:oracle" }),
            new(new[] { "integrations_name:sqlclient" }),
            new(new[] { "integrations_name:sqlite" }),
            new(new[] { "integrations_name:serilog" }),
            new(new[] { "integrations_name:log4net" }),
            new(new[] { "integrations_name:nlog" }),
            new(new[] { "integrations_name:traceannotations" }),
            new(new[] { "integrations_name:grpc" }),
            new(new[] { "integrations_name:process" }),
            new(new[] { "integrations_name:hashalgorithm" }),
            new(new[] { "integrations_name:symmetricalgorithm" }),
            new(new[] { "integrations_name:opentelemetry" }),
            new(new[] { "integrations_name:pathtraversal" }),
            new(new[] { "integrations_name:aws_lambda" }),
            // direct_log_api.requests, index = 371
            new(null),
            // direct_log_api.responses, index = 372
            new(new[] { "status_code:200" }),
            new(new[] { "status_code:201" }),
            new(new[] { "status_code:202" }),
            new(new[] { "status_code:204" }),
            new(new[] { "status_code:2xx" }),
            new(new[] { "status_code:301" }),
            new(new[] { "status_code:302" }),
            new(new[] { "status_code:307" }),
            new(new[] { "status_code:308" }),
            new(new[] { "status_code:3xx" }),
            new(new[] { "status_code:400" }),
            new(new[] { "status_code:401" }),
            new(new[] { "status_code:403" }),
            new(new[] { "status_code:404" }),
            new(new[] { "status_code:405" }),
            new(new[] { "status_code:4xx" }),
            new(new[] { "status_code:500" }),
            new(new[] { "status_code:501" }),
            new(new[] { "status_code:502" }),
            new(new[] { "status_code:503" }),
            new(new[] { "status_code:504" }),
            new(new[] { "status_code:5xx" }),
            // direct_log_api.errors.responses, index = 394
            new(new[] { "type:timeout" }),
            new(new[] { "type:network_error" }),
            new(new[] { "type:status_code" }),
        };

    /// <summary>
    /// Gets an array of metric counts, indexed by integer value of the <see cref="Datadog.Trace.Telemetry.Metrics.Count" />.
    /// Each value represents the number of unique entries in the buffer returned by <see cref="GetCountBuffer()" />
    /// It is equal to the cardinality of the tag combinations (or 1 if there are no tags)
    /// </summary>
    private static int[] CountEntryCounts { get; }
        = new []{ 4, 141, 47, 1, 1, 5, 2, 1, 1, 5, 1, 1, 22, 3, 2, 4, 4, 1, 22, 3, 2, 44, 6, 1, 47, 1, 22, 3, };
}