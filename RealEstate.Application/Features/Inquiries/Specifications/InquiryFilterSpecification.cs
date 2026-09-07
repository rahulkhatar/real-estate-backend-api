using RealEstate.Application.DTOs;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Specifications;

namespace RealEstate.Application.Features.Inquiries.Specifications;

public class InquiryFilterSpecification : BaseSpecification<Inquiry>
{
    public InquiryFilterSpecification(InquiryQueryParams query)
        : base(i => !i.IsDeleted)
    {
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<InquiryStatus>(query.Status, true, out var status))
            AddCriteria(i => i.Status == status);

        ApplyOrderByDescending(i => i.CreatedAt);
        ApplyPaging(query.PageNumber, query.PageSize);
    }
}
