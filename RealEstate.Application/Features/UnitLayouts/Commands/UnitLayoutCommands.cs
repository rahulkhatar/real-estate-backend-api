using AutoMapper;
using FluentValidation;
using MediatR;
using RealEstate.Application.DTOs;
using RealEstate.Core.Entities;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;
using RealEstate.Core.ValueObjects;

namespace RealEstate.Application.Features.UnitLayouts.Commands;

public record CreateUnitLayoutCommand(CreateUnitLayoutDto Dto) : IRequest<UnitLayoutDto>;

public class CreateUnitLayoutCommandValidator : AbstractValidator<CreateUnitLayoutCommand>
{
    public CreateUnitLayoutCommandValidator()
    {
        RuleFor(x => x.Dto.UnitId).NotEmpty();
        RuleFor(x => x.Dto.LayoutType).NotEmpty();
    }
}

public class CreateUnitLayoutCommandHandler(
    IUnitLayoutRepository repository,
    IUnitRepository unitRepository,
    IMapper mapper) : IRequestHandler<CreateUnitLayoutCommand, UnitLayoutDto>
{
    public async Task<UnitLayoutDto> Handle(CreateUnitLayoutCommand request, CancellationToken cancellationToken)
    {
        var unit = await unitRepository.GetByIdAsync(request.Dto.UnitId, cancellationToken)
            ?? throw new NotFoundException(nameof(Core.Entities.Unit), request.Dto.UnitId);

        var layout = mapper.Map<UnitLayout>(request.Dto);
        layout.PropertyId = unit.PropertyId;
        layout.ProjectId = unit.ProjectId;
        layout.UnitSnapshot = new UnitSnapshot { UnitNumber = unit.UnitNumber, Type = unit.Type.ToString() };

        var created = await repository.AddAsync(layout, cancellationToken);
        return mapper.Map<UnitLayoutDto>(created);
    }
}

public record UpdateUnitLayoutCommand(string Id, UpdateUnitLayoutDto Dto) : IRequest<UnitLayoutDto>;

public class UpdateUnitLayoutCommandValidator : AbstractValidator<UpdateUnitLayoutCommand>
{
    public UpdateUnitLayoutCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Dto.LayoutType).NotEmpty();
    }
}

public class UpdateUnitLayoutCommandHandler(IUnitLayoutRepository repository, IMapper mapper)
    : IRequestHandler<UpdateUnitLayoutCommand, UnitLayoutDto>
{
    public async Task<UnitLayoutDto> Handle(UpdateUnitLayoutCommand request, CancellationToken cancellationToken)
    {
        var layout = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(UnitLayout), request.Id);

        mapper.Map(request.Dto, layout);
        await repository.UpdateAsync(layout, cancellationToken);
        return mapper.Map<UnitLayoutDto>(layout);
    }
}

public record DeleteUnitLayoutCommand(string Id) : IRequest;

public class DeleteUnitLayoutCommandHandler(IUnitLayoutRepository repository) : IRequestHandler<DeleteUnitLayoutCommand>
{
    public async Task Handle(DeleteUnitLayoutCommand request, CancellationToken cancellationToken)
    {
        _ = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(UnitLayout), request.Id);

        await repository.DeleteAsync(request.Id, cancellationToken);
    }
}
