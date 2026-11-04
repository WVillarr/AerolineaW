using AerolineaW.Domain.Entities;
using AerolineaW.Domain.Enums;
using AerolineaW.Domain.Exceptions;
using AerolineaW.Domain.ValueObjects;
using Xunit;

namespace AerolineaW.Domain.UnitTests.Entities;

public class BookingTests
{
    private static readonly DateTime Departure = new(2027, 3, 10, 14, 0, 0, DateTimeKind.Utc);

    private static Booking CreateBooking(DateTime departureUtc) =>
        TestFactory
            .CreateFlight(departureUtc: departureUtc, arrivalUtc: departureUtc.AddHours(2))
            .Book(TestFactory.CreatePassenger(), new SeatNumber("1A"), 10m, 100m, departureUtc.AddDays(-2));

    [Fact]
    public void CheckIn_TooEarly_Throws()
    {
        var booking = CreateBooking(Departure);
        var tooEarly = Departure - Flight.CheckInOpens - TimeSpan.FromMinutes(1);

        Assert.Throws<CheckInWindowException>(() => booking.CheckIn(tooEarly));
    }

    [Fact]
    public void CheckIn_TooLate_Throws()
    {
        var booking = CreateBooking(Departure);
        var tooLate = Departure - Flight.CheckInCutoff + TimeSpan.FromMinutes(1);

        Assert.Throws<CheckInWindowException>(() => booking.CheckIn(tooLate));
    }

    [Fact]
    public void CheckIn_WithinWindow_Succeeds()
    {
        var booking = CreateBooking(Departure);
        var withinWindow = Departure - TimeSpan.FromHours(2);

        booking.CheckIn(withinWindow);

        Assert.Equal(BookingStatus.CheckedIn, booking.Status);
        Assert.Equal(withinWindow, booking.CheckedInAtUtc);
    }

    [Fact]
    public void CheckIn_AlreadyCheckedIn_Throws()
    {
        var booking = CreateBooking(Departure);
        var withinWindow = Departure - TimeSpan.FromHours(2);
        booking.CheckIn(withinWindow);

        Assert.Throws<InvalidBookingStatusException>(() => booking.CheckIn(withinWindow));
    }

    [Fact]
    public void CheckIn_AfterCancellation_Throws()
    {
        var booking = CreateBooking(Departure);
        booking.Cancel();

        Assert.Throws<InvalidBookingStatusException>(() => booking.CheckIn(Departure - TimeSpan.FromHours(2)));
    }

    [Fact]
    public void Cancel_ReleasesSeatOnFlight()
    {
        var flight = TestFactory.CreateFlight(departureUtc: Departure, arrivalUtc: Departure.AddHours(2));
        var booking = flight.Book(TestFactory.CreatePassenger(), new SeatNumber("1A"), 10m, 100m, Departure.AddDays(-2));

        booking.Cancel();

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(0, flight.SeatsBooked);
        Assert.Equal(0m, flight.PayloadBookedKg);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Throws()
    {
        var booking = CreateBooking(Departure);
        booking.Cancel();

        Assert.Throws<InvalidBookingStatusException>(booking.Cancel);
    }

    [Theory]
    [InlineData(FlightStatus.Scheduled)]
    [InlineData(FlightStatus.Delayed)]
    [InlineData(FlightStatus.Boarding)]
    public void Cancel_BeforeDeparture_ReleasesCapacity(FlightStatus status)
    {
        var booking = CreateBooking(Departure);
        if (status == FlightStatus.Delayed)
        {
            booking.Flight.Delay();
        }
        else if (status == FlightStatus.Boarding)
        {
            booking.Flight.Board();
        }

        booking.Cancel();

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(0, booking.Flight.SeatsBooked);
        Assert.Equal(0m, booking.Flight.PayloadBookedKg);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cancel_AfterDeparture_PreservesBookingAndCounters(bool landed)
    {
        var booking = CreateBooking(Departure);
        booking.Flight.Board();
        booking.Flight.Depart(Departure);
        if (landed)
        {
            booking.Flight.Land(Departure.AddHours(2));
        }

        Assert.Throws<InvalidBookingStatusException>(booking.Cancel);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(1, booking.Flight.SeatsBooked);
        Assert.Equal(10m, booking.Flight.PayloadBookedKg);
    }
}
