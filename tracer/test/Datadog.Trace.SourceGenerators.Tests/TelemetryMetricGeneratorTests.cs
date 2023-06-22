﻿// <copyright file="TelemetryMetricGeneratorTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Datadog.Trace.SourceGenerators.TelemetryMetric;
using Datadog.Trace.SourceGenerators.TelemetryMetric.Diagnostics;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Datadog.Trace.SourceGenerators.Tests;

public class TelemetryMetricGeneratorTests
{
    [Fact] // edge case, not actually useful
    public void CanGenerateExtensionWithNoMembers()
    {
        const string input = """
            using Datadog.Trace.SourceGenerators;
            namespace MyTests.TestMetricNameSpace;

            [TelemetryMetricType("count")]
            public enum TestMetric
            { 
            }
            """;

        const string expectedEnum = """
            // <auto-generated/>
            #nullable enable

            namespace MyTests.TestMetricNameSpace;
            internal static partial class TestMetricExtensions
            {
                /// <summary>
                /// The number of separate metrics in the <see cref="MyTests.TestMetricNameSpace.TestMetric" /> metric.
                /// </summary>
                public const int Length = 0;

                /// <summary>
                /// Gets the metric name for the provided metric
                /// </summary>
                /// <param name="metric">The metric to get the name for</param>
                /// <returns>The datadog metric name</returns>
                public static string GetName(this MyTests.TestMetricNameSpace.TestMetric metric)
                    => metric switch
                    {
                        _ => null!,
                    };

                /// <summary>
                /// Gets whether the metric is a "common" metric, used by all tracers
                /// </summary>
                /// <param name="metric">The metric to check</param>
                /// <returns>True if the metric is a "common" metric, used by all languages</returns>
                public static bool IsCommon(this MyTests.TestMetricNameSpace.TestMetric metric)
                    => metric switch
                    {
                        _ => true,
                    };

                /// <summary>
                /// Gets the custom namespace for the provided metric
                /// </summary>
                /// <param name="metric">The metric to get the name for</param>
                /// <returns>The datadog metric name</returns>
                public static string? GetNamespace(this MyTests.TestMetricNameSpace.TestMetric metric)
                    => metric switch
                    {
                        _ => null,
                    };
            }
            """;

        const string expectedCollector = """
            // <auto-generated/>
            #nullable enable

            using System.Threading;

            namespace Datadog.Trace.Telemetry;
            internal partial class MetricsTelemetryCollector
            {
                // These can technically overflow, but it's _very_ unlikely as we reset every minute
                // Negative values are normalized during polling
                /// <summary>
                /// Creates the buffer for the <see cref="MyTests.TestMetricNameSpace.TestMetric" /> values.
                /// </summary>
                private static MetricKey[] GetTestMetricBuffer()
                    => new MetricKey[]
                    {
                    };

                /// <summary>
                /// Gets an array of metric counts, indexed by integer value of the <see cref="MyTests.TestMetricNameSpace.TestMetric" />.
                /// Each value represents the number of unique entries in the buffer returned by <see cref="GetTestMetricBuffer()" />
                /// It is equal to the cardinality of the tag combinations (or 1 if there are no tags)
                /// </summary>
                private static int[] TestMetricEntryCounts { get; }
                    = new []{ };
            }
            """;

        const string expectedInterface = """
            // <auto-generated/>
            #nullable enable

            namespace Datadog.Trace.Telemetry;
            internal partial interface IMetricsTelemetryCollector
            {}
            """;

        const string expectedNull = """
            // <auto-generated/>
            #nullable enable

            namespace Datadog.Trace.Telemetry;
            internal partial class NullMetricsTelemetryCollector
            {}
            """;
        var (diagnostics, trees) = TestHelpers.GetGeneratedTrees<TelemetryMetricGenerator>(input);
        using var scope = new AssertionScope();
        diagnostics.Should().BeEmpty();
        // tree 0 is the attributes
        trees[1].Should().Be(expectedEnum);
        trees[2].Should().Be(expectedCollector);
        trees[3].Should().Be(expectedInterface);
        trees[4].Should().Be(expectedNull);
    }

