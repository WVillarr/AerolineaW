using AerolineaW.Domain.Entities;
using AerolineaW.Domain.ValueObjects;
using Xunit;

namespace AerolineaW.Domain.UnitTests.Entities;

public class AirportTests
{
    [Theory]
    [InlineData("", "City", "Country", "America/New_York")]
    [InlineData("Name", "", "Country", "America/New_York")]
    [InlineData("Name", "City", "", "America/New_York")]
    public void Constructor_EmptyRequiredField_Throws(string name, string city, string country, string timeZoneId)
    {
        Assert.Throws<ArgumentException>(() => new Airport(new IataCode("MIA"), name, city, country, timeZoneId));
    }

    [Fact]
    public void Constructor_UnknownTimeZoneId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Airport(new IataCode("MIA"), "Name", "City", "Country", "Not/A_Zone"));
    }

    [Fact]
    public void Constructor_ValidInput_TrimsTextFields()
    {
        var airport = new Airport(new IataCode("mia"), "  Miami Intl  ", " Miami ", " USA ", "America/New_York");

        Assert.Equal("MIA", airport.Code.Value);
        Assert.Equal("Miami Intl", airport.Name);
        Assert.Equal("Miami", airport.City);
        Assert.Equal("USA", airport.Country);
    }

    [Fact]
    public void SetDetails_ValidInput_UpdatesFields()
    {
        var airport = TestFactory.CreateAirport();

        airport.SetDetails("New Name", "New City", "New Country", "Europe/Madrid");

        Assert.Equal("New Name", airport.Name);
        Assert.Equal("Europe/Madrid", airport.TimeZoneId);
    }
}
