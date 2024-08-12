using Xunit;

namespace Samples.InstrumentedTests.Iast.Vulnerabilities.StringPropagation;
public class DefaultInterpolatedStringHandlerTests : InstrumentationTestsBase
{
    protected string TaintedString = "TaintedString";
    protected string NotTaintedString = "TaintedString333";

    public DefaultInterpolatedStringHandlerTests()
    {
        AddTainted(TaintedString);
    }

    [Fact]
    public void GivenATaintedString_WhenCallingYYY_ResultIsTainted()
    {
        var t = "Welcome, " + TaintedString + "eee!";
        var result = $"Hi {TaintedString} {NotTaintedString}.";
        AssertTainted(result);
    }
}
