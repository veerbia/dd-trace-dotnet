// <copyright file="BeginMethodHandler`2.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
#nullable enable

using System;
using System.Runtime.CompilerServices;

#pragma warning disable SA1649 // File name must match first type name

namespace Datadog.Trace.ClrProfiler.CallTarget.Handlers;

internal static class BeginMethodHandler<TIntegration, TTarget, TArg1, TArg2>
{
    private static readonly InvokeDelegate _invokeDelegate;

    static BeginMethodHandler()
    {
        try
        {
            var tArg1ByRef = typeof(TArg1).IsByRef ? typeof(TArg1) : typeof(TArg1).MakeByRefType();
            var tArg2ByRef = typeof(TArg2).IsByRef ? typeof(TArg2) : typeof(TArg2).MakeByRefType();
            if (IntegrationMapper.CreateBeginMethodDelegate(typeof(TIntegration), typeof(TTarget), [tArg1ByRef, tArg2ByRef]) is { } dynMethod)
            {
                _invokeDelegate = (InvokeDelegate)dynMethod.CreateDelegate(typeof(InvokeDelegate));
            }
        }
        catch (Exception ex)
        {
            throw new CallTargetInvokerException(ex);
        }
        finally
        {
            _invokeDelegate ??= (TTarget? _, ref TArg1? _, ref TArg2? _) => CallTargetState.GetDefault();
        }
    }

    internal delegate CallTargetState InvokeDelegate(TTarget? instance, ref TArg1? arg1, ref TArg2? arg2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static CallTargetState Invoke(TTarget? instance, ref TArg1? arg1, ref TArg2? arg2)
    {
        var activeScope = Tracer.Instance.InternalActiveScope;
        // We don't use Tracer.Instance.DistributedSpanContext directly because we already retrieved the
        // active scope from an AsyncLocal instance, and we want to avoid retrieving twice.
        var spanContextRaw = DistributedTracer.Instance.GetSpanContextRaw() ?? activeScope?.Span?.Context;
        return new CallTargetState(activeScope, spanContextRaw, _invokeDelegate(instance, ref arg1, ref arg2));
    }
}
