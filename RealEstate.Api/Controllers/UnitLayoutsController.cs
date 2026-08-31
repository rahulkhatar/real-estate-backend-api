using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.UnitLayouts.Commands;
using RealEstate.Application.Features.UnitLayouts.Queries;

namespace RealEstate.Api.Controllers;

[ApiController]
[Route("api/unit-layouts")]
public class UnitLayoutsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<UnitLayoutDto>>> GetAll()
    {
        var result = await mediator.Send(new GetAllUnitLayoutsQuery());
        return Ok(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<UnitLayoutDto>> GetById(string id)
    {
        var result = await mediator.Send(new GetUnitLayoutByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UnitLayoutDto>> Create(CreateUnitLayoutDto dto)
    {
        var result = await mediator.Send(new CreateUnitLayoutCommand(dto));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UnitLayoutDto>> Update(string id, UpdateUnitLayoutDto dto)
    {
        var result = await mediator.Send(new UpdateUnitLayoutCommand(id, dto));
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        await mediator.Send(new DeleteUnitLayoutCommand(id));
        return NoContent();
    }
}
