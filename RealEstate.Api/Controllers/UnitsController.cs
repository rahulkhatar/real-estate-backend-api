using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.Common;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.UnitLayouts.Queries;
using RealEstate.Application.Features.Units.Commands;
using RealEstate.Application.Features.Units.Queries;

namespace RealEstate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UnitsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponse<UnitDto>>> GetAll([FromQuery] UnitQueryParams query)
    {
        var result = await mediator.Send(new GetAllUnitsQuery(query));
        return Ok(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<UnitDto>> GetById(string id)
    {
        var result = await mediator.Send(new GetUnitByIdQuery(id));
        return Ok(result);
    }

    [HttpGet("{id}/layouts")]
    [AllowAnonymous]
    public async Task<ActionResult<List<UnitLayoutDto>>> GetLayouts(string id)
    {
        var result = await mediator.Send(new GetLayoutsByUnitQuery(id));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UnitDto>> Create(CreateUnitDto dto)
    {
        var result = await mediator.Send(new CreateUnitCommand(dto));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UnitDto>> Update(string id, UpdateUnitDto dto)
    {
        var result = await mediator.Send(new UpdateUnitCommand(id, dto));
        return Ok(result);
    }

    /// <summary>
    /// Admin-only manual override for Available/Booked. Sold is rejected here on purpose — a unit
    /// can only be marked Sold by completing its booking with a successful payment (the internal
    /// cascade from UpdateBookingStatusCommandHandler uses UpdateUnitStatusCommand directly and
    /// isn't affected by this guard).
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UnitDto>> UpdateStatus(string id, [FromBody] UpdateUnitStatusRequest request)
    {
        if (string.Equals(request.Status, "Sold", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "A unit can only be marked Sold by completing its booking with a successful payment." });

        var result = await mediator.Send(new UpdateUnitStatusCommand(id, request.Status));
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        await mediator.Send(new DeleteUnitCommand(id));
        return NoContent();
    }
}

public record UpdateUnitStatusRequest(string Status);