    [Fact]
    public void CanGenerateForCountMetrics()
    {
        const string input = """
            using Datadog.Trace.SourceGenerators;
            using System.ComponentModel;

            namespace MyTests.TestMetricNameSpace;

            [TelemetryMetricType("count")]
            public enum TestMetric
            { 
                [TelemetryMetric("metric.zero")]
                ZeroTagMetric,

                [TelemetryMetric<LogLevel>("metric.one")]
                OneTagMetric,

                [TelemetryMetric<LogLevel, ErrorType>("metric.two")]
                TwoTagMetric,

                [TelemetryMetric("metric.zeroagain")]
                ZeroAgainTagMetric,
            }

            public enum LogLevel
            {
                [Description("debug")] Debug,
                [Description("info")] Info,
                [Description("error")] Error,
            }

            public enum ErrorType
            {
                [Description("random")] Random,
                [Description("ducktyping")] DuckTyping,
            }
            """;

        const string expectedEnum = """
            // <auto-generated/>
            #nullable enable

            namespace MyTests.TestMetricNameSpace;
            internal static partial class TestMetricExtensions
            {
                /// <summary>
                /// The number of separate metrics in the <see cref="MyTests.TestMetricNameSpace.TestMetric" /> metric.
                /// </summary>
                public const int Length = 4;

                /// <summary>
                /// Gets the metric name for the provided metric
                /// </summary>
                /// <param name="metric">The metric to get the name for</param>
                /// <returns>The datadog metric name</returns>
                public static string GetName(this MyTests.TestMetricNameSpace.TestMetric metric)
                    => metric switch
                    {
                        MyTests.TestMetricNameSpace.TestMetric.ZeroTagMetric => "metric.zero",
                        MyTests.TestMetricNameSpace.TestMetric.OneTagMetric => "metric.one",
                        MyTests.TestMetricNameSpace.TestMetric.TwoTagMetric => "metric.two",
                        MyTests.TestMetricNameSpace.TestMetric.ZeroAgainTagMetric => "metric.zeroagain",
                        _ => null!,
                    };

                /// <summary>
                /// Gets whether the metric is a "common" metric, used by all tracers
                /// </summary>
                /// <param name="metric">The metric to check</param>
                /// <returns>True if the metric is a "common" metric, used by all languages</returns>
                public static bool IsCommon(this MyTests.TestMetricNameSpace.TestMetric metric)
                    => metric switch
                    {
                        _ => true,
                    };

                /// <summary>
                /// Gets the custom namespace for the provided metric
                /// </summary>
                /// <param name="metric">The metric to get the name for</param>
                /// <returns>The datadog metric name</returns>
                public static string? GetNamespace(this MyTests.TestMetricNameSpace.TestMetric metric)
                    => metric switch
                    {
                        _ => null,
                    };
            }
            """;

        const string expectedCollector = """
            // <auto-generated/>
            #nullable enable

            using System.Threading;

            namespace Datadog.Trace.Telemetry;
            internal partial class MetricsTelemetryCollector
            {
                // These can technically overflow, but it's _very_ unlikely as we reset every minute
                // Negative values are normalized during polling
                public void RecordTestMetricZeroTagMetric(int increment = 1)
                {
                    Interlocked.Add(ref _buffer.Counts[0].Value, increment);
                }

                public void RecordTestMetricOneTagMetric(MyTests.TestMetricNameSpace.LogLevel tag, int increment = 1)
                {
                    var index = 1 + (int)tag;
                    Interlocked.Add(ref _buffer.Counts[index].Value, increment);
                }

                public void RecordTestMetricTwoTagMetric(MyTests.TestMetricNameSpace.LogLevel tag1, MyTests.TestMetricNameSpace.ErrorType tag2, int increment = 1)
                {
                    var index = 4 + ((int)tag1 * 2) + (int)tag2;
                    Interlocked.Add(ref _buffer.Counts[index].Value, increment);
                }

                public void RecordTestMetricZeroAgainTagMetric(int increment = 1)
                {
                    Interlocked.Add(ref _buffer.Counts[10].Value, increment);
                }

                /// <summary>
                /// Creates the buffer for the <see cref="MyTests.TestMetricNameSpace.TestMetric" /> values.
                /// </summary>
                private static MetricKey[] GetTestMetricBuffer()
                    => new MetricKey[]
                    {
                        // metric.zero, index = 0
                        new(null),
                        // metric.one, index = 1
                        new(new[] { "debug" }),
                        new(new[] { "info" }),
                        new(new[] { "error" }),
                        // metric.two, index = 4
                        new(new[] { "debug", "random" }),
                        new(new[] { "debug", "ducktyping" }),
                        new(new[] { "info", "random" }),
                        new(new[] { "info", "ducktyping" }),
                        new(new[] { "error", "random" }),
                        new(new[] { "error", "ducktyping" }),
                        // metric.zeroagain, index = 10
                        new(null),
                    };

                /// <summary>
                /// Gets an array of metric counts, indexed by integer value of the <see cref="MyTests.TestMetricNameSpace.TestMetric" />.
                /// Each value represents the number of unique entries in the buffer returned by <see cref="GetTestMetricBuffer()" />
                /// It is equal to the cardinality of the tag combinations (or 1 if there are no tags)
                /// </summary>
                private static int[] TestMetricEntryCounts { get; }
                    = new []{ 1, 3, 6, 1, };
            }
            """;

        const string expectedInterface = """
            // <auto-generated/>
            #nullable enable

            namespace Datadog.Trace.Telemetry;
            internal partial interface IMetricsTelemetryCollector
            {
                public void RecordTestMetricZeroTagMetric(int increment = 1);

                public void RecordTestMetricOneTagMetric(MyTests.TestMetricNameSpace.LogLevel tag, int increment = 1);

                public void RecordTestMetricTwoTagMetric(MyTests.TestMetricNameSpace.LogLevel tag1, MyTests.TestMetricNameSpace.ErrorType tag2, int increment = 1);

                public void RecordTestMetricZeroAgainTagMetric(int increment = 1);
            }
            """;

        const string expectedNull = """
            // <auto-generated/>
            #nullable enable

            namespace Datadog.Trace.Telemetry;
            internal partial class NullMetricsTelemetryCollector
            {
                public void RecordTestMetricZeroTagMetric(int increment = 1)
                {
                }

                public void RecordTestMetricOneTagMetric(MyTests.TestMetricNameSpace.LogLevel tag, int increment = 1)
                {
                }

                public void RecordTestMetricTwoTagMetric(MyTests.TestMetricNameSpace.LogLevel tag1, MyTests.TestMetricNameSpace.ErrorType tag2, int increment = 1)
                {
                }

                public void RecordTestMetricZeroAgainTagMetric(int increment = 1)
                {
                }
            }
            """;
        var (diagnostics, trees) = TestHelpers.GetGeneratedTrees<TelemetryMetricGenerator>(input);
        using var scope = new AssertionScope();
        diagnostics.Should().BeEmpty();
        // tree 0 is the attributes
        trees[1].Should().Be(expectedEnum);
        trees[2].Should().Be(expectedCollector);
        trees[3].Should().Be(expectedInterface);
        trees[4].Should().Be(expectedNull);
    }

