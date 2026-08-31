using RealEstate.Application.DTOs;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Specifications;

namespace RealEstate.Application.Features.Bookings.Specifications;

public class BookingFilterSpecification : BaseSpecification<Booking>
{
    public BookingFilterSpecification(BookingQueryParams query)
        : base(b => !b.IsDeleted)
    {
        if (!string.IsNullOrWhiteSpace(query.AgentId))
            AddCriteria(b => b.AgentId == query.AgentId);

        if (!string.IsNullOrWhiteSpace(query.UnitId))
            AddCriteria(b => b.UnitId == query.UnitId);

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<BookingStatus>(query.Status, true, out var status))
            AddCriteria(b => b.Status == status);

        ApplyOrderByDescending(b => b.CreatedAt);
        ApplyPaging(query.PageNumber, query.PageSize);
    }
}
