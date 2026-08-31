using AutoMapper;
using FluentValidation;
using MediatR;
using RealEstate.Application.DTOs;
using RealEstate.Core.Entities;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Projects.Commands;

public record CreateProjectCommand(CreateProjectDto Dto) : IRequest<ProjectDto>;

public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Dto.Location).NotNull();
        RuleFor(x => x.Dto.Location.City).NotEmpty().When(x => x.Dto.Location is not null);
        RuleFor(x => x.Dto.Location.Address).NotEmpty().When(x => x.Dto.Location is not null);
        RuleFor(x => x.Dto.StartDate)
            .LessThan(x => x.Dto.CompletionDate)
            .WithMessage("Start date must be before completion date.");
        RuleFor(x => x.Dto.Type).Must(BeAValidPropertyType).WithMessage("Invalid project type.");
        RuleFor(x => x.Dto.Status).Must(BeAValidProjectStatus).WithMessage("Invalid project status.");
    }

    private static bool BeAValidPropertyType(string value) =>
        Enum.TryParse<Core.Enums.PropertyType>(value, true, out _);

    private static bool BeAValidProjectStatus(string value) =>
        Enum.TryParse<Core.Enums.ProjectStatus>(value, true, out _);
}

public class CreateProjectCommandHandler(IProjectRepository repository, IMapper mapper)
    : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = mapper.Map<Project>(request.Dto);
        var created = await repository.AddAsync(project, cancellationToken);
        return mapper.Map<ProjectDto>(created);
    }
}
