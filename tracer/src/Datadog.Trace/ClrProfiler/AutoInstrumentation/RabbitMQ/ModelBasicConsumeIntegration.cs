// <copyright file="ModelBasicConsumeIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.Logging;
using Datadog.Trace.Tagging;

// Was working when we used RabbitMQ.Client.Impl.ModelBase and _Private_BasicConsume.
// but we don't have the server-generated consumer tag at that point so we can't use the private method...
// the public method doesn't seem to be working
// This also mostly works now, but SetQueue in the method end doesn't seem to work. all the logs here are printed
// properly, but when accessing QueueHelper in RabbitMQIntegration queue is null???
namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.RabbitMQ
{
    /// <summary>
    /// RabbitMQ.Client BasicConsume calltarget instrumentation
    /// </summary>
    [InstrumentMethod(
        AssemblyName = "RabbitMQ.Client",
        TypeName = "RabbitMQ.Client.Impl.ModelBase",
        MethodName = "BasicConsume",
        ReturnTypeName = ClrNames.String,
        ParameterTypeNames = new[] { ClrNames.String, ClrNames.Bool, ClrNames.String, ClrNames.Bool, ClrNames.Bool, RabbitMQConstants.IDictionaryArgumentsTypeName, "RabbitMQ.Client.IBasicConsumer" },
        MinimumVersion = "3.6.9",
        MaximumVersion = "6.*.*",
        IntegrationName = RabbitMQConstants.IntegrationName)]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ModelBasicConsumeIntegration
    {
        private const string Command = "basic.consume";

        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TConsumer">Type of the consumer</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="queue">Name of the queue.</param>
        /// <param name="autoAck">The original autoAck setting</param>
        /// <param name="consumerTag">The original consumerTag setting</param>
        /// <param name="noLocal">The original noLocal setting</param>
        /// <param name="exclusive">The original exclusive setting</param>
        /// <param name="arguments">The original arguments setting</param>
        /// <param name="consumer">The original consumer setting</param>
        /// <returns>Calltarget state value</returns>
        internal static CallTargetState OnMethodBegin<TTarget, TConsumer>(TTarget instance, string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments, TConsumer consumer)
        {
            Console.WriteLine("BasicConsume start");
            Console.WriteLine("Setting queue! consumer: " + consumer + ", queue: " + queue);
            QueueHelper.SetQueue(consumer, queue);
            Console.WriteLine("Done setting queue! consumer: " + consumer + ", queue: " + queue);
            return new CallTargetState(RabbitMQIntegration.CreateScope(Tracer.Instance, out _, Command, SpanKinds.Client, queue: queue));
        }

        /// <summary>
        /// OnMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TReturn">Type of the return value</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="returnValue">Return value instance</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A default CallTargetReturn to satisfy the CallTarget contract</returns>
        internal static CallTargetReturn<TReturn> OnMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, in CallTargetState state)
        {
            Console.WriteLine("BasicConsume ends");
            // if (returnValue is string consumerTag && state.Scope.Span.Tags is RabbitMQTags tags)
            // {
            //    string queue = tags.Queue;
            //    Console.WriteLine("Setting queue! consumerTag: " + consumerTag + ", queue: " + queue);
            //    QueueHelper.SetQueue(consumerTag, queue);
            //    Console.WriteLine("Done setting queue! consumerTag: " + consumerTag + ", queue: " + queue);
            // }

            state.Scope.DisposeWithException(exception);
            return new CallTargetReturn<TReturn>(returnValue);
        }
    }
}
