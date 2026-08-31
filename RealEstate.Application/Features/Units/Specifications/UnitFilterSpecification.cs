using RealEstate.Application.DTOs;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Specifications;

namespace RealEstate.Application.Features.Units.Specifications;

public class UnitFilterSpecification : BaseSpecification<Unit>
{
    public UnitFilterSpecification(UnitQueryParams query)
        : base(u => !u.IsDeleted)
    {
        if (!string.IsNullOrWhiteSpace(query.PropertyId))
            AddCriteria(u => u.PropertyId == query.PropertyId);

        if (!string.IsNullOrWhiteSpace(query.ProjectId))
            AddCriteria(u => u.ProjectId == query.ProjectId);

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<UnitStatus>(query.Status, true, out var status))
            AddCriteria(u => u.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Type) &&
            Enum.TryParse<UnitType>(query.Type, true, out var type))
            AddCriteria(u => u.Type == type);

        if (query.MinPrice.HasValue)
            AddCriteria(u => u.Price >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            AddCriteria(u => u.Price <= query.MaxPrice.Value);

        ApplyOrderByDescending(u => u.CreatedAt);
        ApplyPaging(query.PageNumber, query.PageSize);
    }
}
