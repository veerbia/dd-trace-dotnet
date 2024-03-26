﻿// <copyright company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
// <auto-generated/>

#nullable enable

namespace Datadog.Trace.Telemetry;

/// <summary>
/// Extension methods for <see cref="Datadog.Trace.Telemetry.TelemetryErrorCode" />
/// </summary>
internal static partial class TelemetryErrorCodeExtensions
{
    /// <summary>
    /// The number of members in the enum.
    /// This is a non-distinct count of defined names.
    /// </summary>
    public const int Length = 16;

    /// <summary>
    /// Returns the string representation of the <see cref="Datadog.Trace.Telemetry.TelemetryErrorCode"/> value.
    /// If the attribute is decorated with a <c>[Description]</c> attribute, then
    /// uses the provided value. Otherwise uses the name of the member, equivalent to
    /// calling <c>ToString()</c> on <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value to retrieve the string value for</param>
    /// <returns>The string representation of the value</returns>
    public static string ToStringFast(this Datadog.Trace.Telemetry.TelemetryErrorCode value)
        => value switch
        {
            Datadog.Trace.Telemetry.TelemetryErrorCode.None => nameof(Datadog.Trace.Telemetry.TelemetryErrorCode.None),
            Datadog.Trace.Telemetry.TelemetryErrorCode.ParsingBooleanError => "Error parsing value as boolean",
            Datadog.Trace.Telemetry.TelemetryErrorCode.ParsingInt32Error => "Error parsing value as int32",
            Datadog.Trace.Telemetry.TelemetryErrorCode.ParsingDoubleError => "Error parsing value as double",
            Datadog.Trace.Telemetry.TelemetryErrorCode.FailedValidation => "Invalid value",
            Datadog.Trace.Telemetry.TelemetryErrorCode.JsonStringError => "Error reading value as string from JSON",
            Datadog.Trace.Telemetry.TelemetryErrorCode.JsonInt32Error => "Error reading value as int from JSON",
            Datadog.Trace.Telemetry.TelemetryErrorCode.JsonDoubleError => "Error reading value as double from JSON",
            Datadog.Trace.Telemetry.TelemetryErrorCode.JsonBooleanError => "Error reading value as boolean from JSON",
            Datadog.Trace.Telemetry.TelemetryErrorCode.JsonDictionaryError => "Error reading value as dictionary from JSON",
            Datadog.Trace.Telemetry.TelemetryErrorCode.ParsingCustomError => "Error parsing value",
            Datadog.Trace.Telemetry.TelemetryErrorCode.TracerConfigurationError => "Error configuring Tracer",
            Datadog.Trace.Telemetry.TelemetryErrorCode.AppsecConfigurationError => "Error configuring AppSec",
            Datadog.Trace.Telemetry.TelemetryErrorCode.ContinuousProfilerConfigurationError => "Error configuring Continuous Profiler",
            Datadog.Trace.Telemetry.TelemetryErrorCode.DynamicInstrumentationConfigurationError => "Error configuring Dynamic Instrumentation",
            Datadog.Trace.Telemetry.TelemetryErrorCode.PotentiallyInvalidUdsPath => "Potentially invalid UDS path",
            _ => value.ToString(),
        };

    /// <summary>
    /// Retrieves an array of the values of the members defined in
    /// <see cref="Datadog.Trace.Telemetry.TelemetryErrorCode" />.
    /// Note that this returns a new array with every invocation, so
    /// should be cached if appropriate.
    /// </summary>
    /// <returns>An array of the values defined in <see cref="Datadog.Trace.Telemetry.TelemetryErrorCode" /></returns>
    public static Datadog.Trace.Telemetry.TelemetryErrorCode[] GetValues()
        => new []
        {
            Datadog.Trace.Telemetry.TelemetryErrorCode.None,
            Datadog.Trace.Telemetry.TelemetryErrorCode.ParsingBooleanError,
            Datadog.Trace.Telemetry.TelemetryErrorCode.ParsingInt32Error,
            Datadog.Trace.Telemetry.TelemetryErrorCode.ParsingDoubleError,
            Datadog.Trace.Telemetry.TelemetryErrorCode.FailedValidation,
            Datadog.Trace.Telemetry.TelemetryErrorCode.JsonStringError,
            Datadog.Trace.Telemetry.TelemetryErrorCode.JsonInt32Error,
            Datadog.Trace.Telemetry.TelemetryErrorCode.JsonDoubleError,
            Datadog.Trace.Telemetry.TelemetryErrorCode.JsonBooleanError,
            Datadog.Trace.Telemetry.TelemetryErrorCode.JsonDictionaryError,
            Datadog.Trace.Telemetry.TelemetryErrorCode.ParsingCustomError,
            Datadog.Trace.Telemetry.TelemetryErrorCode.TracerConfigurationError,
            Datadog.Trace.Telemetry.TelemetryErrorCode.AppsecConfigurationError,
            Datadog.Trace.Telemetry.TelemetryErrorCode.ContinuousProfilerConfigurationError,
            Datadog.Trace.Telemetry.TelemetryErrorCode.DynamicInstrumentationConfigurationError,
            Datadog.Trace.Telemetry.TelemetryErrorCode.PotentiallyInvalidUdsPath,
        };