    [Fact]
    public void CanGenerateForGaugeMetrics()
    {
        const string input = """
            using Datadog.Trace.SourceGenerators;
            using System.ComponentModel;

            namespace MyTests.TestMetricNameSpace;

            [TelemetryMetricType("gauge")]
            public enum TestMetric
            { 
                [TelemetryMetric("metric.zero")]
                ZeroTagMetric,

                [TelemetryMetric<LogLevel>("metric.one")]
                OneTagMetric,

                [TelemetryMetric<LogLevel, ErrorType>("metric.two")]
                TwoTagMetric,

                [TelemetryMetric("metric.zeroagain")]
                ZeroAgainTagMetric,
            }

            public enum LogLevel
            {
                [Description("debug")] Debug,
                [Description("info")] Info,
                [Description("error")] Error,
            }

            public enum ErrorType
            {
                [Description("random")] Random,
                [Description("ducktyping")] DuckTyping,
            }
            """;

        const string expectedEnum = """
            // <auto-generated/>
            #nullable enable

            namespace MyTests.TestMetricNameSpace;
            internal static partial class TestMetricExtensions
            {
                /// <summary>
                /// The number of separate metrics in the <see cref="MyTests.TestMetricNameSpace.TestMetric" /> metric.
                /// </summary>
                public const int Length = 4;

                /// <summary>
                /// Gets the metric name for the provided metric
                /// </summary>
                /// <param name="metric">The metric to get the name for</param>
                /// <returns>The datadog metric name</returns>
                public static string GetName(this MyTests.TestMetricNameSpace.TestMetric metric)
                    => metric switch
                    {
                        MyTests.TestMetricNameSpace.TestMetric.ZeroTagMetric => "metric.zero",
                        MyTests.TestMetricNameSpace.TestMetric.OneTagMetric => "metric.one",
                        MyTests.TestMetricNameSpace.TestMetric.TwoTagMetric => "metric.two",
                        MyTests.TestMetricNameSpace.TestMetric.ZeroAgainTagMetric => "metric.zeroagain",
                        _ => null!,
                    };

                /// <summary>
                /// Gets whether the metric is a "common" metric, used by all tracers
                /// </summary>
                /// <param name="metric">The metric to check</param>
                /// <returns>True if the metric is a "common" metric, used by all languages</returns>
                public static bool IsCommon(this MyTests.TestMetricNameSpace.TestMetric metric)
                    => metric switch
                    {
                        _ => true,
                    };

                /// <summary>
                /// Gets the custom namespace for the provided metric
                /// </summary>
                /// <param name="metric">The metric to get the name for</param>
                /// <returns>The datadog metric name</returns>
                public static string? GetNamespace(this MyTests.TestMetricNameSpace.TestMetric metric)
                    => metric switch
                    {
                        _ => null,
                    };
            }
            """;

        const string expectedCollector = """
            // <auto-generated/>
            #nullable enable

            using System.Threading;

            namespace Datadog.Trace.Telemetry;
            internal partial class MetricsTelemetryCollector
            {
                public void RecordTestMetricZeroTagMetric(int value)
                {
                    Interlocked.Exchange(ref _buffer.Gauges[0].Value, value);
                }

                public void RecordTestMetricOneTagMetric(MyTests.TestMetricNameSpace.LogLevel tag, int value)
                {
                    var index = 1 + (int)tag;
                    Interlocked.Exchange(ref _buffer.Gauges[index].Value, value);
                }

                public void RecordTestMetricTwoTagMetric(MyTests.TestMetricNameSpace.LogLevel tag1, MyTests.TestMetricNameSpace.ErrorType tag2, int value)
                {
                    var index = 4 + ((int)tag1 * 2) + (int)tag2;
                    Interlocked.Exchange(ref _buffer.Gauges[index].Value, value);
                }

                public void RecordTestMetricZeroAgainTagMetric(int value)
                {
                    Interlocked.Exchange(ref _buffer.Gauges[10].Value, value);
                }

                /// <summary>
                /// Creates the buffer for the <see cref="MyTests.TestMetricNameSpace.TestMetric" /> values.
                /// </summary>
                private static MetricKey[] GetTestMetricBuffer()
                    => new MetricKey[]
                    {
                        // metric.zero, index = 0
                        new(null),
                        // metric.one, index = 1
                        new(new[] { "debug" }),
                        new(new[] { "info" }),
                        new(new[] { "error" }),
                        // metric.two, index = 4
                        new(new[] { "debug", "random" }),
                        new(new[] { "debug", "ducktyping" }),
                        new(new[] { "info", "random" }),
                        new(new[] { "info", "ducktyping" }),
                        new(new[] { "error", "random" }),
                        new(new[] { "error", "ducktyping" }),
                        // metric.zeroagain, index = 10
                        new(null),
                    };

                /// <summary>
                /// Gets an array of metric counts, indexed by integer value of the <see cref="MyTests.TestMetricNameSpace.TestMetric" />.
                /// Each value represents the number of unique entries in the buffer returned by <see cref="GetTestMetricBuffer()" />
                /// It is equal to the cardinality of the tag combinations (or 1 if there are no tags)
                /// </summary>
                private static int[] TestMetricEntryCounts { get; }
                    = new []{ 1, 3, 6, 1, };
            }
            """;

        const string expectedInterface = """
            // <auto-generated/>
            #nullable enable

            namespace Datadog.Trace.Telemetry;
            internal partial interface IMetricsTelemetryCollector
            {
                public void RecordTestMetricZeroTagMetric(int value);

                public void RecordTestMetricOneTagMetric(MyTests.TestMetricNameSpace.LogLevel tag, int value);

                public void RecordTestMetricTwoTagMetric(MyTests.TestMetricNameSpace.LogLevel tag1, MyTests.TestMetricNameSpace.ErrorType tag2, int value);

                public void RecordTestMetricZeroAgainTagMetric(int value);
            }
            """;

        const string expectedNull = """
            // <auto-generated/>
            #nullable enable

            namespace Datadog.Trace.Telemetry;
            internal partial class NullMetricsTelemetryCollector
            {
                public void RecordTestMetricZeroTagMetric(int value)
                {
                }

                public void RecordTestMetricOneTagMetric(MyTests.TestMetricNameSpace.LogLevel tag, int value)
                {
                }

                public void RecordTestMetricTwoTagMetric(MyTests.TestMetricNameSpace.LogLevel tag1, MyTests.TestMetricNameSpace.ErrorType tag2, int value)
                {
                }

                public void RecordTestMetricZeroAgainTagMetric(int value)
                {
                }
            }
            """;
        var (diagnostics, trees) = TestHelpers.GetGeneratedTrees<TelemetryMetricGenerator>(input);
        using var scope = new AssertionScope();
        diagnostics.Should().BeEmpty();
        // tree 0 is the attributes
        trees[1].Should().Be(expectedEnum);
        trees[2].Should().Be(expectedCollector);
        trees[3].Should().Be(expectedInterface);
        trees[4].Should().Be(expectedNull);
    }

