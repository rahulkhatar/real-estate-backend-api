using MediatR;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Properties.Commands;

public record DeletePropertyCommand(string Id) : IRequest;

public class DeletePropertyCommandHandler(
    IPropertyRepository repository,
    IUnitRepository unitRepository,
    IProjectRepository projectRepository) : IRequestHandler<DeletePropertyCommand>
{
    public async Task Handle(DeletePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Core.Entities.Property), request.Id);

        var units = await unitRepository.GetByPropertyIdAsync(property.Id, cancellationToken);
        if (units.Count > 0)
            throw new ConflictException(
                $"Cannot delete property '{property.Name}': it still has {units.Count} unit(s) attached.");

        await repository.DeleteAsync(request.Id, cancellationToken);

        var project = await projectRepository.GetByIdAsync(property.ProjectId, cancellationToken);
        if (project is not null)
        {
            project.TotalProperties = Math.Max(0, project.TotalProperties - 1);
            await projectRepository.UpdateAsync(project, cancellationToken);
        }
    }
}
