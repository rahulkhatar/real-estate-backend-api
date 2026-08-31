using AutoMapper;
using FluentValidation;
using MediatR;
using RealEstate.Application.DTOs;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;
using RealEstate.Core.ValueObjects;

namespace RealEstate.Application.Features.Bookings.Commands;

public record CreateBookingCommand(CreateBookingDto Dto) : IRequest<BookingDto>;

public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.Dto.UnitId).NotEmpty();
        RuleFor(x => x.Dto.AgentId).NotEmpty();
        RuleFor(x => x.Dto.CustomerName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Dto.CustomerEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Dto.CustomerPhone).NotEmpty();
        RuleFor(x => x.Dto.BookingAmount).GreaterThan(0);
    }
}

/// <summary>Only an Admin can call this (enforced at the controller) — they pick which agent the booking is credited to.</summary>
public class CreateBookingCommandHandler(
    IBookingRepository bookingRepository,
    IUnitRepository unitRepository,
    IPropertyRepository propertyRepository,
    IAgentRepository agentRepository,
    IMapper mapper) : IRequestHandler<CreateBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var unit = await unitRepository.GetByIdAsync(request.Dto.UnitId, cancellationToken)
            ?? throw new NotFoundException(nameof(Core.Entities.Unit), request.Dto.UnitId);

        if (unit.Status != UnitStatus.Available)
            throw new ConflictException($"Unit {unit.UnitNumber} is not available for booking (current status: {unit.Status}).");

        if (await bookingRepository.HasActiveBookingForUnitAsync(unit.Id, cancellationToken))
            throw new ConflictException($"Unit {unit.UnitNumber} already has an active booking.");

        var agent = await agentRepository.GetByIdAsync(request.Dto.AgentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Agent), request.Dto.AgentId);

        var booking = new Booking
        {
            UnitId = unit.Id,
            PropertyId = unit.PropertyId,
            ProjectId = unit.ProjectId,
            AgentId = agent.Id,
            UnitSnapshot = new UnitSnapshot
            {
                UnitNumber = unit.UnitNumber,
                Type = unit.Type.ToString(),
                ImageUrl = unit.Images.Count > 0 ? unit.Images[0].Url : string.Empty,
            },
            PropertySnapshot = unit.PropertySnapshot,
            ProjectSnapshot = unit.ProjectSnapshot,
            AgentSnapshot = new AgentSnapshot
            {
                Name = agent.Name,
                Email = agent.Email,
                Phone = agent.Phone,
                LicenseNumber = agent.LicenseNumber,
            },
            CustomerName = request.Dto.CustomerName,
            CustomerEmail = request.Dto.CustomerEmail,
            CustomerPhone = request.Dto.CustomerPhone,
            CustomerAddress = request.Dto.CustomerAddress,
            BookingAmount = request.Dto.BookingAmount,
            TotalAmount = unit.Price,
            Notes = request.Dto.Notes,
            Status = BookingStatus.Pending,
        };

        var created = await bookingRepository.AddAsync(booking, cancellationToken);

        await unitRepository.UpdateStatusAsync(unit.Id, UnitStatus.Booked, cancellationToken);

        agent.TotalBookings += 1;
        await agentRepository.UpdateAsync(agent, cancellationToken);

        return mapper.Map<BookingDto>(created);
    }
}