    [Fact]
    public void CanGenerateForDistributionMetrics()
    {
        const string input = """
            using Datadog.Trace.SourceGenerators;
            using System.ComponentModel;

            namespace MyTests.TestMetricNameSpace;

            [TelemetryMetricType("distribution")]
            public enum TestMetric
            { 
                [TelemetryMetric("metric.zero")]
                ZeroTagMetric,

                [TelemetryMetric<LogLevel>("metric.one")]
                OneTagMetric,

                [TelemetryMetric<LogLevel, ErrorType>("metric.two")]
                TwoTagMetric,

                [TelemetryMetric("metric.zeroagain")]
                ZeroAgainTagMetric,
            }

            public enum LogLevel
            {
                [Description("debug")] Debug,
                [Description("info")] Info,
                [Description("error")] Error,
            }

            public enum ErrorType
            {
                [Description("random")] Random,
                [Description("ducktyping")] DuckTyping,
            }
            """;

        const string expectedEnum = """
            // <auto-generated/>
            #nullable enable

            namespace MyTests.TestMetricNameSpace;
            internal static partial class TestMetricExtensions
            {
                /// <summary>
                /// The number of separate metrics in the <see cref="MyTests.TestMetricNameSpace.TestMetric" /> metric.
                /// </summary>
                public const int Length = 4;

                /// <summary>
                /// Gets the metric name for the provided metric
                /// </summary>
                /// <param name="metric">The metric to get the name for</param>
                /// <returns>The datadog metric name</returns>
                public static string GetName(this MyTests.TestMetricNameSpace.TestMetric metric)
                    => metric switch
                    {
                        MyTests.TestMetricNameSpace.TestMetric.ZeroTagMetric => "metric.zero",
                        MyTests.TestMetricNameSpace.TestMetric.OneTagMetric => "metric.one",
                        MyTests.TestMetricNameSpace.TestMetric.TwoTagMetric => "metric.two",
                        MyTests.TestMetricNameSpace.TestMetric.ZeroAgainTagMetric => "metric.zeroagain",
                        _ => null!,
                    };

                /// <summary>
                /// Gets whether the metric is a "common" metric, used by all tracers
                /// </summary>
                /// <param name="metric">The metric to check</param>
                /// <returns>True if the metric is a "common" metric, used by all languages</returns>
                public static bool IsCommon(this MyTests.TestMetricNameSpace.TestMetric metric)
                    => metric switch
                    {
                        _ => true,
                    };

                /// <summary>
                /// Gets the custom namespace for the provided metric
                /// </summary>
                /// <param name="metric">The metric to get the name for</param>
                /// <returns>The datadog metric name</returns>
                public static string? GetNamespace(this MyTests.TestMetricNameSpace.TestMetric metric)
                    => metric switch
                    {
                        _ => null,
                    };
            }
            """;

        const string expectedCollector = """
            // <auto-generated/>
            #nullable enable

            using System.Threading;

            namespace Datadog.Trace.Telemetry;
            internal partial class MetricsTelemetryCollector
            {
                public void RecordTestMetricZeroTagMetric(double value)
                {
                    _buffer.Distributions[0].Values.TryEnqueue(value);
                }

                public void RecordTestMetricOneTagMetric(MyTests.TestMetricNameSpace.LogLevel tag, double value)
                {
                    var index = 1 + (int)tag;
                    _buffer.Distributions[index].Values.TryEnqueue(value);
                }

                public void RecordTestMetricTwoTagMetric(MyTests.TestMetricNameSpace.LogLevel tag1, MyTests.TestMetricNameSpace.ErrorType tag2, double value)
                {
                    var index = 4 + ((int)tag1 * 2) + (int)tag2;
                    _buffer.Distributions[index].Values.TryEnqueue(value);
                }

                public void RecordTestMetricZeroAgainTagMetric(double value)
                {
                    _buffer.Distributions[10].Values.TryEnqueue(value);
                }

                /// <summary>
                /// Creates the buffer for the <see cref="MyTests.TestMetricNameSpace.TestMetric" /> values.
                /// </summary>
                private static DistributionKey[] GetTestMetricBuffer()
                    => new DistributionKey[]
                    {
                        // metric.zero, index = 0
                        new(null),
                        // metric.one, index = 1
                        new(new[] { "debug" }),
                        new(new[] { "info" }),
                        new(new[] { "error" }),
                        // metric.two, index = 4
                        new(new[] { "debug", "random" }),
                        new(new[] { "debug", "ducktyping" }),
                        new(new[] { "info", "random" }),
                        new(new[] { "info", "ducktyping" }),
                        new(new[] { "error", "random" }),
                        new(new[] { "error", "ducktyping" }),
                        // metric.zeroagain, index = 10
                        new(null),
                    };

                /// <summary>
                /// Gets an array of metric counts, indexed by integer value of the <see cref="MyTests.TestMetricNameSpace.TestMetric" />.
                /// Each value represents the number of unique entries in the buffer returned by <see cref="GetTestMetricBuffer()" />
                /// It is equal to the cardinality of the tag combinations (or 1 if there are no tags)
                /// </summary>
                private static int[] TestMetricEntryCounts { get; }
                    = new []{ 1, 3, 6, 1, };
            }
            """;

        const string expectedInterface = """
            // <auto-generated/>
            #nullable enable

            namespace Datadog.Trace.Telemetry;
            internal partial interface IMetricsTelemetryCollector
            {
                public void RecordTestMetricZeroTagMetric(double value);

                public void RecordTestMetricOneTagMetric(MyTests.TestMetricNameSpace.LogLevel tag, double value);

                public void RecordTestMetricTwoTagMetric(MyTests.TestMetricNameSpace.LogLevel tag1, MyTests.TestMetricNameSpace.ErrorType tag2, double value);

                public void RecordTestMetricZeroAgainTagMetric(double value);
            }
            """;

        const string expectedNull = """
            // <auto-generated/>
            #nullable enable

            namespace Datadog.Trace.Telemetry;
            internal partial class NullMetricsTelemetryCollector
            {
                public void RecordTestMetricZeroTagMetric(double value)
                {
                }

                public void RecordTestMetricOneTagMetric(MyTests.TestMetricNameSpace.LogLevel tag, double value)
                {
                }

                public void RecordTestMetricTwoTagMetric(MyTests.TestMetricNameSpace.LogLevel tag1, MyTests.TestMetricNameSpace.ErrorType tag2, double value)
                {
                }

                public void RecordTestMetricZeroAgainTagMetric(double value)
                {
                }
            }
            """;
        var (diagnostics, trees) = TestHelpers.GetGeneratedTrees<TelemetryMetricGenerator>(input);
        using var scope = new AssertionScope();
        diagnostics.Should().BeEmpty();
        // tree 0 is the attributes
        trees[1].Should().Be(expectedEnum);
        trees[2].Should().Be(expectedCollector);
        trees[3].Should().Be(expectedInterface);
        trees[4].Should().Be(expectedNull);
    }

