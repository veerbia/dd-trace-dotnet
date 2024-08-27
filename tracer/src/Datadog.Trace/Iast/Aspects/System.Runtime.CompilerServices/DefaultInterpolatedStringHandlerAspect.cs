// <copyright file="DefaultInterpolatedStringHandlerAspect.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Runtime.CompilerServices;
using Datadog.Trace.Iast.Dataflow;
using Datadog.Trace.Iast.Propagation;
using Datadog.Trace.Logging;

#if NET6_0_OR_GREATER

#nullable enable
namespace Datadog.Trace.Iast.Aspects.System;

/// <summary> String class aspects </summary>
[AspectClass("mscorlib,netstandard,System.Private.CoreLib,System.Runtime", [AspectFilter.StringOptimization])]
[global::System.ComponentModel.Browsable(false)]
[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
internal class DefaultInterpolatedStringHandlerAspect
{
    private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(DefaultInterpolatedStringHandlerAspect));

    /// <summary>
    /// DefaultInterpolatedStringHandler.AppendFormatted aspect
    /// </summary>
    /// <param name="target"> DefaultInterpolatedStringHandler base instance </param>
    /// <param name="value"> value to append </param>
    [AspectMethodReplace("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(System.String)")]
    public static void AppendFormatted(ref DefaultInterpolatedStringHandler target, string? value)
    {
        target.AppendFormatted(value);
        try
        {
            if (value is not null)
            {
                var text = target.ToString();
                var parameterLength = value.Length;
                var initialLength = text.Length - parameterLength;
            }
        }
        catch (Exception ex)
        {
            IastModule.Log.Error(ex, $"Error invoking {nameof(DefaultInterpolatedStringHandlerAspect)}.{nameof(AppendFormatted)}");
        }
    }
}
#endif
