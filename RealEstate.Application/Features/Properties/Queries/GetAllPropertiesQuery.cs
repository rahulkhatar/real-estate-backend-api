using AutoMapper;
using MediatR;
using RealEstate.Application.Common;
using RealEstate.Application.Common.Caching;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Properties.Specifications;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Properties.Queries;

public record GetAllPropertiesQuery(PropertyQueryParams Query) : IRequest<PagedResponse<PropertyDto>>, ICacheableQuery
{
    public CacheEntityType EntityType => CacheEntityType.Property;
}

public class GetAllPropertiesQueryHandler(IPropertyRepository repository, IMapper mapper)
    : IRequestHandler<GetAllPropertiesQuery, PagedResponse<PropertyDto>>
{
    public async Task<PagedResponse<PropertyDto>> Handle(GetAllPropertiesQuery request, CancellationToken cancellationToken)
    {
        var spec = new PropertyFilterSpecification(request.Query);
        var result = await repository.ListPagedAsync(spec, request.Query.PageNumber, request.Query.PageSize, cancellationToken);
        var items = mapper.Map<List<PropertyDto>>(result.Items);
        return PagedResponse<PropertyDto>.From(result, items);
    }
}

public record GetPropertiesByProjectQuery(string ProjectId) : IRequest<List<PropertyDto>>, ICacheableQuery
{
    public CacheEntityType EntityType => CacheEntityType.Property;
}

public class GetPropertiesByProjectQueryHandler(IPropertyRepository repository, IMapper mapper)
    : IRequestHandler<GetPropertiesByProjectQuery, List<PropertyDto>>
{
    public async Task<List<PropertyDto>> Handle(GetPropertiesByProjectQuery request, CancellationToken cancellationToken)
    {
        var properties = await repository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        return mapper.Map<List<PropertyDto>>(properties);
    }
}
