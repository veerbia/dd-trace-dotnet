// <copyright file="StackWalkerTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using Datadog.Trace.Iast;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Security.Unit.Tests.IAST
{
    public class StackWalkerTests
    {
        [Theory]
        [InlineData("SystemOfGame", false)]
        public void CheckAssemblyExclussion(string assemblyName, bool outcome)
        {
            var time1 = DateTime.Now;
            for (int i = 0; i < 10000000; i++)
            {
                // we check twice to make sure that the cache does not change the outcome
                StackWalker.AssemblyExcluded(assemblyName).Should().Be(outcome);
                StackWalker.AssemblyExcluded(assemblyName).Should().Be(outcome);
            }

            var time = (DateTime.Now - time1).TotalMilliseconds;
            time.Should().BeLessThan(0);
        }
    }
}
