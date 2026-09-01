using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using RealEstate.Application.Common.Caching;
using RealEstate.Application.DTOs;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Units.Commands;

public record UpdateUnitStatusCommand(string Id, string Status) : IRequest<UnitDto>, IInvalidatesCache
{
    public IReadOnlyCollection<CacheEntityType> AffectedEntityTypes => [CacheEntityType.Unit];
}

public class UpdateUnitStatusCommandValidator : AbstractValidator<UpdateUnitStatusCommand>
{
    public UpdateUnitStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status)
            .Must(s => Enum.TryParse<UnitStatus>(s, true, out _))
            .WithMessage("Status must be one of: Available, Booked, Sold.");
    }
}

/// <summary>
/// Cascades a unit status change up to its property and project: bumps sold counters
/// and auto-flips the parent to "Sold" once every child is sold, per the platform's
/// "Unit → Property → Project" status-tracking requirement.
/// </summary>
public class UpdateUnitStatusCommandHandler(
    IUnitRepository unitRepository,
    IPropertyRepository propertyRepository,
    IProjectRepository projectRepository,
    IListingEmbeddingRepository embeddingRepository,
    ICacheService cache,
    ILogger<UpdateUnitStatusCommandHandler> logger,
    IMapper mapper) : IRequestHandler<UpdateUnitStatusCommand, UnitDto>
{
    public async Task<UnitDto> Handle(UpdateUnitStatusCommand request, CancellationToken cancellationToken)
    {
        var unit = await unitRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Core.Entities.Unit), request.Id);

        var newStatus = Enum.Parse<UnitStatus>(request.Status, true);
        if (newStatus == unit.Status)
            return mapper.Map<UnitDto>(unit);

        var wasSold = unit.Status == UnitStatus.Sold;
        var isNowSold = newStatus == UnitStatus.Sold;

        unit.Status = newStatus;
        await unitRepository.UpdateAsync(unit, cancellationToken);

        try
        {
            // Cheap field-only sync (no OpenAI call) — the unit's description hasn't changed, just its status.
            await embeddingRepository.UpdateStatusAsync(unit.Id, newStatus.ToString(), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to sync AI index status for unit {UnitId}.", unit.Id);
        }

        if (wasSold != isNowSold)
        {
            var property = await propertyRepository.GetByIdAsync(unit.PropertyId, cancellationToken);
            if (property is not null)
            {
                property.SoldUnits = Math.Max(0, property.SoldUnits + (isNowSold ? 1 : -1));

                var propertyNowFullySold = property.TotalUnits > 0 && property.SoldUnits >= property.TotalUnits;
                var propertyWasSold = property.Status == Core.Enums.PropertyStatus.Sold;
                property.Status = propertyNowFullySold ? Core.Enums.PropertyStatus.Sold : Core.Enums.PropertyStatus.Available;

                await propertyRepository.UpdateAsync(property, cancellationToken);

                // A sold-flip cascades into the Property read model -- CacheInvalidationBehavior
                // only knows about this command's own (Unit) entity type, not this conditional cascade.
                await cache.BumpVersionsAsync([CacheEntityType.Property], cancellationToken);

                if (propertyWasSold != propertyNowFullySold)
                {
                    var project = await projectRepository.GetByIdAsync(property.ProjectId, cancellationToken);
                    if (project is not null)
                    {
                        project.SoldProperties = Math.Max(0, project.SoldProperties + (propertyNowFullySold ? 1 : -1));
                        project.Status = project.TotalProperties > 0 && project.SoldProperties >= project.TotalProperties
                            ? Core.Enums.ProjectStatus.Sold
                            : project.Status == Core.Enums.ProjectStatus.Sold ? Core.Enums.ProjectStatus.Active : project.Status;

                        await projectRepository.UpdateAsync(project, cancellationToken);
                        await cache.BumpVersionsAsync([CacheEntityType.Project], cancellationToken);
                    }
                }
            }
        }

        return mapper.Map<UnitDto>(unit);
    }
}
