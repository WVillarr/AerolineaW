using AerolineaW.Domain.ValueObjects;
using Xunit;

namespace AerolineaW.Domain.UnitTests.ValueObjects;

public class BookingReferenceTests
{
    [Theory]
    [InlineData("")]
    [InlineData("ABC12")]
    [InlineData("ABC1234")]
    [InlineData("ABC-12")]
    public void Constructor_InvalidInput_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => new BookingReference(value));
    }

    [Fact]
    public void Generate_ProducesAValidSixCharacterReference()
    {
        for (var i = 0; i < 50; i++)
        {
            var reference = BookingReference.Generate();

            Assert.Equal(6, reference.Value.Length);
            Assert.DoesNotContain(reference.Value, c => c is '0' or 'O' or '1' or 'I');
        }
    }
}
