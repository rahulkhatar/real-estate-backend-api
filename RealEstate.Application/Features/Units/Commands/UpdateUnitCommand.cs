using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Chat.Commands;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Units.Commands;

public record UpdateUnitCommand(string Id, UpdateUnitDto Dto) : IRequest<UnitDto>;

public class UpdateUnitCommandValidator : AbstractValidator<UpdateUnitCommand>
{
    public UpdateUnitCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Dto.Price).GreaterThan(0);
    }
}

public class UpdateUnitCommandHandler(
    IUnitRepository repository,
    IMediator mediator,
    ILogger<UpdateUnitCommandHandler> logger,
    IMapper mapper) : IRequestHandler<UpdateUnitCommand, UnitDto>
{
    public async Task<UnitDto> Handle(UpdateUnitCommand request, CancellationToken cancellationToken)
    {
        var unit = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Core.Entities.Unit), request.Id);

        // Status is changed via the dedicated UpdateUnitStatusCommand so that the
        // property/project sold-count cascade always runs — never overwrite it here.
        var status = unit.Status;
        mapper.Map(request.Dto, unit);
        unit.Status = status;

        await repository.UpdateAsync(unit, cancellationToken);

        try
        {
            await mediator.Send(new IndexUnitEmbeddingCommand(unit.Id), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to re-index unit {UnitId} for the AI chat assistant.", unit.Id);
        }

        return mapper.Map<UnitDto>(unit);
    }
}
