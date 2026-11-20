using System.Globalization;

namespace Calculator.Core.Tests;

[Trait("Feature", "F2")]
[Trait("Requirement", "R4")]
public sealed class NumberFormatterTests
{
    // Built from literals/constructors (not parsed) so scale and the sign of zero are preserved;
    // discovery enumeration is disabled so xUnit never round-trips them through serialization.
    public static TheoryData<decimal, string> FormatCases => new()
    {
        { 2.5m, "2.5" },
        { 7.000m, "7" },
        { 0m, "0" },
        { new decimal(0, 0, 0, isNegative: true, scale: 1), "0" },
        { -2.5m, "-2.5" },
        { 0.0000001m, "0.0000001" },
        { 79228162514264337593543950335m, "79228162514264337593543950335" },
        { 12345678901234567890.123456789m, "12345678901234567890.12346" },
        { 1234567890123456789012344.5m, "1234567890123456789012345" },
        { -0.00000000000000000000000025m, "-0.0000000000000000000000003" },
        { -12345678901234567890.123456789m, "-12345678901234567890.12346" },
        { 9.999999999999999999999999999m, "10" },
        { 0.9999999999999999999999999999m, "1" },
        { 7922816251426433759354395033.5m, "7922816251426433759354395034" },
        { 0.0000000000000000000000000001m, "0.0000000000000000000000000001" },
        { -0.0000000000000000000000000001m, "-0.0000000000000000000000000001" },
    };

    [Theory]
    [MemberData(nameof(FormatCases), DisableDiscoveryEnumeration = true)]
    public void Format_Value_ReturnsDisplayText(decimal value, string expected)
    {
        Assert.Equal(expected, NumberFormatter.Format(value));
    }

    [Theory]
    [InlineData("1/3", "0.3333333333333333333333333")]
    [InlineData("2/3", "0.6666666666666666666666667")]
    [InlineData("1/3*3", "1")]
    public void Format_ResultOfDivision_RoundsToDisplayDigits(string input, string expected)
    {
        var value = Expression.Parse(input).Evaluate().Value;

        Assert.Equal(expected, NumberFormatter.Format(value));
    }

    [Theory]
    [InlineData("uk-UA")]
    [InlineData("de-DE")]
    public void Format_NonInvariantCurrentCulture_OutputIsUnchanged(string cultureName)
    {
        var original = CultureInfo.CurrentCulture;
        string sum;
        string negative;
        string large;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            sum = NumberFormatter.Format(Expression.Parse("0.1+0.2").Evaluate().Value);
            negative = NumberFormatter.Format(-2.5m);
            large = NumberFormatter.Format(12345678901234567890.123456789m);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        Assert.Equal("0.3", sum);
        Assert.Equal("-2.5", negative);
        Assert.Equal("12345678901234567890.12346", large);
    }
}
