using RealEstate.Application.DTOs;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Specifications;

namespace RealEstate.Application.Features.Projects.Specifications;

public class ProjectFilterSpecification : BaseSpecification<Project>
{
    public ProjectFilterSpecification(ProjectQueryParams query)
        : base(p => !p.IsDeleted)
    {
        if (!string.IsNullOrWhiteSpace(query.City))
            AddCriteria(p => p.Location.City.ToLower() == query.City.ToLower());

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<ProjectStatus>(query.Status, true, out var status))
            AddCriteria(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Type) &&
            Enum.TryParse<PropertyType>(query.Type, true, out var type))
            AddCriteria(p => p.Type == type);

        if (!string.IsNullOrWhiteSpace(query.Search))
            AddCriteria(p => p.Name.ToLower().Contains(query.Search.ToLower()));

        ApplyOrderByDescending(p => p.CreatedAt);
        ApplyPaging(query.PageNumber, query.PageSize);
    }
}
