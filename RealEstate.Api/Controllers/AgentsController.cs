using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Agents;
using RealEstate.Application.Features.Bookings.Queries;
using RealEstate.Application.Interfaces;

namespace RealEstate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentsController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<AgentDto>>> GetAll()
    {
        var result = await mediator.Send(new GetAllAgentsQuery());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AgentDto>> GetById(string id)
    {
        var result = await mediator.Send(new GetAgentByIdQuery(id));
        return Ok(result);
    }

    [HttpGet("me")]
    public async Task<ActionResult<AgentDto>> GetCurrent()
    {
        if (currentUser.AgentId is null) return Unauthorized();
        var result = await mediator.Send(new GetAgentByIdQuery(currentUser.AgentId));
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AgentDto>> UpdateProfile(string id, UpdateAgentProfileDto dto)
    {
        // Agents may only edit their own profile; admins may edit any.
        if (currentUser.AgentId != id && !currentUser.IsInRole("Admin"))
            return Forbid();

        var result = await mediator.Send(new UpdateAgentProfileCommand(id, dto));
        return Ok(result);
    }

    [HttpGet("{id}/bookings")]
    public async Task<ActionResult<List<BookingDto>>> GetBookings(string id)
    {
        if (currentUser.AgentId != id && !currentUser.IsInRole("Admin"))
            return Forbid();

        var result = await mediator.Send(new GetBookingsByAgentQuery(id));
        return Ok(result);
    }

    [HttpGet("{id}/earnings")]
    public async Task<ActionResult<AgentEarningsDto>> GetEarnings(string id)
    {
        if (currentUser.AgentId != id && !currentUser.IsInRole("Admin"))
            return Forbid();

        var result = await mediator.Send(new GetAgentEarningsQuery(id));
        return Ok(result);
    }

    [HttpPut("{id}/commission")]
    public async Task<ActionResult<AgentDto>> UpdateCommission(string id, UpdateAgentCommissionDto dto)
    {
        if (!currentUser.IsInRole("Admin"))
            return Forbid();

        var result = await mediator.Send(new UpdateAgentCommissionCommand(id, dto));
        return Ok(result);
    }
}
