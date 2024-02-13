// <copyright file="AzureStorageBlobsTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Datadog.Trace.Configuration;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
using FluentAssertions.Execution;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.Azure
{
    [UsesVerify]
    public class AzureStorageBlobsTests : TracingIntegrationTest
    {
        public AzureStorageBlobsTests(ITestOutputHelper output)
            : base("AzureStorageBlobs", output)
        {
        }

        public static IEnumerable<object[]> GetEnabledConfig()
            => from packageVersionArray in new string[] { string.Empty }
               from metadataSchemaVersion in new[] { "v0", "v1" }
               select new string[] { packageVersionArray, metadataSchemaVersion };

        public override Result ValidateIntegrationSpan(MockSpan span, string metadataSchemaVersion) => span.Tags["span.kind"] switch
        {
            SpanKinds.Consumer => span.IsAzureServiceBusInbound(metadataSchemaVersion),
            SpanKinds.Producer => span.IsAzureServiceBusOutbound(metadataSchemaVersion),
            SpanKinds.Client => span.IsAzureServiceBusRequest(metadataSchemaVersion),
            _ => throw new ArgumentException($"span.Tags[\"span.kind\"] is not a supported value for the AWS SQS integration: {span.Tags["span.kind"]}", nameof(span)),
        };

        // [SkippableTheory(Skip = "We are unable to test all the features of Azure Service Bus with an emulator. For now, run only locally with a connection string to a live Azure Service Bus namespace")]
        [SkippableTheory]
        [MemberData(nameof(GetEnabledConfig))]
        [Trait("Category", "EndToEnd")]
        public async Task SubmitsTraces(string packageVersion, string metadataSchemaVersion)
        {
            SetEnvironmentVariable("DD_TRACE_SPAN_ATTRIBUTE_SCHEMA", metadataSchemaVersion);
            SetEnvironmentVariable("DD_TRACE_OTEL_ENABLED", "true");

            // If you want to use a custom connection string, set it here
            // SetEnvironmentVariable("BLOBS_CONNECTION_STRING", null);

#if NETFRAMEWORK
            var frameworkName = "NetFramework";
#else
            var frameworkName = "NetCore";
#endif

            var normalizedPrefix = NormalizePrefix(TestPrefix) + "-" + metadataSchemaVersion.ToLowerInvariant() + "-";

            using (var telemetry = this.ConfigureTelemetry())
            using (var agent = EnvironmentHelper.GetMockAgent())
            using (RunSampleAndWaitForExit(agent, arguments: normalizedPrefix, packageVersion: packageVersion))
            {
                // 32 traces have three spans: Blob* (assembly:Azure.Storage.Blobs) -> Azure.Core.Http.Request (assembly:Azure.Core) -> http.request (assembly:corelib)
                // 6 traces have four spans:  Blob* (assembly:Azure.Storage.Blobs) -> Blob* (assembly:Azure.Storage.Blobs) -> Azure.Core.Http.Request (assembly:Azure.Core) -> http.request (assembly:corelib)
                // Specifically, the following commands have the double Blob operations:
                // - BlobContainerClient.CreateIfNotExists
                // - BlobContainerClient.Exists
                // - BlockBlobClient.OpenWrite
                // - BlockBlobClient.OpenRead
                // - BlobBaseClient.DownloadTo
                // - BlobContainerClient.DeleteIfExists
                const int expectedProcessorSpanCount = 120;
                var spans = agent.WaitForSpans(expectedProcessorSpanCount);

                using var s = new AssertionScope();
                spans.Count().Should().Be(expectedProcessorSpanCount);

                // var serviceBusSpans = spans.Where(s => s.Tags["span.kind"] != "internal");
                // ValidateIntegrationSpans(serviceBusSpans, metadataSchemaVersion, expectedServiceName: "Samples.AzureServiceBus", isExternalSpan: false);

                var filename = $"{nameof(AzureStorageBlobsTests)}.{frameworkName}.Schema{metadataSchemaVersion.ToUpper()}";

                var settings = VerifyHelper.GetSpanVerifierSettings();
                settings.AddRegexScrubber(new Regex(@"[a-zA-Z0-9-]+.blob.core.windows.net"), "localhost");
                settings.AddSimpleScrubber(normalizedPrefix, string.Empty); // Remove the container prefix so each run generates the same snapshot
                settings.AddRegexScrubber(new Regex(@"http.user_agent: azsdk-net-Storage.Blobs.*,"), "http.user_agent: azsdk-net-Storage.Blobs,");

                await VerifyHelper.VerifySpans(spans, settings, OrderSpans)
                                  .UseFileName(filename)
                                  .DisableRequireUniquePrefix();

                telemetry.AssertIntegrationEnabled(IntegrationId.OpenTelemetry);
            }
        }

        private static string NormalizePrefix(string input)
        {
            StringBuilder sb = new();

            foreach (char c in input)
            {
                switch (c)
                {
                    case (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-':
                        sb.Append(c);
                        break;
                    case (>= 'A' and <= 'Z'):
                        sb.Append(char.ToLower(c));
                        break;
                    default:
                        break;
                }
            }

            return sb.ToString();
        }

        private static IOrderedEnumerable<MockSpan> OrderSpans(IReadOnlyCollection<MockSpan> spans)
            => spans
                .OrderBy(x => x.Start)
                .ThenBy(x => x.Duration);
    }
}
