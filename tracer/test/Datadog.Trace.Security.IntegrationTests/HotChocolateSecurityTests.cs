// <copyright file="HotChocolateSecurityTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NETCOREAPP3_1_OR_GREATER

using System.Net;
using System.Threading.Tasks;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.Security.IntegrationTests
{
    public class HotChocolateSecurityTests : AspNetBase, IClassFixture<AspNetCoreTestFixture>
    {
        private readonly AspNetCoreTestFixture fixture;

        public HotChocolateSecurityTests(AspNetCoreTestFixture fixture, ITestOutputHelper outputHelper)
            : base("HotChocolate", outputHelper, "/shutdown", samplesDir: "test/test-applications/integrations", changeDefaults: true)
        {
            this.fixture = fixture;
            this.fixture.SetOutput(outputHelper);
        }

        public override void Dispose()
        {
            base.Dispose();
            fixture.SetOutput(null);
        }

        [SkippableTheory]
        [InlineData(@"{""query"":""query Book{book{title author{name}}}""}")]
        [InlineData(@"{""query"":""query Book{book(name: \""<script>test\""){title author{name}}}""}")]
        [InlineData(@"{""query"":""query Book{book{title author{name}}} query Book{book(name: \""<script>test\""){title author{name}}}""}")]
        [InlineData(@"{""query"":""query Book{book(badArray: [\""hello world\"", true, 5, \""<script>test\""]){title author{name}}}""}")]
        [InlineData(@"{""query"":""query Book{book(badObject: { title: \""<script>test\"" }){title author{name}}}""}")]
        [InlineData(@"{""query"":""query Book{testAlias: book(name: \""<script>test\""){title author{name}}}""}")]
        [InlineData(@"{""query"":""query Book($name: String!){book(name: $name){title author{name}}}"", ""variables"": { ""name"": ""<script>test""}}")]
        [Trait("RunOnWindows", "True")]
        public async Task TestQuerySecurity(string query)
        {
            await fixture.TryStartApp(this, enableSecurity: true, externalRulesFile: DefaultRuleFile);
            SetHttpPort(fixture.HttpPort);

            var settings = VerifyHelper.GetSpanVerifierSettings(query);

            // On netcoreapp3.1 and net5.0 the response content type is application/json
            // So we change it to application/graphql-response+json to match the newer version
            settings.AddSimpleScrubber("http.response.headers.content-type: application/json; charset=utf-8", "http.response.headers.content-type: application/graphql-response+json; charset=utf-8");

            await TestAppSecRequestWithVerifyAsync(fixture.Agent, "/graphql", query, 1, 1, settings);
        }
    }
}
#endif