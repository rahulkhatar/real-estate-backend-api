using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Chat.Commands;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Projects.Commands;

public record UpdateProjectCommand(string Id, UpdateProjectDto Dto) : IRequest<ProjectDto>;

public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Dto.StartDate)
            .LessThan(x => x.Dto.CompletionDate)
            .WithMessage("Start date must be before completion date.");
    }
}

public class UpdateProjectCommandHandler(IProjectRepository repository, IPropertyRepository propertyRepository,
    IUnitRepository unitRepository, IMediator mediator, ILogger<UpdateProjectCommandHandler> logger, IMapper mapper)
    : IRequestHandler<UpdateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Core.Entities.Project), request.Id);

        var previousName = project.Name;
        var previousCity = project.Location.City;

        mapper.Map(request.Dto, project);
        await repository.UpdateAsync(project, cancellationToken);

        // Snapshots on child collections are denormalized for read performance; refresh them
        // only when the fields they cache actually changed.
        if (previousName != project.Name || previousCity != project.Location.City)
        {
            var snapshot = new Core.ValueObjects.ProjectSnapshot { Name = project.Name, City = project.Location.City };
            await propertyRepository.UpdateProjectSnapshotAsync(project.Id, snapshot, cancellationToken);
            await unitRepository.UpdateProjectSnapshotAsync(project.Id, snapshot, cancellationToken);

            // Same reasoning as the property-rename cascade: units' cached snapshot fields are
            // updated above, but the AI's embedding text still references the old name/city.
            var units = await unitRepository.GetByProjectIdAsync(project.Id, cancellationToken);
            await EmbeddingReindexHelper.ReindexUnitsAsync(units, mediator, logger, cancellationToken);
        }

        return mapper.Map<ProjectDto>(project);
    }
}
