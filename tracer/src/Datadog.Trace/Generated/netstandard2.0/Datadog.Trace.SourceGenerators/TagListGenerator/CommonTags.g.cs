﻿// <copyright company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
// <auto-generated/>

#nullable enable

using Datadog.Trace.Processors;
using Datadog.Trace.Tagging;
using System;

namespace Datadog.Trace.Tagging
{
    partial class CommonTags
    {
        // SamplingLimitDecisionBytes = MessagePack.Serialize("_dd.limit_psr");
        private static ReadOnlySpan<byte> SamplingLimitDecisionBytes => new byte[] { 173, 95, 100, 100, 46, 108, 105, 109, 105, 116, 95, 112, 115, 114 };
        // TracesKeepRateBytes = MessagePack.Serialize("_dd.tracer_kr");
        private static ReadOnlySpan<byte> TracesKeepRateBytes => new byte[] { 173, 95, 100, 100, 46, 116, 114, 97, 99, 101, 114, 95, 107, 114 };
        // SamplingAgentDecisionBytes = MessagePack.Serialize("_dd.agent_psr");
        private static ReadOnlySpan<byte> SamplingAgentDecisionBytes => new byte[] { 173, 95, 100, 100, 46, 97, 103, 101, 110, 116, 95, 112, 115, 114 };

        public override double? GetMetric(string key)
        {
            return key switch
            {
                "_dd.limit_psr" => SamplingLimitDecision,
                "_dd.tracer_kr" => TracesKeepRate,
                "_dd.agent_psr" => SamplingAgentDecision,
                _ => base.GetMetric(key),
            };
        }

        public override void SetMetric(string key, double? value)
        {
            switch(key)
            {
                case "_dd.limit_psr": 
                    SamplingLimitDecision = value;
                    break;
                case "_dd.tracer_kr": 
                    TracesKeepRate = value;
                    break;
                case "_dd.agent_psr": 
                    SamplingAgentDecision = value;
                    break;
                default: 
                    base.SetMetric(key, value);
                    break;
            }
        }

        public override void EnumerateMetrics<TProcessor>(ref TProcessor processor)
        {
            if (SamplingLimitDecision is not null)
            {
                processor.Process(new TagItem<double>("_dd.limit_psr", SamplingLimitDecision.Value, SamplingLimitDecisionBytes));
            }

            if (TracesKeepRate is not null)
            {
                processor.Process(new TagItem<double>("_dd.tracer_kr", TracesKeepRate.Value, TracesKeepRateBytes));
            }

            if (SamplingAgentDecision is not null)
            {
                processor.Process(new TagItem<double>("_dd.agent_psr", SamplingAgentDecision.Value, SamplingAgentDecisionBytes));
            }

            base.EnumerateMetrics(ref processor);
        }

        protected override void WriteAdditionalMetrics(System.Text.StringBuilder sb)
        {
            if (SamplingLimitDecision is not null)
            {
                sb.Append("_dd.limit_psr (metric):")
                  .Append(SamplingLimitDecision.Value)
                  .Append(',');
            }

            if (TracesKeepRate is not null)
            {
                sb.Append("_dd.tracer_kr (metric):")
                  .Append(TracesKeepRate.Value)
                  .Append(',');
            }

            if (SamplingAgentDecision is not null)
            {
                sb.Append("_dd.agent_psr (metric):")
                  .Append(SamplingAgentDecision.Value)
                  .Append(',');
            }

            base.WriteAdditionalMetrics(sb);
        }
    }
}
