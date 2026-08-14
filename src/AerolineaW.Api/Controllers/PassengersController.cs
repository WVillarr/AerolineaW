using AerolineaW.Application.Passengers;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AerolineaW.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class PassengersController(IPassengerService passengerService) : ControllerBase
{
    /// <summary>Lists every passenger.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PassengerResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await passengerService.GetAllAsync(cancellationToken));

    /// <summary>Gets a single passenger by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<PassengerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await passengerService.GetByIdAsync(id, cancellationToken));

    /// <summary>Registers a new passenger.</summary>
    [HttpPost]
    [ProducesResponseType<PassengerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreatePassengerRequest request, CancellationToken cancellationToken)
    {
        var passenger = await passengerService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = passenger.Id, version = "1.0" }, passenger);
    }

    /// <summary>Updates a passenger's contact details.</summary>
    [HttpPut("{id:int}/contact")]
    [ProducesResponseType<PassengerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateContact(int id, UpdatePassengerContactRequest request, CancellationToken cancellationToken) =>
        Ok(await passengerService.UpdateContactDetailsAsync(id, request, cancellationToken));

    /// <summary>Deletes a passenger.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await passengerService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
