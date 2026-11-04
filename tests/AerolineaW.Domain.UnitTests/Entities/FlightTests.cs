using AerolineaW.Domain.Entities;
using AerolineaW.Domain.Exceptions;
using AerolineaW.Domain.ValueObjects;
using Xunit;

namespace AerolineaW.Domain.UnitTests.Entities;

public class FlightTests
{
    private static readonly DateTime Departure = new(2027, 3, 10, 14, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Arrival = Departure.AddHours(2);

    [Fact]
    public void Constructor_SameOriginAndDestination_Throws()
    {
        var airport = TestFactory.CreateAirport("MIA");

        Assert.Throws<InvalidFlightRouteException>(() =>
            TestFactory.CreateFlight(origin: airport, destination: TestFactory.CreateAirport("MIA")));
    }

    [Fact]
    public void Constructor_ArrivalNotAfterDeparture_Throws()
    {
        Assert.Throws<InvalidFlightScheduleException>(() =>
            TestFactory.CreateFlight(departureUtc: Departure, arrivalUtc: Departure));
    }

    [Fact]
    public void Constructor_AircraftNotInService_Throws()
    {
        var aircraft = TestFactory.CreateAircraft();
        aircraft.SendToMaintenance();

        Assert.Throws<AircraftUnavailableException>(() => TestFactory.CreateFlight(aircraft: aircraft));
    }

    [Fact]
    public void Constructor_ValidInput_StartsScheduledWithNoSeatsBooked()
    {
        var flight = TestFactory.CreateFlight();

        Assert.Equal(Enums.FlightStatus.Scheduled, flight.Status);
        Assert.Equal(0, flight.SeatsBooked);
        Assert.Empty(flight.Bookings);
    }

    [Fact]
    public void AssignAircraft_NotInService_Throws()
    {
        var flight = TestFactory.CreateFlight();
        var replacement = TestFactory.CreateAircraft();
        replacement.SendToMaintenance();

        Assert.Throws<AircraftUnavailableException>(() => flight.AssignAircraft(replacement));
    }

    [Fact]
    public void AssignAircraft_SmallerThanCurrentBookings_Throws()
    {
        var flight = TestFactory.CreateFlight(aircraft: TestFactory.CreateAircraft(seatCapacity: 2), departureUtc: Departure, arrivalUtc: Arrival);
        flight.Book(TestFactory.CreatePassenger("P1"), new SeatNumber("1A"), 10m, 100m, Departure.AddDays(-2));
        flight.Book(TestFactory.CreatePassenger("P2"), new SeatNumber("1B"), 10m, 100m, Departure.AddDays(-2));

        var tooSmall = TestFactory.CreateAircraft(seatCapacity: 1);

        Assert.Throws<FlightFullException>(() => flight.AssignAircraft(tooSmall));
    }

    [Fact]
    public void AssignAircraft_FlightAlreadyInFlight_Throws()
    {
        var flight = TestFactory.CreateFlight();
        flight.Board();
        flight.Depart(Departure);

        Assert.Throws<InvalidFlightTransitionException>(() => flight.AssignAircraft(TestFactory.CreateAircraft()));
    }

    [Theory]
    [InlineData(19, false)]
    [InlineData(20, true)]
    public void AssignAircraft_ValidatesExistingPayload(int capacity, bool succeeds)
    {
        var flight = TestFactory.CreateFlight(departureUtc: Departure, arrivalUtc: Arrival);
        flight.Book(TestFactory.CreatePassenger(), new SeatNumber("1A"), 20m, 100m, Departure.AddDays(-2));
        var original = flight.Aircraft;
        var replacement = TestFactory.CreateAircraft(maxPayloadKg: capacity);

        if (succeeds)
        {
            flight.AssignAircraft(replacement);
            Assert.Same(replacement, flight.Aircraft);
        }
        else
        {
            Assert.Throws<PayloadExceededException>(() => flight.AssignAircraft(replacement));
            Assert.Same(original, flight.Aircraft);
        }

        Assert.Equal(20m, flight.PayloadBookedKg);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Land_NotAfterDeparture_LeavesFlightUnchanged(int minutes)
    {
        var flight = TestFactory.CreateFlight();
        flight.Board();
        flight.Depart(Departure);

        Assert.Throws<InvalidFlightTransitionException>(() => flight.Land(Departure.AddMinutes(minutes)));
        Assert.Equal(Enums.FlightStatus.InFlight, flight.Status);
        Assert.Null(flight.ActualArrivalUtc);
    }

    [Fact]
    public void Book_ExceedsSeatCapacity_ThrowsFlightFull()
    {
        var flight = TestFactory.CreateFlight(aircraft: TestFactory.CreateAircraft(seatCapacity: 1), departureUtc: Departure, arrivalUtc: Arrival);
        flight.Book(TestFactory.CreatePassenger("P1"), new SeatNumber("1A"), 10m, 100m, Departure.AddDays(-2));

        Assert.Throws<FlightFullException>(() =>
            flight.Book(TestFactory.CreatePassenger("P2"), new SeatNumber("1B"), 10m, 100m, Departure.AddDays(-2)));
    }

    [Fact]
    public void Book_SeatAlreadyTaken_Throws()
    {
        var flight = TestFactory.CreateFlight(departureUtc: Departure, arrivalUtc: Arrival);
        flight.Book(TestFactory.CreatePassenger("P1"), new SeatNumber("1A"), 10m, 100m, Departure.AddDays(-2));

        Assert.Throws<SeatAlreadyTakenException>(() =>
            flight.Book(TestFactory.CreatePassenger("P2"), new SeatNumber("1A"), 10m, 100m, Departure.AddDays(-2)));
    }

    [Fact]
    public void Book_ExceedsMaxPayload_Throws()
    {
        var flight = TestFactory.CreateFlight(aircraft: TestFactory.CreateAircraft(maxPayloadKg: 15m), departureUtc: Departure, arrivalUtc: Arrival);

        Assert.Throws<PayloadExceededException>(() =>
            flight.Book(TestFactory.CreatePassenger(), new SeatNumber("1A"), 20m, 100m, Departure.AddDays(-2)));
    }

    [Fact]
    public void Book_AfterBookingCutoff_Throws()
    {
        var flight = TestFactory.CreateFlight(departureUtc: Departure, arrivalUtc: Arrival);
        var afterCutoff = Departure - Flight.BookingCutoff + TimeSpan.FromMinutes(1);

        Assert.Throws<BookingWindowClosedException>(() =>
            flight.Book(TestFactory.CreatePassenger(), new SeatNumber("1A"), 10m, 100m, afterCutoff));
    }

    [Fact]
    public void Book_FlightNotScheduled_Throws()
    {
        var flight = TestFactory.CreateFlight(departureUtc: Departure, arrivalUtc: Arrival);
        flight.Board();

        Assert.Throws<InvalidFlightTransitionException>(() =>
            flight.Book(TestFactory.CreatePassenger(), new SeatNumber("1A"), 10m, 100m, Departure.AddDays(-2)));
    }

    [Fact]
    public void Book_ValidRequest_IncrementsCountersAndReturnsConfirmedBooking()
    {
        var flight = TestFactory.CreateFlight(departureUtc: Departure, arrivalUtc: Arrival);

        var booking = flight.Book(TestFactory.CreatePassenger(), new SeatNumber("1A"), 12m, 250m, Departure.AddDays(-2));

        Assert.Equal(1, flight.SeatsBooked);
        Assert.Equal(12m, flight.PayloadBookedKg);
        Assert.Equal(Enums.BookingStatus.Confirmed, booking.Status);
        Assert.Single(flight.Bookings);
    }

    [Fact]
    public void CancellingABooking_FreesTheSeatForRebooking()
    {
        var flight = TestFactory.CreateFlight(departureUtc: Departure, arrivalUtc: Arrival);
        var booking = flight.Book(TestFactory.CreatePassenger("P1"), new SeatNumber("1A"), 10m, 100m, Departure.AddDays(-2));

        booking.Cancel();
        var rebooked = flight.Book(TestFactory.CreatePassenger("P2"), new SeatNumber("1A"), 10m, 100m, Departure.AddDays(-2));

        Assert.Equal(1, flight.SeatsBooked);
        Assert.Equal(new SeatNumber("1A"), rebooked.Seat);
    }

    [Fact]
    public void Board_FromScheduled_Succeeds()
    {
        var flight = TestFactory.CreateFlight();
        flight.Board();

        Assert.Equal(Enums.FlightStatus.Boarding, flight.Status);
    }

    [Fact]
    public void Board_FromNonScheduled_Throws()
    {
        var flight = TestFactory.CreateFlight();
        flight.Board();
        flight.Depart(Departure);

        Assert.Throws<InvalidFlightTransitionException>(flight.Board);
    }

    [Fact]
    public void Depart_FromBoarding_SetsActualDeparture()
    {
        var flight = TestFactory.CreateFlight();
        flight.Board();

        flight.Depart(Departure);

        Assert.Equal(Enums.FlightStatus.InFlight, flight.Status);
        Assert.Equal(Departure, flight.ActualDepartureUtc);
    }

    [Fact]
    public void Depart_WithoutBoardingFirst_Throws()
    {
        var flight = TestFactory.CreateFlight();

        Assert.Throws<InvalidFlightTransitionException>(() => flight.Depart(Departure));
    }

    [Fact]
    public void Land_FromInFlight_SetsActualArrival()
    {
        var flight = TestFactory.CreateFlight();
        flight.Board();
        flight.Depart(Departure);

        flight.Land(Arrival);

        Assert.Equal(Enums.FlightStatus.Landed, flight.Status);
        Assert.Equal(Arrival, flight.ActualArrivalUtc);
    }

    [Fact]
    public void Land_WithoutDepartingFirst_Throws()
    {
        var flight = TestFactory.CreateFlight();

        Assert.Throws<InvalidFlightTransitionException>(() => flight.Land(Arrival));
    }

    [Theory]
    [InlineData(Enums.FlightStatus.Scheduled)]
    [InlineData(Enums.FlightStatus.Boarding)]
    public void Delay_FromScheduledOrBoarding_Succeeds(Enums.FlightStatus initial)
    {
        var flight = TestFactory.CreateFlight();
        if (initial == Enums.FlightStatus.Boarding)
        {
            flight.Board();
        }

        flight.Delay();

        Assert.Equal(Enums.FlightStatus.Delayed, flight.Status);
    }

    [Fact]
    public void Delay_FromInFlight_Throws()
    {
        var flight = TestFactory.CreateFlight();
        flight.Board();
        flight.Depart(Departure);

        Assert.Throws<InvalidFlightTransitionException>(flight.Delay);
    }

    [Fact]
    public void ResumeFromDelay_FromDelayed_ReturnsToScheduled()
    {
        var flight = TestFactory.CreateFlight();
        flight.Delay();

        flight.ResumeFromDelay();

        Assert.Equal(Enums.FlightStatus.Scheduled, flight.Status);
    }

    [Fact]
    public void ResumeFromDelay_WhenNotDelayed_Throws()
    {
        var flight = TestFactory.CreateFlight();

        Assert.Throws<InvalidFlightTransitionException>(flight.ResumeFromDelay);
    }

    [Theory]
    [InlineData(Enums.FlightStatus.InFlight)]
    [InlineData(Enums.FlightStatus.Landed)]
    public void Cancel_AfterDeparture_Throws(Enums.FlightStatus terminalStatus)
    {
        var flight = TestFactory.CreateFlight();
        flight.Board();
        flight.Depart(Departure);
        if (terminalStatus == Enums.FlightStatus.Landed)
        {
            flight.Land(Arrival);
        }

        Assert.Throws<InvalidFlightTransitionException>(flight.Cancel);
    }

    [Fact]
    public void Cancel_BeforeDeparture_CancelsAllActiveBookings()
    {
        var flight = TestFactory.CreateFlight(departureUtc: Departure, arrivalUtc: Arrival);
        var booking = flight.Book(TestFactory.CreatePassenger(), new SeatNumber("1A"), 10m, 100m, Departure.AddDays(-2));

        flight.Cancel();

        Assert.Equal(Enums.FlightStatus.Cancelled, flight.Status);
        Assert.Equal(Enums.BookingStatus.Cancelled, booking.Status);
    }

    [Fact]
    public void OverlapsWithTurnaround_ScheduleWithinBuffer_ReturnsTrue()
    {
        var flight = TestFactory.CreateFlight(departureUtc: Departure, arrivalUtc: Arrival);
        var candidateDeparture = Arrival.AddMinutes(10); // less than the 45-minute turnaround buffer
        var candidateArrival = candidateDeparture.AddHours(1);

        Assert.True(flight.OverlapsWithTurnaround(candidateDeparture, candidateArrival));
    }

    [Fact]
    public void OverlapsWithTurnaround_ScheduleWellClear_ReturnsFalse()
    {
        var flight = TestFactory.CreateFlight(departureUtc: Departure, arrivalUtc: Arrival);
        var candidateDeparture = Arrival.AddHours(3);
        var candidateArrival = candidateDeparture.AddHours(1);

        Assert.False(flight.OverlapsWithTurnaround(candidateDeparture, candidateArrival));
    }
}
