using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Chat.Commands;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;
using RealEstate.Core.ValueObjects;

namespace RealEstate.Application.Features.Properties.Commands;

public record UpdatePropertyCommand(string Id, UpdatePropertyDto Dto) : IRequest<PropertyDto>;

public class UpdatePropertyCommandValidator : AbstractValidator<UpdatePropertyCommand>
{
    public UpdatePropertyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(150);
    }
}

public class UpdatePropertyCommandHandler(
    IPropertyRepository repository,
    IUnitRepository unitRepository,
    IMediator mediator,
    ILogger<UpdatePropertyCommandHandler> logger,
    IMapper mapper) : IRequestHandler<UpdatePropertyCommand, PropertyDto>
{
    public async Task<PropertyDto> Handle(UpdatePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Core.Entities.Property), request.Id);

        var previousName = property.Name;
        var previousType = property.Type;

        mapper.Map(request.Dto, property);
        await repository.UpdateAsync(property, cancellationToken);

        if (previousName != property.Name || previousType != property.Type)
        {
            var snapshot = new PropertySnapshot { Name = property.Name, Type = property.Type.ToString() };
            await unitRepository.UpdatePropertySnapshotAsync(property.Id, snapshot, cancellationToken);

            // The units' cached snapshot fields are updated above, but the AI's embedding text
            // still references the old name — re-embed every affected unit so chat search stays accurate.
            var units = await unitRepository.GetByPropertyIdAsync(property.Id, cancellationToken);
            await EmbeddingReindexHelper.ReindexUnitsAsync(units, mediator, logger, cancellationToken);
        }

        return mapper.Map<PropertyDto>(property);
    }
}
