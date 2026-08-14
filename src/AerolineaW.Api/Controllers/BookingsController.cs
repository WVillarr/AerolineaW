using AerolineaW.Application.Bookings;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AerolineaW.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class BookingsController(IBookingService bookingService) : ControllerBase
{
    /// <summary>Gets a single booking by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await bookingService.GetByIdAsync(id, cancellationToken));

    /// <summary>Looks a booking up by its 6-character reference (PNR) — no flight id required.</summary>
    [HttpGet("by-reference/{reference}")]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByReference(string reference, CancellationToken cancellationToken) =>
        Ok(await bookingService.GetByReferenceAsync(reference, cancellationToken));

    /// <summary>Checks a passenger in. Only allowed within the check-in window before departure.</summary>
    [HttpPost("{id:int}/check-in")]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CheckIn(int id, CancellationToken cancellationToken) =>
        Ok(await bookingService.CheckInAsync(id, cancellationToken));

    /// <summary>Cancels a booking and releases its seat back to the flight.</summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken) =>
        Ok(await bookingService.CancelAsync(id, cancellationToken));
}
