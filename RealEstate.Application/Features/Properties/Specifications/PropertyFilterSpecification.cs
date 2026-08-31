using RealEstate.Application.DTOs;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Specifications;

namespace RealEstate.Application.Features.Properties.Specifications;

public class PropertyFilterSpecification : BaseSpecification<Property>
{
    public PropertyFilterSpecification(PropertyQueryParams query)
        : base(p => !p.IsDeleted)
    {
        if (!string.IsNullOrWhiteSpace(query.ProjectId))
            AddCriteria(p => p.ProjectId == query.ProjectId);

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<PropertyStatus>(query.Status, true, out var status))
            AddCriteria(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Type) &&
            Enum.TryParse<PropertyType>(query.Type, true, out var type))
            AddCriteria(p => p.Type == type);

        if (query.MinPrice.HasValue)
            AddCriteria(p => p.TotalPrice >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            AddCriteria(p => p.TotalPrice <= query.MaxPrice.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
            AddCriteria(p => p.Name.ToLower().Contains(query.Search.ToLower()));

        ApplyOrderByDescending(p => p.CreatedAt);
        ApplyPaging(query.PageNumber, query.PageSize);
    }

    public PropertyFilterSpecification(string projectId) : base(p => !p.IsDeleted && p.ProjectId == projectId)
    {
        ApplyOrderByDescending(p => p.CreatedAt);
    }
}
