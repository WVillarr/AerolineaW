using AerolineaW.Application.Airports;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AerolineaW.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class AirportsController(IAirportService airportService) : ControllerBase
{
    /// <summary>Lists every airport.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AirportResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await airportService.GetAllAsync(cancellationToken));

    /// <summary>Gets a single airport by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<AirportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await airportService.GetByIdAsync(id, cancellationToken));

    /// <summary>Registers a new airport.</summary>
    [HttpPost]
    [ProducesResponseType<AirportResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateAirportRequest request, CancellationToken cancellationToken)
    {
        var airport = await airportService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = airport.Id, version = "1.0" }, airport);
    }

    /// <summary>Updates an airport's details.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<AirportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateAirportRequest request, CancellationToken cancellationToken) =>
        Ok(await airportService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Deletes an airport.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await airportService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