    [Theory]
    [InlineData(@"null")]
    [InlineData("\"\"")]
    public void CantUseAnEmptyMetricType(string metricType)
    {
        var input = $$"""
            using Datadog.Trace.SourceGenerators;
            namespace MyTests.TestMetricNameSpace;

            [TelemetryMetricType({{metricType}})]
            public enum TestMetric
            { 
                [TelemetryMetric("some.metric", 1)]
                SomeMetric,
                [TelemetryMetric("another.metric", 2, false)]
                AnotherMetric,
                [TelemetryMetric("other.metric", 3, false, "ASM")]
                OtherMetric,
            }
            """;

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput<TelemetryMetricGenerator>(input);
        diagnostics.Should().Contain(diag => diag.Id == MissingMetricTypeDiagnostic.Id);
    }

    [Theory]
    [InlineData(@"null")]
    [InlineData("\"\"")]
    public void CantUseAnEmptyMetricName(string name)
    {
        var input = $$"""
            using Datadog.Trace.SourceGenerators;
            namespace MyTests.TestMetricNameSpace;

            [TelemetryMetricType("Count")]
            public enum TestMetric
            { 
                [TelemetryMetric({{name}}, 1)]
                SomeMetric,
            }
            """;

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput<TelemetryMetricGenerator>(input);
        diagnostics.Should().Contain(diag => diag.Id == RequiredValuesMissingDiagnostic.Id);
    }

