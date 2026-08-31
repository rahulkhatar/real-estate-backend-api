using AutoMapper;
using MediatR;
using RealEstate.Application.Common;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Projects.Specifications;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Projects.Queries;

public record GetAllProjectsQuery(ProjectQueryParams Query) : IRequest<PagedResponse<ProjectDto>>;

public class GetAllProjectsQueryHandler(IProjectRepository repository, IMapper mapper)
    : IRequestHandler<GetAllProjectsQuery, PagedResponse<ProjectDto>>
{
    public async Task<PagedResponse<ProjectDto>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken)
    {
        var spec = new ProjectFilterSpecification(request.Query);
        var result = await repository.ListPagedAsync(spec, request.Query.PageNumber, request.Query.PageSize, cancellationToken);
        var items = mapper.Map<List<ProjectDto>>(result.Items);
        return PagedResponse<ProjectDto>.From(result, items);
    }
}
