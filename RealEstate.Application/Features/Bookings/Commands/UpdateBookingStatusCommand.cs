using AutoMapper;
using FluentValidation;
using MediatR;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Units.Commands;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Bookings.Commands;

public record UpdateBookingStatusCommand(string Id, UpdateBookingStatusDto Dto) : IRequest<BookingDto>;

public class UpdateBookingStatusCommandValidator : AbstractValidator<UpdateBookingStatusCommand>
{
    public UpdateBookingStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Dto.Status)
            .Must(s => Enum.TryParse<BookingStatus>(s, true, out _))
            .WithMessage("Status must be one of: Pending, Confirmed, Completed, Cancelled.");
        RuleFor(x => x.Dto.CancellationReason)
            .NotEmpty()
            .When(x => Enum.TryParse<BookingStatus>(x.Dto.Status, true, out var s) && s == BookingStatus.Cancelled)
            .WithMessage("A cancellation reason is required when cancelling a booking.");
    }
}

/// <summary>
/// Drives the booking status flow: Pending/Confirmed can move to Confirmed, Completed, or
/// Cancelled; Completed and Cancelled are terminal. Completing a booking sells the unit
/// (reusing UpdateUnitStatusCommand so the existing property/project sold-cascade still
/// fires); cancelling a Pending/Confirmed booking frees the unit back to Available.
/// </summary>
public class UpdateBookingStatusCommandHandler(
    IBookingRepository bookingRepository,
    IUnitRepository unitRepository,
    IAgentRepository agentRepository,
    IMediator mediator,
    IMapper mapper) : IRequestHandler<UpdateBookingStatusCommand, BookingDto>
{
    public async Task<BookingDto> Handle(UpdateBookingStatusCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), request.Id);

        var newStatus = Enum.Parse<BookingStatus>(request.Dto.Status, true);

        if (newStatus == booking.Status)
            return mapper.Map<BookingDto>(booking);

        if (booking.Status is BookingStatus.Completed or BookingStatus.Cancelled)
            throw new ConflictException($"This booking is already {booking.Status.ToString().ToLower()} and can no longer be changed.");

        booking.Status = newStatus;

        switch (newStatus)
        {
            case BookingStatus.Completed:
                await mediator.Send(new UpdateUnitStatusCommand(booking.UnitId, nameof(UnitStatus.Sold)), cancellationToken);

                var completingAgent = await agentRepository.GetByIdAsync(booking.AgentId, cancellationToken);
                if (completingAgent is not null)
                {
                    completingAgent.TotalRevenue += booking.TotalAmount;
                    await agentRepository.UpdateAsync(completingAgent, cancellationToken);
                }
                break;

            case BookingStatus.Cancelled:
                booking.CancellationReason = request.Dto.CancellationReason;
                booking.CancelledAt = DateTime.UtcNow;
                await unitRepository.UpdateStatusAsync(booking.UnitId, UnitStatus.Available, cancellationToken);

                var agent = await agentRepository.GetByIdAsync(booking.AgentId, cancellationToken);
                if (agent is not null)
                {
                    agent.TotalBookings = Math.Max(0, agent.TotalBookings - 1);
                    await agentRepository.UpdateAsync(agent, cancellationToken);
                }
                break;
        }

        await bookingRepository.UpdateAsync(booking, cancellationToken);
        return mapper.Map<BookingDto>(booking);
    }
}
