﻿// <auto-generated/>
#nullable enable

using Datadog.Trace.Processors;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Redis
{
    partial class RedisTags
    {
        // SpanKindBytes = System.Text.Encoding.UTF8.GetBytes("span.kind");
        private static readonly byte[] SpanKindBytes = new byte[] { 115, 112, 97, 110, 46, 107, 105, 110, 100 };
        // InstrumentationNameBytes = System.Text.Encoding.UTF8.GetBytes("component");
        private static readonly byte[] InstrumentationNameBytes = new byte[] { 99, 111, 109, 112, 111, 110, 101, 110, 116 };
        // RawCommandBytes = System.Text.Encoding.UTF8.GetBytes("db.statement");
        private static readonly byte[] RawCommandBytes = new byte[] { 100, 98, 46, 115, 116, 97, 116, 101, 109, 101, 110, 116 };
        // HostBytes = System.Text.Encoding.UTF8.GetBytes("out.host");
        private static readonly byte[] HostBytes = new byte[] { 111, 117, 116, 46, 104, 111, 115, 116 };
        // PortBytes = System.Text.Encoding.UTF8.GetBytes("out.port");
        private static readonly byte[] PortBytes = new byte[] { 111, 117, 116, 46, 112, 111, 114, 116 };

        public override string? GetTag(string key)
        {
            return key switch
            {
                "span.kind" => SpanKind,
                "component" => InstrumentationName,
                "db.statement" => RawCommand,
                "out.host" => Host,
                "out.port" => Port,
                _ => base.GetTag(key),
            };
        }

        public override void SetTag(string key, string value)
        {
            switch(key)
            {
                case "component": 
                    InstrumentationName = value;
                    break;
                case "db.statement": 
                    RawCommand = value;
                    break;
                case "out.host": 
                    Host = value;
                    break;
                case "out.port": 
                    Port = value;
                    break;
                case "span.kind": 
                    Logger.Value.Warning("Attempted to set readonly tag {TagName} on {TagType}. Ignoring.", key, nameof(RedisTags));
                    break;
                default: 
                    base.SetTag(key, value);
                    break;
            }
        }

        public override void EnumerateTags<TProcessor>(ref TProcessor processor)
        {
            if (SpanKind is not null)
            {
                processor.Process(new TagItem<string>("span.kind", SpanKind, SpanKindBytes));
            }

            if (InstrumentationName is not null)
            {
                processor.Process(new TagItem<string>("component", InstrumentationName, InstrumentationNameBytes));
            }

            if (RawCommand is not null)
            {
                processor.Process(new TagItem<string>("db.statement", RawCommand, RawCommandBytes));
            }

            if (Host is not null)
            {
                processor.Process(new TagItem<string>("out.host", Host, HostBytes));
            }

            if (Port is not null)
            {
                processor.Process(new TagItem<string>("out.port", Port, PortBytes));
            }

            base.EnumerateTags(ref processor);
        }

        protected override void WriteAdditionalTags(System.Text.StringBuilder sb)
        {
            if (SpanKind is not null)
            {
                sb.Append("span.kind (tag):")
                  .Append(SpanKind)
                  .Append(',');
            }

            if (InstrumentationName is not null)
            {
                sb.Append("component (tag):")
                  .Append(InstrumentationName)
                  .Append(',');
            }

            if (RawCommand is not null)
            {
                sb.Append("db.statement (tag):")
                  .Append(RawCommand)
                  .Append(',');
            }

            if (Host is not null)
            {
                sb.Append("out.host (tag):")
                  .Append(Host)
                  .Append(',');
            }

            if (Port is not null)
            {
                sb.Append("out.port (tag):")
                  .Append(Port)
                  .Append(',');
            }

            base.WriteAdditionalTags(sb);
        }
    }
}
