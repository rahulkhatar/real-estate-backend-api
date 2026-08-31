using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Bookings.Queries;
using RealEstate.Application.Features.Payments.Commands;
using RealEstate.Application.Features.Payments.Queries;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Enums;

namespace RealEstate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PaymentOrderResponseDto>> CreateOrder(CreatePaymentOrderDto dto)
    {
        var result = await mediator.Send(new CreatePaymentOrderCommand(dto));
        return Ok(result);
    }

    [HttpPost("manual")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PaymentDto>> RecordManual(RecordManualPaymentDto dto)
    {
        var result = await mediator.Send(new RecordManualPaymentCommand(dto));
        return Ok(result);
    }

    [HttpGet("booking/{bookingId}")]
    public async Task<ActionResult<PaymentDto>> GetByBooking(string bookingId)
    {
        var booking = await mediator.Send(new GetBookingByIdQuery(bookingId));
        if (!currentUser.IsInRole("Admin") && booking.AgentId != currentUser.AgentId)
            return Forbid();

        var result = await mediator.Send(new GetPaymentByBookingIdQuery(bookingId));
        return result is null ? NotFound(new { message = "No payment found for this booking." }) : Ok(result);
    }

    [HttpGet("booking/{bookingId}/history")]
    public async Task<ActionResult<List<PaymentDto>>> GetHistory(string bookingId)
    {
        var booking = await mediator.Send(new GetBookingByIdQuery(bookingId));
        if (!currentUser.IsInRole("Admin") && booking.AgentId != currentUser.AgentId)
            return Forbid();

        var result = await mediator.Send(new GetPaymentHistoryByBookingIdQuery(bookingId));
        return Ok(result);
    }

    [HttpPost("webhook/stripe")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook()
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();

        await mediator.Send(new ProcessPaymentWebhookCommand(PaymentProvider.Stripe, payload, signature));
        return Ok();
    }

    [HttpPost("webhook/razorpay")]
    [AllowAnonymous]
    public async Task<IActionResult> RazorpayWebhook()
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        var signature = Request.Headers["X-Razorpay-Signature"].ToString();

        await mediator.Send(new ProcessPaymentWebhookCommand(PaymentProvider.Razorpay, payload, signature));
        return Ok();
    }
}
