using AerolineaW.Domain.Entities;
using AerolineaW.Domain.Enums;
using Xunit;

namespace AerolineaW.Domain.UnitTests.Entities;

public class PassengerTests
{
    [Theory]
    [InlineData("", "AB123456", "Country", "+1 555 0100")]
    [InlineData("Jane Doe", "", "Country", "+1 555 0100")]
    [InlineData("Jane Doe", "AB123456", "", "+1 555 0100")]
    [InlineData("Jane Doe", "AB123456", "Country", "")]
    public void Constructor_EmptyRequiredField_Throws(string fullName, string documentNumber, string country, string phone)
    {
        Assert.Throws<ArgumentException>(() => new Passenger(fullName, DocumentType.Passport, documentNumber, country, phone));
    }

    [Fact]
    public void Constructor_ValidInput_TrimsTextFields()
    {
        var passenger = new Passenger(" Jane Doe ", DocumentType.NationalId, " AB123456 ", " Country ", " +1 555 0100 ");

        Assert.Equal("Jane Doe", passenger.FullName);
        Assert.Equal("AB123456", passenger.DocumentNumber);
        Assert.Equal(DocumentType.NationalId, passenger.DocumentType);
    }

    [Fact]
    public void UpdateContactDetails_ValidInput_UpdatesFields()
    {
        var passenger = TestFactory.CreatePassenger();

        passenger.UpdateContactDetails("New Country", "+1 555 0199");

        Assert.Equal("New Country", passenger.Country);
        Assert.Equal("+1 555 0199", passenger.Phone);
    }

    [Fact]
    public void UpdateContactDetails_EmptyPhone_Throws()
    {
        var passenger = TestFactory.CreatePassenger();

        Assert.Throws<ArgumentException>(() => passenger.UpdateContactDetails("Country", ""));
    }
}
