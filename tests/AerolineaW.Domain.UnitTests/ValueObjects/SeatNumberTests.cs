using AerolineaW.Domain.ValueObjects;
using Xunit;

namespace AerolineaW.Domain.UnitTests.ValueObjects;

public class SeatNumberTests
{
    [Theory]
    [InlineData("1A", "1A")]
    [InlineData("14a", "14A")]
    [InlineData("999B", "999B")]
    public void Constructor_ValidInput_Normalizes(string input, string expected)
    {
        Assert.Equal(expected, new SeatNumber(input).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A1")]
    [InlineData("14")]
    [InlineData("0A")]
    [InlineData("1234A")]
    [InlineData("14AB")]
    public void Constructor_InvalidInput_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => new SeatNumber(value));
    }
}
