using AutoMapper;
using MediatR;
using RealEstate.Application.Common.Caching;
using RealEstate.Application.DTOs;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Projects.Queries;

public record GetProjectByIdQuery(string Id) : IRequest<ProjectDto>, ICacheableQuery
{
    public CacheEntityType EntityType => CacheEntityType.Project;
}

public class GetProjectByIdQueryHandler(IProjectRepository repository, IMapper mapper)
    : IRequestHandler<GetProjectByIdQuery, ProjectDto>
{
    public async Task<ProjectDto> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Core.Entities.Project), request.Id);

        return mapper.Map<ProjectDto>(project);
    }
}
