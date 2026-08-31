using MediatR;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Projects.Commands;

public record DeleteProjectCommand(string Id) : IRequest;

public class DeleteProjectCommandHandler(IProjectRepository repository, IPropertyRepository propertyRepository)
    : IRequestHandler<DeleteProjectCommand>
{
    public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Core.Entities.Project), request.Id);

        var properties = await propertyRepository.GetByProjectIdAsync(project.Id, cancellationToken);
        if (properties.Count > 0)
            throw new ConflictException(
                $"Cannot delete project '{project.Name}': it still has {properties.Count} propert{(properties.Count == 1 ? "y" : "ies")} attached.");

        await repository.DeleteAsync(request.Id, cancellationToken);
    }
}
