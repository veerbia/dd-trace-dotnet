// <copyright file="CoveredAssemblyAttribute.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
#nullable enable

using System;

namespace Datadog.Trace.Ci.Coverage.Attributes
{
    /// <summary>
    /// Covered assembly attribute
    /// This attributes marks an assembly as a processed.
    /// </summary>
    public class CoveredAssemblyAttribute : Attribute
    {
    }
}
