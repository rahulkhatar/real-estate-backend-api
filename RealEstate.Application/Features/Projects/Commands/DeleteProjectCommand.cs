using MediatR;
using RealEstate.Application.Common.Caching;
using RealEstate.Application.Features.Properties.Commands;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Projects.Commands;

public record DeleteProjectCommand(string Id) : IRequest, IInvalidatesCache
{
    public IReadOnlyCollection<CacheEntityType> AffectedEntityTypes => [CacheEntityType.Project];
}

public class DeleteProjectCommandHandler(
    IProjectRepository repository,
    IPropertyRepository propertyRepository,
    IMediator mediator) : IRequestHandler<DeleteProjectCommand>
{
    public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Core.Entities.Project), request.Id);

        // Cascade: each property's own delete handler cascades further into its units.
        var properties = await propertyRepository.GetByProjectIdAsync(project.Id, cancellationToken);
        foreach (var property in properties)
            await mediator.Send(new DeletePropertyCommand(property.Id), cancellationToken);

        await repository.DeleteAsync(request.Id, cancellationToken);
    }
}
