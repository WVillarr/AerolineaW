using AerolineaW.Domain.Entities;
using AerolineaW.Domain.Enums;
using AerolineaW.Domain.Exceptions;
using Xunit;

namespace AerolineaW.Domain.UnitTests.Entities;

public class AircraftTests
{
    [Theory]
    [InlineData("", "Boeing 737", 150, 2000)]
    [InlineData("N12345", "", 150, 2000)]
    [InlineData("N12345", "Boeing 737", 0, 2000)]
    [InlineData("N12345", "Boeing 737", -1, 2000)]
    [InlineData("N12345", "Boeing 737", 150, 0)]
    public void Constructor_InvalidInput_Throws(string registration, string model, int seatCapacity, decimal maxPayloadKg)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Aircraft(registration, model, seatCapacity, maxPayloadKg));
    }

    [Fact]
    public void Constructor_ValidInput_StartsInService()
    {
        var aircraft = new Aircraft("n12345", "Boeing 737", 150, 2000m);

        Assert.Equal(AircraftStatus.InService, aircraft.Status);
        Assert.Equal("N12345", aircraft.Registration);
    }

    [Fact]
    public void SendToMaintenance_FromInService_Succeeds()
    {
        var aircraft = TestFactory.CreateAircraft();

        aircraft.SendToMaintenance();

        Assert.Equal(AircraftStatus.InMaintenance, aircraft.Status);
    }

    [Fact]
    public void SendToMaintenance_WhenRetired_Throws()
    {
        var aircraft = TestFactory.CreateAircraft();
        aircraft.Retire();

        Assert.Throws<AircraftUnavailableException>(aircraft.SendToMaintenance);
    }

    [Fact]
    public void ReturnToService_FromMaintenance_Succeeds()
    {
        var aircraft = TestFactory.CreateAircraft();
        aircraft.SendToMaintenance();

        aircraft.ReturnToService();

        Assert.Equal(AircraftStatus.InService, aircraft.Status);
    }

    [Fact]
    public void ReturnToService_WhenRetired_Throws()
    {
        var aircraft = TestFactory.CreateAircraft();
        aircraft.Retire();

        Assert.Throws<AircraftUnavailableException>(aircraft.ReturnToService);
    }

    [Fact]
    public void Retire_IsIdempotent()
    {
        var aircraft = TestFactory.CreateAircraft();

        aircraft.Retire();
        aircraft.Retire();

        Assert.Equal(AircraftStatus.Retired, aircraft.Status);
    }
}
