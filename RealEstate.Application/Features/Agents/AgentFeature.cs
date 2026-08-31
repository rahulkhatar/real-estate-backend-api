using AutoMapper;
using FluentValidation;
using MediatR;
using RealEstate.Application.DTOs;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Agents;

public record GetAgentByIdQuery(string Id) : IRequest<AgentDto>;

public class GetAgentByIdQueryHandler(IAgentRepository repository, IMapper mapper)
    : IRequestHandler<GetAgentByIdQuery, AgentDto>
{
    public async Task<AgentDto> Handle(GetAgentByIdQuery request, CancellationToken cancellationToken)
    {
        var agent = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Agent), request.Id);

        return mapper.Map<AgentDto>(agent);
    }
}

/// <summary>Powers the Admin's agent-picker dropdown when creating a booking.</summary>
public record GetAllAgentsQuery : IRequest<List<AgentDto>>;

public class GetAllAgentsQueryHandler(IAgentRepository repository, IMapper mapper)
    : IRequestHandler<GetAllAgentsQuery, List<AgentDto>>
{
    public async Task<List<AgentDto>> Handle(GetAllAgentsQuery request, CancellationToken cancellationToken)
    {
        var agents = await repository.ListAllAsync(cancellationToken);
        return mapper.Map<List<AgentDto>>(agents.OrderBy(a => a.Name));
    }
}

public record UpdateAgentProfileCommand(string Id, UpdateAgentProfileDto Dto) : IRequest<AgentDto>;

public class UpdateAgentProfileCommandValidator : AbstractValidator<UpdateAgentProfileCommand>
{
    public UpdateAgentProfileCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(150);
    }
}

public class UpdateAgentProfileCommandHandler(IAgentRepository repository, IMapper mapper)
    : IRequestHandler<UpdateAgentProfileCommand, AgentDto>
{
    public async Task<AgentDto> Handle(UpdateAgentProfileCommand request, CancellationToken cancellationToken)
    {
        var agent = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Agent), request.Id);

        mapper.Map(request.Dto, agent);
        await repository.UpdateAsync(agent, cancellationToken);
        return mapper.Map<AgentDto>(agent);
    }
}

/// <summary>Admin-only — an agent's commission rate is a business decision, not agent self-service (kept out of UpdateAgentProfileDto).</summary>
public record UpdateAgentCommissionCommand(string Id, UpdateAgentCommissionDto Dto) : IRequest<AgentDto>;

public class UpdateAgentCommissionCommandValidator : AbstractValidator<UpdateAgentCommissionCommand>
{
    public UpdateAgentCommissionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Dto.CommissionPercentage).InclusiveBetween(0, 100);
    }
}

public class UpdateAgentCommissionCommandHandler(IAgentRepository repository, IMapper mapper)
    : IRequestHandler<UpdateAgentCommissionCommand, AgentDto>
{
    public async Task<AgentDto> Handle(UpdateAgentCommissionCommand request, CancellationToken cancellationToken)
    {
        var agent = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Agent), request.Id);

        agent.CommissionPercentage = request.Dto.CommissionPercentage;
        await repository.UpdateAsync(agent, cancellationToken);
        return mapper.Map<AgentDto>(agent);
    }
}

/// <summary>
/// Earnings are derived from Completed bookings rather than persisted as a separate ledger —
/// Booking already carries everything needed (TotalAmount, AgentId, snapshots), and deriving
/// avoids a second source of truth that could drift. Commission uses the agent's CURRENT
/// CommissionPercentage applied uniformly across history (no rate-at-time-of-sale locking).
/// </summary>
public record GetAgentEarningsQuery(string AgentId) : IRequest<AgentEarningsDto>;

public class GetAgentEarningsQueryHandler(IAgentRepository agentRepository, IBookingRepository bookingRepository)
    : IRequestHandler<GetAgentEarningsQuery, AgentEarningsDto>
{
    public async Task<AgentEarningsDto> Handle(GetAgentEarningsQuery request, CancellationToken cancellationToken)
    {
        var agent = await agentRepository.GetByIdAsync(request.AgentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Agent), request.AgentId);

        var bookings = await bookingRepository.GetByAgentIdAsync(request.AgentId, cancellationToken);
        var completed = bookings.Where(b => b.Status == BookingStatus.Completed).OrderByDescending(b => b.UpdatedAt).ToList();

        var totalRevenue = completed.Sum(b => b.TotalAmount);
        var totalCommission = Math.Round(totalRevenue * agent.CommissionPercentage / 100, 2);

        var monthly = completed
            .GroupBy(b => new { b.UpdatedAt.Year, b.UpdatedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g =>
            {
                var revenue = g.Sum(b => b.TotalAmount);
                return new MonthlyEarningDto
                {
                    Month = $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                    Deals = g.Count(),
                    Revenue = revenue,
                    Commission = Math.Round(revenue * agent.CommissionPercentage / 100, 2),
                };
            })
            .ToList();

        var history = completed.Select(b => new EarningEntryDto
        {
            BookingId = b.Id,
            UnitNumber = b.UnitSnapshot.UnitNumber,
            PropertyName = b.PropertySnapshot.Name,
            ProjectName = b.ProjectSnapshot.Name,
            CustomerName = b.CustomerName,
            ImageUrl = b.UnitSnapshot.ImageUrl,
            TotalAmount = b.TotalAmount,
            CommissionAmount = Math.Round(b.TotalAmount * agent.CommissionPercentage / 100, 2),
            CompletedAt = b.UpdatedAt,
        }).ToList();

        return new AgentEarningsDto
        {
            AgentId = agent.Id,
            AgentName = agent.Name,
            CommissionPercentage = agent.CommissionPercentage,
            TotalDeals = completed.Count,
            TotalRevenue = totalRevenue,
            TotalCommission = totalCommission,
            AverageCommissionPerDeal = completed.Count > 0 ? Math.Round(totalCommission / completed.Count, 2) : 0,
            MonthlyBreakdown = monthly,
            History = history,
        };
    }
}
