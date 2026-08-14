using AerolineaW.Application.Aircraft;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AerolineaW.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class AircraftController(IAircraftService aircraftService) : ControllerBase
{
    /// <summary>Lists every aircraft in the fleet.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AircraftResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await aircraftService.GetAllAsync(cancellationToken));

    /// <summary>Gets a single aircraft by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<AircraftResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await aircraftService.GetByIdAsync(id, cancellationToken));

    /// <summary>Registers a new aircraft, starting in service.</summary>
    [HttpPost]
    [ProducesResponseType<AircraftResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateAircraftRequest request, CancellationToken cancellationToken)
    {
        var aircraft = await aircraftService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = aircraft.Id, version = "1.0" }, aircraft);
    }

    /// <summary>Takes an in-service aircraft out of service for maintenance.</summary>
    [HttpPost("{id:int}/send-to-maintenance")]
    [ProducesResponseType<AircraftResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendToMaintenance(int id, CancellationToken cancellationToken) =>
        Ok(await aircraftService.SendToMaintenanceAsync(id, cancellationToken));

    /// <summary>Returns an aircraft from maintenance to service.</summary>
    [HttpPost("{id:int}/return-to-service")]
    [ProducesResponseType<AircraftResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReturnToService(int id, CancellationToken cancellationToken) =>
        Ok(await aircraftService.ReturnToServiceAsync(id, cancellationToken));

    /// <summary>Permanently retires an aircraft.</summary>
    [HttpPost("{id:int}/retire")]
    [ProducesResponseType<AircraftResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Retire(int id, CancellationToken cancellationToken) =>
        Ok(await aircraftService.RetireAsync(id, cancellationToken));

    /// <summary>Deletes an aircraft.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await aircraftService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
