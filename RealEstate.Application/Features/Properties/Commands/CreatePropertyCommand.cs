using AutoMapper;
using FluentValidation;
using MediatR;
using RealEstate.Application.DTOs;
using RealEstate.Core.Entities;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;
using RealEstate.Core.ValueObjects;

namespace RealEstate.Application.Features.Properties.Commands;

public record CreatePropertyCommand(CreatePropertyDto Dto) : IRequest<PropertyDto>;

public class CreatePropertyCommandValidator : AbstractValidator<CreatePropertyCommand>
{
    public CreatePropertyCommandValidator()
    {
        RuleFor(x => x.Dto.ProjectId).NotEmpty();
        RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Dto.BasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dto.TotalPrice).GreaterThanOrEqualTo(0);
    }
}

public class CreatePropertyCommandHandler(
    IPropertyRepository repository,
    IProjectRepository projectRepository,
    IMapper mapper) : IRequestHandler<CreatePropertyCommand, PropertyDto>
{
    public async Task<PropertyDto> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.Dto.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Core.Entities.Project), request.Dto.ProjectId);

        var property = mapper.Map<Property>(request.Dto);
        property.ProjectSnapshot = new ProjectSnapshot { Name = project.Name, City = project.Location.City };

        var created = await repository.AddAsync(property, cancellationToken);

        project.TotalProperties += 1;
        await projectRepository.UpdateAsync(project, cancellationToken);

        return mapper.Map<PropertyDto>(created);
    }
}
