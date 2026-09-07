using AutoMapper;
using MediatR;
using RealEstate.Application.Common;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Inquiries.Specifications;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Inquiries.Queries;

// Not ICacheableQuery -- an Admin marking a lead Contacted/Closed needs to see that reflected
// immediately, same reasoning as Bookings never being cached.
public record GetAllInquiriesQuery(InquiryQueryParams Query) : IRequest<PagedResponse<InquiryDto>>;

public class GetAllInquiriesQueryHandler(IInquiryRepository repository, IMapper mapper)
    : IRequestHandler<GetAllInquiriesQuery, PagedResponse<InquiryDto>>
{
    public async Task<PagedResponse<InquiryDto>> Handle(GetAllInquiriesQuery request, CancellationToken cancellationToken)
    {
        var spec = new InquiryFilterSpecification(request.Query);
        var result = await repository.ListPagedAsync(spec, request.Query.PageNumber, request.Query.PageSize, cancellationToken);
        var items = mapper.Map<List<InquiryDto>>(result.Items);
        return PagedResponse<InquiryDto>.From(result, items);
    }
}
