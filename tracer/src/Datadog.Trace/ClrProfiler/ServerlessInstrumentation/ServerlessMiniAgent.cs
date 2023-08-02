// <copyright file="ServerlessMiniAgent.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Diagnostics;
using System.IO;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;
using Datadog.Trace.Util;

namespace Datadog.Trace.ClrProfiler.ServerlessInstrumentation;

internal static class ServerlessMiniAgent
{
    private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(ServerlessMiniAgent));

    internal static string GetMiniAgentPath(PlatformID os, ImmutableTracerSettings settings)
    {
        if (!settings.IsRunningInGCPFunctions && !settings.IsRunningInAzureFunctionsConsumptionPlan)
        {
            return null;
        }

        if (EnvironmentHelpers.GetEnvironmentVariable("DD_MINI_AGENT_PATH") != null)
        {
            return EnvironmentHelpers.GetEnvironmentVariable("DD_MINI_AGENT_PATH");
        }

        // Environment.OSVersion.Platform can return PlatformID.Unix on MacOS, this is OK as GCP & Azure don't have MacOs functions.
        if (os != PlatformID.Unix && os != PlatformID.Win32NT)
        {
            Log.Error("Serverless Mini Agent is only supported on Windows and Linux.");
            return null;
        }

        string rustBinaryPathRoot;
        if (settings.IsRunningInGCPFunctions)
        {
            rustBinaryPathRoot = "/layers/google.dotnet.publish/publish/bin";
        }
        else
        {
            rustBinaryPathRoot = "/home/site/wwwroot";
        }

        var isWindows = os == PlatformID.Win32NT;

        string rustBinaryPathOsFolder = isWindows ? "datadog-serverless-agent-windows-amd64" : "datadog-serverless-agent-linux-amd64";
        string rustBinaryName = isWindows ? "datadog-serverless-trace-mini-agent.exe" : "datadog-serverless-trace-mini-agent";
        return System.IO.Path.Combine(rustBinaryPathRoot, rustBinaryPathOsFolder, rustBinaryName);
    }

    internal static void StartServerlessMiniAgent(ImmutableTracerSettings settings)
    {
        var serverlessMiniAgentPath = ServerlessMiniAgent.GetMiniAgentPath(Environment.OSVersion.Platform, settings);
        if (string.IsNullOrEmpty(serverlessMiniAgentPath))
        {
            return;
        }

        if (!File.Exists(serverlessMiniAgentPath))
        {
            Log.Error("Serverless Mini Agent does not exist: {Path}", serverlessMiniAgentPath);
            return;
        }

        try
        {
            Log.Debug("Trying to spawn the Serverless Mini Agent at path: {Path}", serverlessMiniAgentPath);
            Process process = new Process();
            process.StartInfo.FileName = serverlessMiniAgentPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += MiniAgentDataReceivedHandler;

            process.Start();
            process.BeginOutputReadLine();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error spawning the Serverless Mini Agent.");
        }
    }

    // Tries to clean Mini Agent logs and log to the correct level, otherwise just logs the data as-is to Info
    // Mini Agent logs will be prefixed with "[Datadog Serverless Mini Agent]"
    private static void MiniAgentDataReceivedHandler(object sender, DataReceivedEventArgs outLine)
    {
        var data = outLine.Data;
        if (string.IsNullOrEmpty(data))
        {
            return;
        }

        var logTuple = ProcessMiniAgentLog(data);
        string level = logTuple.Item1;
        string processedLog = logTuple.Item2;

        switch (level)
        {
            case "ERROR":
                Log.Error("[Datadog Serverless Mini Agent] {Data}", processedLog);
                break;
            case "WARN":
                Log.Warning("[Datadog Serverless Mini Agent] {Data}", processedLog);
                break;
            case "INFO":
                Log.Information("[Datadog Serverless Mini Agent] {Data}", processedLog);
                break;
            case "DEBUG":
                Log.Debug("[Datadog Serverless Mini Agent] {Data}", processedLog);
                break;
            default:
                Log.Information("[Datadog Serverless Mini Agent] {Data}", data);
                break;
        }
    }

    // Processes a raw log from the mini agent, returning a Tuple of the log level and the cleaned log data
    // For example, given this raw log:
    // [2023-06-06T01:31:30Z DEBUG datadog_trace_mini_agent::mini_agent] Random log
    // This function will return:
    // ("DEBUG", "Random log")
    internal static Tuple<string, string> ProcessMiniAgentLog(string rawLog)
    {
        int logPrefixCutoff = rawLog.IndexOf("]");
        if (logPrefixCutoff < 0 || logPrefixCutoff == rawLog.Length - 1)
        {
            return Tuple.Create("INFO", rawLog);
        }

        int levelLeftBound = rawLog.IndexOf(" ");
        if (levelLeftBound < 0)
        {
            return Tuple.Create("INFO", rawLog);
        }

        int levelRightBound = rawLog.IndexOf(" ", levelLeftBound + 1);
        if (levelRightBound < 0 || levelRightBound - levelLeftBound < 1)
        {
            return Tuple.Create("INFO", rawLog);
        }

        string level = rawLog.Substring(levelLeftBound + 1, levelRightBound - levelLeftBound - 1);

        if (!(level is "ERROR" or "WARN" or "INFO" or "DEBUG"))
        {
            return Tuple.Create("INFO", rawLog);
        }

        string processedLog = rawLog.Substring(logPrefixCutoff + 2);

        if (level is "DEBUG")
        {
            return Tuple.Create("INFO", $"[DEBUG] {processedLog}");
        }

        return Tuple.Create(level, processedLog);
    }
}
