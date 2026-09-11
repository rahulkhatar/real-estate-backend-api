using MediatR;
using RealEstate.Application.Common.Caching;
using RealEstate.Application.Features.Units.Commands;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Properties.Commands;

// IRequest<Unit>, not bare IRequest -- see DeleteProjectCommand for why: CacheInvalidationBehavior
// only applies to requests that actually implement IRequest<TResponse>.
public record DeletePropertyCommand(string Id) : IRequest<Unit>, IInvalidatesCache
{
    // Also bumps Project: deletion decrements the parent project's TotalProperties.
    public IReadOnlyCollection<CacheEntityType> AffectedEntityTypes => [CacheEntityType.Property, CacheEntityType.Project];
}

public class DeletePropertyCommandHandler(
    IPropertyRepository repository,
    IUnitRepository unitRepository,
    IProjectRepository projectRepository,
    IMediator mediator) : IRequestHandler<DeletePropertyCommand, Unit>
{
    public async Task<Unit> Handle(DeletePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Core.Entities.Property), request.Id);

        // Cascade: each unit's own delete handler cascades further into its layouts/bookings/payments.
        var units = await unitRepository.GetByPropertyIdAsync(property.Id, cancellationToken);
        foreach (var unit in units)
            await mediator.Send(new DeleteUnitCommand(unit.Id), cancellationToken);

        await repository.DeleteAsync(request.Id, cancellationToken);

        var project = await projectRepository.GetByIdAsync(property.ProjectId, cancellationToken);
        if (project is not null)
        {
            project.TotalProperties = Math.Max(0, project.TotalProperties - 1);
            await projectRepository.UpdateAsync(project, cancellationToken);
        }

        return Unit.Value;
    }
}
