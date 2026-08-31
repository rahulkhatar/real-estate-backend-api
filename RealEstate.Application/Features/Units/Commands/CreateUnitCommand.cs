using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Chat.Commands;
using RealEstate.Core.Entities;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;
using RealEstate.Core.ValueObjects;

namespace RealEstate.Application.Features.Units.Commands;

public record CreateUnitCommand(CreateUnitDto Dto) : IRequest<UnitDto>;

public class CreateUnitCommandValidator : AbstractValidator<CreateUnitCommand>
{
    public CreateUnitCommandValidator()
    {
        RuleFor(x => x.Dto.PropertyId).NotEmpty();
        RuleFor(x => x.Dto.UnitNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Dto.Price).GreaterThan(0);
    }
}

public class CreateUnitCommandHandler(
    IUnitRepository repository,
    IPropertyRepository propertyRepository,
    IMediator mediator,
    ILogger<CreateUnitCommandHandler> logger,
    IMapper mapper) : IRequestHandler<CreateUnitCommand, UnitDto>
{
    public async Task<UnitDto> Handle(CreateUnitCommand request, CancellationToken cancellationToken)
    {
        var property = await propertyRepository.GetByIdAsync(request.Dto.PropertyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Core.Entities.Property), request.Dto.PropertyId);

        if (await repository.ExistsByUnitNumberAsync(property.Id, request.Dto.UnitNumber, ct: cancellationToken))
            throw new ConflictException($"Unit number '{request.Dto.UnitNumber}' already exists under this property.");

        var unit = mapper.Map<Core.Entities.Unit>(request.Dto);
        unit.PropertyId = property.Id;
        unit.ProjectId = property.ProjectId;
        unit.ProjectSnapshot = property.ProjectSnapshot;
        unit.PropertySnapshot = new PropertySnapshot { Name = property.Name, Type = property.Type.ToString() };

        var created = await repository.AddAsync(unit, cancellationToken);

        property.TotalUnits += 1;
        await propertyRepository.UpdateAsync(property, cancellationToken);

        try
        {
            await mediator.Send(new IndexUnitEmbeddingCommand(created.Id), cancellationToken);
        }
        catch (Exception ex)
        {
            // Best-effort — the unit is created either way; the AI index just falls a step behind
            // (e.g. OpenAI not configured, or a transient API error) until the next successful index.
            logger.LogWarning(ex, "Failed to index unit {UnitId} for the AI chat assistant.", created.Id);
        }

        return mapper.Map<UnitDto>(created);
    }
}
