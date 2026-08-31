using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Chat.Commands;

namespace RealEstate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController(IMediator mediator) : ControllerBase
{
    /// <summary>Public — a browsing visitor doesn't need to be logged in to ask the assistant about listings.</summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponseDto>> Ask(AskChatDto dto)
    {
        var result = await mediator.Send(new AskChatCommand(dto));
        return Ok(result);
    }

    [HttpPost("reindex")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReindexResultDto>> Reindex()
    {
        var result = await mediator.Send(new ReindexListingsCommand());
        return Ok(result);
    }
}