    /// <summary>
    /// Retrieves an array of the names of the members defined in
    /// <see cref="Datadog.Trace.Telemetry.TelemetryErrorCode" />.
    /// Note that this returns a new array with every invocation, so
    /// should be cached if appropriate.
    /// Ignores <c>[Description]</c> definitions.
    /// </summary>
    /// <returns>An array of the names of the members defined in <see cref="Datadog.Trace.Telemetry.TelemetryErrorCode" /></returns>
    public static string[] GetNames()
        => new []
        {
            nameof(Datadog.Trace.Telemetry.TelemetryErrorCode.None),
            nameof(Datadog.Trace.Telemetry.TelemetryErrorCode.ParsingBooleanError),
            nameof(Datadog.Trace.Telemetry.TelemetryErrorCode.ParsingInt32Error),
            nameof(Datadog.Trace.Telemetry.TelemetryErrorCode.ParsingDoubleError),
            nameof(Datadog.Trace.Telemetry.TelemetryErrorCode.FailedValidation),
            nameof(Datadog.Trace.Telemetry.TelemetryErrorCode.JsonStringError),
            nameof(Datadog.Trace.Telemetry.TelemetryErrorCode.JsonInt32Error),
            nameof(Datadog.Trace.Telemetry.TelemetryErrorCode.JsonDoubleError),
            nameof(Datadog.Trace.Telemetry.TelemetryErrorCode.JsonBooleanError),
            nameof(Datadog.Trace.Telemetry.TelemetryErrorCode.JsonDictionaryError),
            nameof(Datadog.Trace.Telemetry.TelemetryErrorCode.ParsingCustomError),
            nameof(Datadog.Trace.Telemetry.TelemetryErrorCode.TracerConfigurationError),
            nameof(Datadog.Trace.Telemetry.TelemetryErrorCode.AppsecConfigurationError),
            nameof(Datadog.Trace.Telemetry.TelemetryErrorCode.ContinuousProfilerConfigurationError),
            nameof(Datadog.Trace.Telemetry.TelemetryErrorCode.DynamicInstrumentationConfigurationError),
            nameof(Datadog.Trace.Telemetry.TelemetryErrorCode.PotentiallyInvalidUdsPath),
        };

    /// <summary>
    /// Retrieves an array of the names of the members defined in
    /// <see cref="Datadog.Trace.Telemetry.TelemetryErrorCode" />.
    /// Note that this returns a new array with every invocation, so
    /// should be cached if appropriate.
    /// Uses <c>[Description]</c> definition if available, otherwise uses the name of the property
    /// </summary>
    /// <returns>An array of the names of the members defined in <see cref="Datadog.Trace.Telemetry.TelemetryErrorCode" /></returns>
    public static string[] GetDescriptions()
        => new []
        {
            nameof(Datadog.Trace.Telemetry.TelemetryErrorCode.None),
            "Error parsing value as boolean",
            "Error parsing value as int32",
            "Error parsing value as double",
            "Invalid value",
            "Error reading value as string from JSON",
            "Error reading value as int from JSON",
            "Error reading value as double from JSON",
            "Error reading value as boolean from JSON",
            "Error reading value as dictionary from JSON",
            "Error parsing value",
            "Error configuring Tracer",
            "Error configuring AppSec",
            "Error configuring Continuous Profiler",
            "Error configuring Dynamic Instrumentation",
            "Potentially invalid UDS path",
        };
}