using System.Runtime.CompilerServices;
using Xunit;

namespace Samples.InstrumentedTests.Iast.Vulnerabilities.StringPropagation;
public class DefaultInterpolatedStringHandlerTests : InstrumentationTestsBase
{
    protected string TaintedString = "TaintedString";
    protected string NotTaintedString = "TaintedString333";

    // Not testing generics until they are fully supported:
    // public void AppendFormatted<T>(T value)
    // public void AppendFormatted<T>(T value, string? format)
    // public void AppendFormatted<T>(T value, int alignment)
    // public void AppendFormatted<T>(T value, int alignment, string? format)

    public DefaultInterpolatedStringHandlerTests()
    {
        AddTainted(TaintedString);
    }

    [Fact]
    public void GivenATaintedString_WhenCallingConcatOptimized_ResultIsTainted()
    {
        var t = TaintedString + "w";
        var result = $"Hi {TaintedString} {NotTaintedString}.";
        AssertTainted(result);
    }

#if NET6_0_OR_GREATER

    [Fact]
    public void GivenATaintedString_WhenCallingAppendFormatted_ResultIsTainted()
    {
        DefaultInterpolatedStringHandler handler = new();
        handler.AppendFormatted(TaintedString);
        var result = handler.ToString();
        AssertTainted(result);
    }

    [Fact]
    public void GivenATaintedString_WhenCallingAppendFormatted_ResultIsTainted2()
    {
        DefaultInterpolatedStringHandler handler = new();
        handler.AppendFormatted(TaintedString, 0);
        var result = handler.ToString();
        AssertTainted(result);
    }

    [Fact]
    public void GivenATaintedString_WhenCallingAppendFormatted_ResultIsTainted3()
    {
        DefaultInterpolatedStringHandler handler = new();
        handler.AppendFormatted(TaintedString, 0, "format");
        var result = handler.ToString();
        AssertTainted(result);
    }

    [Fact]
    public void GivenATaintedString_WhenCallingAppendFormatted_ResultIsTainted4()
    {
        DefaultInterpolatedStringHandler handler = new();
        handler.AppendFormatted((object)TaintedString, 0, "format");
        var result = handler.ToString();
        AssertTainted(result);
    }

    [Fact]
    public void GivenATaintedString_WhenCallingToStringAndClear_ResultIsTainted()
    {
        DefaultInterpolatedStringHandler handler = new();
        handler.AppendFormatted(TaintedString);
        handler.AppendLiteral("literal");
        var result = handler.ToStringAndClear();
        AssertTainted(result);
    }

#endif
}