    [Theory]
    [InlineData("[TelemetryMetric(\"some.metric\")]")]
    [InlineData("[TelemetryMetric(\"some.metric\", false)]")]
    [InlineData("[TelemetryMetric(\"some.metric\", false, \"ASM\")]")]
    [InlineData("[TelemetryMetric<LogLevel>(\"some.metric\")]")]
    [InlineData("[TelemetryMetric<LogLevel, ErrorType>(\"some.metric\")]")]
    public void CantUseDuplicateValues(string metricDefinition)
    {
        var input = $$"""
            using Datadog.Trace.SourceGenerators;
            using System.ComponentModel;

            namespace MyTests.TestMetricNameSpace;

            [TelemetryMetricType("distribution")]
            public enum TestMetric
            { 
                {{metricDefinition}}
                SomeMetric,
                {{metricDefinition}}
                OtherMetric,
            }

            public enum LogLevel
            {
                [Description("debug")] Debug,
                [Description("info")] Info,
                [Description("error")] Error,
            }

            public enum ErrorType
            {
                [Description("random")] Random,
                [Description("ducktyping")] DuckTyping,
            }
            """;

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput<TelemetryMetricGenerator>(input);
        diagnostics.Should().Contain(diag => diag.Id == DuplicateMetricValueDiagnostic.Id);
    }
}