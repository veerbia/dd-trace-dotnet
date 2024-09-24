// <copyright file="XUnitRetriesTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Datadog.Trace.Ci.Tags;
using Datadog.Trace.Configuration;
using Datadog.Trace.TestHelpers;
using Datadog.Trace.TestHelpers.Ci;
using Datadog.Trace.Vendors.Newtonsoft.Json;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.CI;

[Collection(nameof(TransportTestsCollection))]
public class XUnitRetriesTests : TestingFrameworkEvpTest
{
    private const string SettingsJson = """{"data":{"id":"511938a3f19c12f8bb5e5caa695ca24f4563de3f","type":"ci_app_tracers_test_service_settings","attributes":{"code_coverage":false,"early_flake_detection":{"enabled":false,"slow_test_retries":{"10s":5,"30s":3,"5m":2,"5s":10},"faulty_session_threshold":100},"flaky_test_retries_enabled":true,"itr_enabled":false,"require_git":false,"tests_skipping":false}}}""";

    public XUnitRetriesTests(ITestOutputHelper output)
        : base("XUnitTestsRetries", output)
    {
        SetServiceName("xunit-retries");
        SetServiceVersion("1.0.0");
    }

    [SkippableTheory]
    [MemberData(nameof(PackageVersions.XUnit), MemberType = typeof(PackageVersions))]
    [Trait("Category", "EndToEnd")]
    [Trait("Category", "TestIntegrations")]
    [Trait("Category", "FlakyRetries")]
    public async Task FlakyRetries(string packageVersion)
    {
        EnvironmentHelper.EnableDefaultTransport();
        var tests = new List<MockCIVisibilityTest>();
        var testSuites = new List<MockCIVisibilityTestSuite>();
        var testModules = new List<MockCIVisibilityTestModule>();

        InjectSession(
            out var sessionId,
            out var sessionCommand,
            out var sessionWorkingDirectory,
            out var gitRepositoryUrl,
            out var gitBranch,
            out var gitCommitSha);

        try
        {
            var retryCount = 5;
            SetEnvironmentVariable(ConfigurationKeys.CIVisibility.FlakyRetryEnabled, "1");
            SetEnvironmentVariable(ConfigurationKeys.CIVisibility.FlakyRetryCount, retryCount.ToString());

            using var agent = EnvironmentHelper.GetMockAgent();
            agent.EventPlatformProxyPayloadReceived += (sender, e) =>
            {
                if (e.Value.PathAndQuery.EndsWith("api/v2/libraries/tests/services/setting"))
                {
                    e.Value.Response = new MockTracerResponse(SettingsJson, 200);
                    return;
                }

                if (e.Value.PathAndQuery.EndsWith("api/v2/citestcycle"))
                {
                    var payload = JsonConvert.DeserializeObject<MockCIVisibilityProtocol>(e.Value.BodyInJson);
                    if (payload.Events?.Length > 0)
                    {
                        foreach (var @event in payload.Events)
                        {
                            if (@event.Content.ToString() is { } eventContent)
                            {
                                if (@event.Type == SpanTypes.Test)
                                {
                                    tests.Add(JsonConvert.DeserializeObject<MockCIVisibilityTest>(eventContent));
                                }
                                else if (@event.Type == SpanTypes.TestSuite)
                                {
                                    testSuites.Add(JsonConvert.DeserializeObject<MockCIVisibilityTestSuite>(eventContent));
                                }
                                else if (@event.Type == SpanTypes.TestModule)
                                {
                                    testModules.Add(JsonConvert.DeserializeObject<MockCIVisibilityTestModule>(eventContent));
                                }
                            }
                        }
                    }
                }
            };

            using var processResult = await RunDotnetTestSampleAndWaitForExit(
                                          agent,
                                          packageVersion: packageVersion);

            const string alwaysFails = "Samples.XUnitTestsRetries.TestSuite.AlwaysFails";
            const string alwaysPasses = "Samples.XUnitTestsRetries.TestSuite.AlwaysPasses";
            const string trueAtLastRetry = "Samples.XUnitTestsRetries.TestSuite.TrueAtLastRetry";
            const string trueAtThirdRetry = "Samples.XUnitTestsRetries.TestSuite.TrueAtThirdRetry";

            // 1 Module
            testModules.Should().HaveCount(1);

            // 1 Suite
            testSuites.Should().HaveCount(1);

            // AlwaysFails => 1 + 5 retries
            var alwaysFailsTests = tests.Where(t => t.Resource == alwaysFails).ToList();
            alwaysFailsTests.Should().HaveCount(1 + retryCount);
            alwaysFailsTests.Should().OnlyContain(t => t.Meta[TestTags.Status] == TestTags.StatusFail);

            // AlwaysPasses => 1
            var alwaysPassesTests = tests.Where(t => t.Resource == alwaysPasses).ToList();
            alwaysPassesTests.Should().HaveCount(1);
            alwaysPassesTests.Should().OnlyContain(t => t.Meta[TestTags.Status] == TestTags.StatusPass);

            // TrueAtLastRetry => 1 + 5 retries
            var trueAtLastRetryTests = tests.Where(t => t.Resource == trueAtLastRetry).ToList();
            trueAtLastRetryTests.Should().HaveCount(1 + retryCount);
            trueAtLastRetryTests.Should().Contain(t => t.Meta[TestTags.Status] == TestTags.StatusPass);

            // TrueAtThirdRetry => 1 + 3 retries
            var trueAtThirdRetryTests = tests.Where(t => t.Resource == trueAtThirdRetry).ToList();
            trueAtThirdRetryTests.Should().HaveCount(1 + 3);
            trueAtThirdRetryTests.Should().Contain(t => t.Meta[TestTags.Status] == TestTags.StatusPass);
        }
        catch
        {
            WriteSpans(tests);
            throw;
        }

        await Task.CompletedTask;
    }
}
