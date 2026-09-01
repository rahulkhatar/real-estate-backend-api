using AutoMapper;
using MediatR;
using RealEstate.Application.Common.Caching;
using RealEstate.Application.DTOs;
using RealEstate.Core.Entities;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.UnitLayouts.Queries;

public record GetUnitLayoutByIdQuery(string Id) : IRequest<UnitLayoutDto>, ICacheableQuery
{
    public CacheEntityType EntityType => CacheEntityType.UnitLayout;
}

public class GetUnitLayoutByIdQueryHandler(IUnitLayoutRepository repository, IMapper mapper)
    : IRequestHandler<GetUnitLayoutByIdQuery, UnitLayoutDto>
{
    public async Task<UnitLayoutDto> Handle(GetUnitLayoutByIdQuery request, CancellationToken cancellationToken)
    {
        var layout = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(UnitLayout), request.Id);

        return mapper.Map<UnitLayoutDto>(layout);
    }
}

public record GetLayoutsByUnitQuery(string UnitId) : IRequest<List<UnitLayoutDto>>, ICacheableQuery
{
    public CacheEntityType EntityType => CacheEntityType.UnitLayout;
}

public class GetLayoutsByUnitQueryHandler(IUnitLayoutRepository repository, IMapper mapper)
    : IRequestHandler<GetLayoutsByUnitQuery, List<UnitLayoutDto>>
{
    public async Task<List<UnitLayoutDto>> Handle(GetLayoutsByUnitQuery request, CancellationToken cancellationToken)
    {
        var layouts = await repository.GetByUnitIdAsync(request.UnitId, cancellationToken);
        return mapper.Map<List<UnitLayoutDto>>(layouts);
    }
}

public record GetAllUnitLayoutsQuery : IRequest<List<UnitLayoutDto>>, ICacheableQuery
{
    public CacheEntityType EntityType => CacheEntityType.UnitLayout;
}

public class GetAllUnitLayoutsQueryHandler(IUnitLayoutRepository repository, IMapper mapper)
    : IRequestHandler<GetAllUnitLayoutsQuery, List<UnitLayoutDto>>
{
    public async Task<List<UnitLayoutDto>> Handle(GetAllUnitLayoutsQuery request, CancellationToken cancellationToken)
    {
        var layouts = await repository.ListAllAsync(cancellationToken);
        return mapper.Map<List<UnitLayoutDto>>(layouts);
    }
}
