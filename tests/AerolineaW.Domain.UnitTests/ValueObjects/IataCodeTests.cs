using AerolineaW.Domain.ValueObjects;
using Xunit;

namespace AerolineaW.Domain.UnitTests.ValueObjects;

public class IataCodeTests
{
    [Fact]
    public void Constructor_LowercaseInput_NormalizesToUppercase()
    {
        var code = new IataCode("mia");

        Assert.Equal("MIA", code.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("MI")]
    [InlineData("MIAM")]
    [InlineData("M1A")]
    public void Constructor_InvalidInput_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => new IataCode(value));
    }

    [Fact]
    public void EqualityIsByValue()
    {
        Assert.Equal(new IataCode("MIA"), new IataCode("mia"));
    }
}
