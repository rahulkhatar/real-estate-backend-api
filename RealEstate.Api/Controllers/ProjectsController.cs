using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.Common;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Projects.Commands;
using RealEstate.Application.Features.Projects.Queries;
using RealEstate.Application.Features.Properties.Queries;

namespace RealEstate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponse<ProjectDto>>> GetAll([FromQuery] ProjectQueryParams query)
    {
        var result = await mediator.Send(new GetAllProjectsQuery(query));
        return Ok(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProjectDto>> GetById(string id)
    {
        var result = await mediator.Send(new GetProjectByIdQuery(id));
        return Ok(result);
    }

    [HttpGet("{id}/properties")]
    [AllowAnonymous]
    public async Task<ActionResult<List<PropertyDto>>> GetProperties(string id)
    {
        var result = await mediator.Send(new GetPropertiesByProjectQuery(id));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProjectDto>> Create(CreateProjectDto dto)
    {
        var result = await mediator.Send(new CreateProjectCommand(dto));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProjectDto>> Update(string id, UpdateProjectDto dto)
    {
        var result = await mediator.Send(new UpdateProjectCommand(id, dto));
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        await mediator.Send(new DeleteProjectCommand(id));
        return NoContent();
    }
}
