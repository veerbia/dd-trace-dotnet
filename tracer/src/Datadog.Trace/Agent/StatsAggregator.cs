// <copyright file="StatsAggregator.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Datadog.Trace.Agent.DiscoveryService;
using Datadog.Trace.Agent.Native;
using Datadog.Trace.Agent.TraceSamplers;
using Datadog.Trace.Configuration;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Logging;
using Datadog.Trace.PlatformHelpers;
using Datadog.Trace.Processors;
using Datadog.Trace.Telemetry;
using Datadog.Trace.Telemetry.Metrics;
using Datadog.Trace.Util;

namespace Datadog.Trace.Agent
{
    internal class StatsAggregator : IStatsAggregator
    {
        private const int BufferCount = 2;

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<StatsAggregator>();

        private readonly StatsBuffer[] _buffers;

        private readonly IApi _api;
        private readonly ITraceProcessor[] _traceProcessors;

        private readonly TaskCompletionSource<bool> _processExit;

        private readonly TimeSpan _bucketDuration;
        private readonly Task _flushTask;
        private readonly StatsDiscovery _statsDiscovery;

        private int _currentBuffer;

        internal StatsAggregator(IApi api, ImmutableTracerSettings settings, IDiscoveryService discoveryService)
        {
            _statsDiscovery = new StatsDiscovery(discoveryService);
            _api = api;
            _processExit = new TaskCompletionSource<bool>();
            _bucketDuration = TimeSpan.FromSeconds(settings.StatsComputationInterval);
            _buffers = new StatsBuffer[BufferCount];
            _traceProcessors = new ITraceProcessor[]
            {
                new Processors.NormalizerTraceProcessor(),
                new Processors.ObfuscatorTraceProcessor(false),
            };

            var header = new ClientStatsPayload
            {
                Environment = settings.EnvironmentInternal,
                Version = settings.ServiceVersionInternal,
                HostName = HostMetadata.Instance.Hostname
            };

            for (int i = 0; i < _buffers.Length; i++)
            {
                _buffers[i] = new(header);
            }

            _flushTask = Task.Run(Flush);
            _flushTask.ContinueWith(t => Log.Error(t.Exception, "Error in StatsAggregator"), TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Gets the current buffer.
        /// StatsBuffer is not thread-safe, this property is not intended to be used outside of the class,
        /// except for tests.
        /// </summary>
        internal StatsBuffer CurrentBuffer => _buffers[_currentBuffer];

        public bool? CanComputeStats => _statsDiscovery.CanComputeStats;

        public static IStatsAggregator Create(IApi api, ImmutableTracerSettings settings, IDiscoveryService discoveryService)
        {
            return settings.StatsComputationEnabledInternal ? new StatsAggregator(api, settings, discoveryService) : new NullStatsAggregator();
            // return settings.StatsComputationEnabledInternal ? new NativeStatsAggregator(settings, discoveryService) : new NullStatsAggregator();
        }

        public Task DisposeAsync()
        {
            _statsDiscovery.Dispose();
            _processExit.TrySetResult(true);
            return _flushTask;
        }

        public void Add(params Span[] spans)
        {
            AddRange(new ArraySegment<Span>(spans, 0, spans.Length));
        }

        public void AddRange(ArraySegment<Span> spans)
        {
            // Contention around this lock is expected to be very small:
            // AddRange is called from the serialization thread, and concurrent serialization
            // of traces is a rare corner-case (happening only during shutdown).
            // The Flush thread only acquires the lock long enough to swap the metrics buffer.
            lock (_buffers)
            {
                for (int i = 0; i < spans.Count; i++)
                {
                    AddToBuffer(spans.Array[i + spans.Offset]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArraySegment<Span> ProcessTrace(ArraySegment<Span> trace)
        {
            foreach (var processor in _traceProcessors)
            {
                try
                {
                    trace = processor.Process(trace);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error executing trace processor {TraceProcessorType}", processor?.GetType());
                }
            }

            return trace;
        }

        internal static StatsAggregationKey BuildKey(Span span)
        {
            var rawHttpStatusCode = span.GetTag(Tags.HttpStatusCode);

            if (rawHttpStatusCode == null || !int.TryParse(rawHttpStatusCode, out var httpStatusCode))
            {
                httpStatusCode = 0;
            }

            return new StatsAggregationKey(
                span.ResourceName,
                span.ServiceName,
                span.OperationName,
                span.Type,
                httpStatusCode,
                span.Context.Origin == "synthetics");
        }

        internal async Task Flush()
        {
            // Use a do/while loop to still flush once if _processExit is already completed (this makes testing easier)
            do
            {
                if (CanComputeStats == false)
                {
                    // TODO: When we implement the feature to continuously poll the Agent Configuration,
                    // we may want to stay in this loop instead of returning
                    return;
                }

                await Task.WhenAny(_processExit.Task, Task.Delay(_bucketDuration)).ConfigureAwait(false);

                var buffer = CurrentBuffer;

                lock (_buffers)
                {
                    _currentBuffer = (_currentBuffer + 1) % BufferCount;
                }

                TelemetryFactory.Metrics.RecordGaugeStatsBuckets(buffer.Buckets.Count);

                if (buffer.Buckets.Count > 0)
                {
                    // Push the metrics
                    if (CanComputeStats == true)
                    {
                        await _api.SendStatsAsync(buffer, _bucketDuration.ToNanoseconds()).ConfigureAwait(false);
                    }

                    buffer.Reset();
                }
            }
            while (!_processExit.Task.IsCompleted);
        }

        /// <summary>
        /// Converts a nanosec timestamp into a float nanosecond timestamp truncated to a fixed precision.
        /// Span timestamps must have maximum precision, but we can reduce precision of timestamps for
        /// aggregated stats points to achieve more efficient data representation.
        /// </summary>
        /// <param name="ns">Timestamp to convert</param>
        /// <returns>Timestamp with truncated precision</returns>
        private static double ConvertTimestamp(long ns)
        {
            // 10 bits precision (any value will be +/- 1/1024)
            const long roundMask = 1 << 10;

            int shift = 0;

            while (ns > roundMask)
            {
                ns >>= 1;
                shift++;
            }

            return ns << shift;
        }

        private void AddToBuffer(Span span)
        {
            if ((!span.IsTopLevel && span.GetMetric(Tags.Measured) != 1.0) || span.GetMetric(Tags.PartialSnapshot) > 0)
            {
                return;
            }

            var key = BuildKey(span);

            var buffer = CurrentBuffer;

            if (!buffer.Buckets.TryGetValue(key, out var bucket))
            {
                bucket = new StatsBucket(key);
                buffer.Buckets.Add(key, bucket);
            }

            bucket.Hits++;

            if (span.IsTopLevel)
            {
                bucket.TopLevelHits++;
            }

            var duration = span.Duration.ToNanoseconds();

            bucket.Duration += duration;

            if (span.Error)
            {
                bucket.Errors++;
                bucket.ErrorSummary.Add(ConvertTimestamp(duration));
            }
            else
            {
                bucket.OkSummary.Add(ConvertTimestamp(duration));
            }
        }
    }
}
