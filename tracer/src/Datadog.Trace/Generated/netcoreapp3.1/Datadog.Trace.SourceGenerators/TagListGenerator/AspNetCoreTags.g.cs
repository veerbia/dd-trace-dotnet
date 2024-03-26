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
    partial class AspNetCoreTags
    {
        // InstrumentationNameBytes = MessagePack.Serialize("component");
        private static ReadOnlySpan<byte> InstrumentationNameBytes => new byte[] { 169, 99, 111, 109, 112, 111, 110, 101, 110, 116 };
        // AspNetCoreRouteBytes = MessagePack.Serialize("aspnet_core.route");
        private static ReadOnlySpan<byte> AspNetCoreRouteBytes => new byte[] { 177, 97, 115, 112, 110, 101, 116, 95, 99, 111, 114, 101, 46, 114, 111, 117, 116, 101 };
        // HttpRouteBytes = MessagePack.Serialize("http.route");
        private static ReadOnlySpan<byte> HttpRouteBytes => new byte[] { 170, 104, 116, 116, 112, 46, 114, 111, 117, 116, 101 };

        public override string? GetTag(string key)
        {
            return key switch
            {
                "component" => InstrumentationName,
                "aspnet_core.route" => AspNetCoreRoute,
                "http.route" => HttpRoute,
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
                case "aspnet_core.route": 
                    AspNetCoreRoute = value;
                    break;
                case "http.route": 
                    HttpRoute = value;
                    break;
                default: 
                    base.SetTag(key, value);
                    break;
            }
        }

        public override void EnumerateTags<TProcessor>(ref TProcessor processor)
        {
            if (InstrumentationName is not null)
            {
                processor.Process(new TagItem<string>("component", InstrumentationName, InstrumentationNameBytes));
            }

            if (AspNetCoreRoute is not null)
            {
                processor.Process(new TagItem<string>("aspnet_core.route", AspNetCoreRoute, AspNetCoreRouteBytes));
            }

            if (HttpRoute is not null)
            {
                processor.Process(new TagItem<string>("http.route", HttpRoute, HttpRouteBytes));
            }

            base.EnumerateTags(ref processor);
        }

        protected override void WriteAdditionalTags(System.Text.StringBuilder sb)
        {
            if (InstrumentationName is not null)
            {
                sb.Append("component (tag):")
                  .Append(InstrumentationName)
                  .Append(',');
            }

            if (AspNetCoreRoute is not null)
            {
                sb.Append("aspnet_core.route (tag):")
                  .Append(AspNetCoreRoute)
                  .Append(',');
            }

            if (HttpRoute is not null)
            {
                sb.Append("http.route (tag):")
                  .Append(HttpRoute)
                  .Append(',');
            }

            base.WriteAdditionalTags(sb);
        }
    }
}
