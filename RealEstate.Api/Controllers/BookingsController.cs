using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.Common;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Bookings.Commands;
using RealEstate.Application.Features.Bookings.Queries;
using RealEstate.Application.Interfaces;

namespace RealEstate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<BookingDto>>> GetAll([FromQuery] BookingQueryParams query)
    {
        // Agents only ever see their own bookings — Admins can see everyone's (optionally filtered by agentId).
        if (!currentUser.IsInRole("Admin"))
            query.AgentId = currentUser.AgentId;

        var result = await mediator.Send(new GetAllBookingsQuery(query));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookingDto>> GetById(string id)
    {
        var result = await mediator.Send(new GetBookingByIdQuery(id));

        if (!currentUser.IsInRole("Admin") && result.AgentId != currentUser.AgentId)
            return Forbid();

        return Ok(result);
    }

    /// <summary>Admin-only — bookings are created centrally, crediting whichever agent closed the deal.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BookingDto>> Create(CreateBookingDto dto)
    {
        var result = await mediator.Send(new CreateBookingCommand(dto));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// A booking can only reach Completed (which sells the unit) through a successful payment —
    /// never a direct manual status change, even by an Admin. This endpoint is left for Cancelled
    /// (and, if ever needed, Confirmed); Completed is rejected here on purpose.
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BookingDto>> UpdateStatus(string id, UpdateBookingStatusDto dto)
    {
        if (string.Equals(dto.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "A booking can only be completed by a successful payment, not set directly." });

        var result = await mediator.Send(new UpdateBookingStatusCommand(id, dto));
        return Ok(result);
    }
}
