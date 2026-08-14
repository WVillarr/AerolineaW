using AerolineaW.Application.Bookings;
using AerolineaW.Application.Common;
using AerolineaW.Application.Flights;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AerolineaW.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class FlightsController(IFlightService flightService, IBookingService bookingService) : ControllerBase
{
    /// <summary>Lists flights, most imminent departure first.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<FlightResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PageRequest page, CancellationToken cancellationToken) =>
        Ok(await flightService.GetAllAsync(page, cancellationToken));

    /// <summary>Gets a single flight by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<FlightResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await flightService.GetByIdAsync(id, cancellationToken));

    /// <summary>Schedules a new flight. Rejects an aircraft already committed to an overlapping flight.</summary>
    [HttpPost]
    [ProducesResponseType<FlightResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateFlightRequest request, CancellationToken cancellationToken)
    {
        var flight = await flightService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = flight.Id, version = "1.0" }, flight);
    }

    /// <summary>Reassigns the aircraft operating this flight.</summary>
    [HttpPut("{id:int}/aircraft")]
    [ProducesResponseType<FlightResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignAircraft(int id, AssignAircraftRequest request, CancellationToken cancellationToken) =>
        Ok(await flightService.AssignAircraftAsync(id, request, cancellationToken));

    /// <summary>Begins boarding: Scheduled to Boarding.</summary>
    [HttpPost("{id:int}/board")]
    [ProducesResponseType<FlightResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Board(int id, CancellationToken cancellationToken) =>
        Ok(await flightService.BoardAsync(id, cancellationToken));

    /// <summary>Records departure: Boarding to InFlight.</summary>
    [HttpPost("{id:int}/depart")]
    [ProducesResponseType<FlightResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Depart(int id, [FromBody] RecordActualTimeRequest? request, CancellationToken cancellationToken) =>
        Ok(await flightService.DepartAsync(id, request ?? new RecordActualTimeRequest(null), cancellationToken));

    /// <summary>Records arrival: InFlight to Landed.</summary>
    [HttpPost("{id:int}/land")]
    [ProducesResponseType<FlightResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Land(int id, [FromBody] RecordActualTimeRequest? request, CancellationToken cancellationToken) =>
        Ok(await flightService.LandAsync(id, request ?? new RecordActualTimeRequest(null), cancellationToken));

    /// <summary>Marks the flight Delayed.</summary>
    [HttpPost("{id:int}/delay")]
    [ProducesResponseType<FlightResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delay(int id, CancellationToken cancellationToken) =>
        Ok(await flightService.DelayAsync(id, cancellationToken));

    /// <summary>Returns a Delayed flight to Scheduled.</summary>
    [HttpPost("{id:int}/resume")]
    [ProducesResponseType<FlightResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResumeFromDelay(int id, CancellationToken cancellationToken) =>
        Ok(await flightService.ResumeFromDelayAsync(id, cancellationToken));

    /// <summary>Cancels the flight and every active booking on it.</summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType<FlightResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken) =>
        Ok(await flightService.CancelAsync(id, cancellationToken));

    /// <summary>Lists every booking on this flight.</summary>
    [HttpGet("{id:int}/bookings")]
    [ProducesResponseType<IReadOnlyList<BookingResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBookings(int id, CancellationToken cancellationToken) =>
        Ok(await bookingService.GetByFlightIdAsync(id, cancellationToken));

    /// <summary>
    /// Books a passenger onto this flight. Under concurrent requests for the last seats,
    /// the losing request gets a 409 instead of an oversold flight.
    /// </summary>
    [HttpPost("{id:int}/bookings")]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBooking(int id, CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var booking = await bookingService.CreateAsync(id, request, cancellationToken);
        return CreatedAtAction(
            nameof(BookingsController.GetById), "Bookings", new { id = booking.Id, version = "1.0" }, booking);
    }
}
