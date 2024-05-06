// <copyright file="AspNetCore5RaspPerformance.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NETCOREAPP3_0_OR_GREATER
#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Datadog.Trace.Configuration;
using Datadog.Trace.Iast.Telemetry;
using Datadog.Trace.Security.IntegrationTests.IAST;
using Datadog.Trace.TestHelpers;
using VerifyTests;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.Security.IntegrationTests.Rasp;

public class AspNetCore5RaspPerformance : AspNetBase, IClassFixture<AspNetCoreTestFixture>
{
    // This class is used to test RASP features either with IAST enabled or disabled. Since they both use common instrumentation
    // points, we should test that IAST works normally with or without RASP enabled.
    public AspNetCore5RaspPerformance(AspNetCoreTestFixture fixture, ITestOutputHelper outputHelper)
        : base("AspNetCore5", outputHelper, "/shutdown", testName: "AspNetCore5.SecurityEnabled")
    {
        Fixture = fixture;
    }

    protected bool IastEnabled { get; }

    protected AspNetCoreTestFixture Fixture { get; }

    public override void Dispose()
    {
        base.Dispose();
        Fixture.SetOutput(null);
    }

    [SkippableTheory]
    [InlineData("/Iast/GetFileContent?file=filename", false, false, 100000)]
    [InlineData("/Iast/GetFileContent?file=filename", true, false, 100000)]
    [InlineData("/Iast/GetFileContent?file=filename", true, true, 100000)]
    [InlineData("/Iast/WeakHashing", false, false, 100000)]
    [InlineData("/Iast/WeakHashing", true, false, 100000)]
    [InlineData("/Iast/WeakHashing", true, true, 100000)]
    [Trait("RunOnWindows", "True")]
    public async Task TestRaspRequest(string url, bool enableRasp, bool useRules, int executions)
    {
        Init(enableRasp, useRules);
        await TryStartApp();
        var agent = Fixture.Agent;

        // Warm up
        SendRequestsNoWait(url, null, 100);

        var time = DateTime.Now;
        SendRequestsNoWait(url, null, executions);

        var seconds = DateTime.Now.Subtract(time).TotalSeconds;

        LogTime(seconds, enableRasp, useRules, executions, "TestRaspRequest.csv", url);
    }

    [SkippableTheory]
    [InlineData("/Iast/WeakHashing", false, false, 50)]
    [InlineData("/Iast/WeakHashing", true, false, 50)]
    [InlineData("/Iast/WeakHashing", true, true, 50)]
    [Trait("RunOnWindows", "True")]
    public async Task TestRaspStartup(string url, bool enableRasp, bool useRules, int executions)
    {
        Init(enableRasp, useRules);

        var time = DateTime.Now;
        for (int i = 0; i < executions; i++)
        {
            await TryStartApp();
            SendRequestsNoWait(url, null, 1);

            if (i != executions - 1)
            {
                Fixture.Dispose();
                Fixture.Process?.WaitForExit();
                Fixture.Process = null;
            }
        }

        var seconds = DateTime.Now.Subtract(time).TotalSeconds;

        LogTime(seconds, enableRasp, useRules, executions, "TestRaspStartup.csv", url);
    }

    private void LogTime(double seconds, bool enableRasp, bool useRules, int executions, string fileName, string url)
    {
        // Write the data into a csv file
        var data = $"RASP: {enableRasp}, UseRules: {useRules}, Executions: {executions}, Time: {seconds}, url:{url}" + Environment.NewLine;

        var path = Directory.GetCurrentDirectory() + "\\performance";
        Directory.CreateDirectory(path);
        var pathFile = Path.Combine(path, fileName);
        File.AppendAllText(pathFile, data);
    }

    private void Init(bool enableRasp, bool useRules)
    {
        IncludeAllHttpSpans = true;
        EnableRasp(enableRasp);
        SetSecurity(true);
        EnableIast(false);
        if (useRules)
        {
            SetEnvironmentVariable(ConfigurationKeys.AppSec.Rules, "rasp-rule-set.json");
        }
    }

    private async Task TryStartApp()
    {
        await Fixture.TryStartApp(this, true);
        SetHttpPort(Fixture.HttpPort);
    }
}
#endif
