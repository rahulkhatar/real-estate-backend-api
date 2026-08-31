using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.Common;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Properties.Commands;
using RealEstate.Application.Features.Properties.Queries;
using RealEstate.Application.Features.Units.Queries;

namespace RealEstate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PropertiesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponse<PropertyDto>>> GetAll([FromQuery] PropertyQueryParams query)
    {
        var result = await mediator.Send(new GetAllPropertiesQuery(query));
        return Ok(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<PropertyDto>> GetById(string id)
    {
        var result = await mediator.Send(new GetPropertyByIdQuery(id));
        return Ok(result);
    }

    [HttpGet("{id}/units")]
    [AllowAnonymous]
    public async Task<ActionResult<List<UnitDto>>> GetUnits(string id)
    {
        var result = await mediator.Send(new GetUnitsByPropertyQuery(id));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PropertyDto>> Create(CreatePropertyDto dto)
    {
        var result = await mediator.Send(new CreatePropertyCommand(dto));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PropertyDto>> Update(string id, UpdatePropertyDto dto)
    {
        var result = await mediator.Send(new UpdatePropertyCommand(id, dto));
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        await mediator.Send(new DeletePropertyCommand(id));
        return NoContent();
    }
}
