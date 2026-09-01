using AutoMapper;
using MediatR;
using RealEstate.Application.Common.Caching;
using RealEstate.Application.DTOs;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Properties.Queries;

public record GetPropertyByIdQuery(string Id) : IRequest<PropertyDto>, ICacheableQuery
{
    public CacheEntityType EntityType => CacheEntityType.Property;
}

public class GetPropertyByIdQueryHandler(IPropertyRepository repository, IMapper mapper)
    : IRequestHandler<GetPropertyByIdQuery, PropertyDto>
{
    public async Task<PropertyDto> Handle(GetPropertyByIdQuery request, CancellationToken cancellationToken)
    {
        var property = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Core.Entities.Property), request.Id);

        return mapper.Map<PropertyDto>(property);
    }
}
