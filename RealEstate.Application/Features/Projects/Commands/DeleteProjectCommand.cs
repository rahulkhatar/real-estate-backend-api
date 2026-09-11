using MediatR;
using RealEstate.Application.Common.Caching;
using RealEstate.Application.Features.Properties.Commands;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Projects.Commands;

// IRequest<Unit> (not the bare, non-generic IRequest) -- CacheInvalidationBehavior<TRequest,
// TResponse> can only close over requests that actually implement IRequest<TResponse>, and a
// bare IRequest does NOT satisfy IRequest<Unit> in MediatR 12.x, so the invalidation behavior
// was being silently skipped for this command and the delete never busted the read cache.
public record DeleteProjectCommand(string Id) : IRequest<Unit>, IInvalidatesCache
{
    public IReadOnlyCollection<CacheEntityType> AffectedEntityTypes => [CacheEntityType.Project];
}

public class DeleteProjectCommandHandler(
    IProjectRepository repository,
    IPropertyRepository propertyRepository,
    IMediator mediator) : IRequestHandler<DeleteProjectCommand, Unit>
{
    public async Task<Unit> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Core.Entities.Project), request.Id);

        // Cascade: each property's own delete handler cascades further into its units.
        var properties = await propertyRepository.GetByProjectIdAsync(project.Id, cancellationToken);
        foreach (var property in properties)
            await mediator.Send(new DeletePropertyCommand(property.Id), cancellationToken);

        await repository.DeleteAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
