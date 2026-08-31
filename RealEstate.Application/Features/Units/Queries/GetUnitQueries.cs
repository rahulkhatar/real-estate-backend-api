using AutoMapper;
using MediatR;
using RealEstate.Application.Common;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Units.Specifications;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Units.Queries;

public record GetUnitByIdQuery(string Id) : IRequest<UnitDto>;

public class GetUnitByIdQueryHandler(IUnitRepository repository, IMapper mapper)
    : IRequestHandler<GetUnitByIdQuery, UnitDto>
{
    public async Task<UnitDto> Handle(GetUnitByIdQuery request, CancellationToken cancellationToken)
    {
        var unit = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Core.Entities.Unit), request.Id);

        return mapper.Map<UnitDto>(unit);
    }
}

public record GetAllUnitsQuery(UnitQueryParams Query) : IRequest<PagedResponse<UnitDto>>;

public class GetAllUnitsQueryHandler(IUnitRepository repository, IMapper mapper)
    : IRequestHandler<GetAllUnitsQuery, PagedResponse<UnitDto>>
{
    public async Task<PagedResponse<UnitDto>> Handle(GetAllUnitsQuery request, CancellationToken cancellationToken)
    {
        var spec = new UnitFilterSpecification(request.Query);
        var result = await repository.ListPagedAsync(spec, request.Query.PageNumber, request.Query.PageSize, cancellationToken);
        var items = mapper.Map<List<UnitDto>>(result.Items);
        return PagedResponse<UnitDto>.From(result, items);
    }
}

public record GetUnitsByPropertyQuery(string PropertyId) : IRequest<List<UnitDto>>;

public class GetUnitsByPropertyQueryHandler(IUnitRepository repository, IMapper mapper)
    : IRequestHandler<GetUnitsByPropertyQuery, List<UnitDto>>
{
    public async Task<List<UnitDto>> Handle(GetUnitsByPropertyQuery request, CancellationToken cancellationToken)
    {
        var units = await repository.GetByPropertyIdAsync(request.PropertyId, cancellationToken);
        return mapper.Map<List<UnitDto>>(units);
    }
}
