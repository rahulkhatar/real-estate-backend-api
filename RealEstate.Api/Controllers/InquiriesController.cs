using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.Common;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Inquiries.Commands;
using RealEstate.Application.Features.Inquiries.Queries;

namespace RealEstate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InquiriesController(IMediator mediator) : ControllerBase
{
    /// <summary>Public -- the Contact Us form and any listing's "Enquire" button submit here, no auth required.</summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<InquiryDto>> Create(CreateInquiryDto dto)
    {
        var result = await mediator.Send(new CreateInquiryCommand(dto));
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResponse<InquiryDto>>> GetAll([FromQuery] InquiryQueryParams query)
    {
        var result = await mediator.Send(new GetAllInquiriesQuery(query));
        return Ok(result);
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<InquiryDto>> UpdateStatus(string id, UpdateInquiryStatusDto dto)
    {
        var result = await mediator.Send(new UpdateInquiryStatusCommand(id, dto));
        return Ok(result);
    }
}
