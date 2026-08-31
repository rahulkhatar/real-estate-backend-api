using AutoMapper;
using MediatR;
using RealEstate.Application.Common;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Bookings.Specifications;
using RealEstate.Core.Entities;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Bookings.Queries;

public record GetBookingByIdQuery(string Id) : IRequest<BookingDto>;

public class GetBookingByIdQueryHandler(IBookingRepository repository, IMapper mapper)
    : IRequestHandler<GetBookingByIdQuery, BookingDto>
{
    public async Task<BookingDto> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var booking = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), request.Id);

        return mapper.Map<BookingDto>(booking);
    }
}

public record GetAllBookingsQuery(BookingQueryParams Query) : IRequest<PagedResponse<BookingDto>>;

public class GetAllBookingsQueryHandler(IBookingRepository repository, IMapper mapper)
    : IRequestHandler<GetAllBookingsQuery, PagedResponse<BookingDto>>
{
    public async Task<PagedResponse<BookingDto>> Handle(GetAllBookingsQuery request, CancellationToken cancellationToken)
    {
        var spec = new BookingFilterSpecification(request.Query);
        var result = await repository.ListPagedAsync(spec, request.Query.PageNumber, request.Query.PageSize, cancellationToken);
        var items = mapper.Map<List<BookingDto>>(result.Items);
        return PagedResponse<BookingDto>.From(result, items);
    }
}

public record GetBookingsByAgentQuery(string AgentId) : IRequest<List<BookingDto>>;

public class GetBookingsByAgentQueryHandler(IBookingRepository repository, IMapper mapper)
    : IRequestHandler<GetBookingsByAgentQuery, List<BookingDto>>
{
    public async Task<List<BookingDto>> Handle(GetBookingsByAgentQuery request, CancellationToken cancellationToken)
    {
        var bookings = await repository.GetByAgentIdAsync(request.AgentId, cancellationToken);
        return mapper.Map<List<BookingDto>>(bookings);
    }
}
