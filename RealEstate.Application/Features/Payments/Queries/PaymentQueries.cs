using AutoMapper;
using MediatR;
using RealEstate.Application.DTOs;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Payments.Queries;

public record GetPaymentByBookingIdQuery(string BookingId) : IRequest<PaymentDto?>;

public class GetPaymentByBookingIdQueryHandler(IPaymentRepository paymentRepository, IMapper mapper)
    : IRequestHandler<GetPaymentByBookingIdQuery, PaymentDto?>
{
    public async Task<PaymentDto?> Handle(GetPaymentByBookingIdQuery request, CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.GetByBookingIdAsync(request.BookingId, cancellationToken);
        return payment is null ? null : mapper.Map<PaymentDto>(payment);
    }
}

public record GetPaymentHistoryByBookingIdQuery(string BookingId) : IRequest<List<PaymentDto>>;

public class GetPaymentHistoryByBookingIdQueryHandler(IPaymentRepository paymentRepository, IMapper mapper)
    : IRequestHandler<GetPaymentHistoryByBookingIdQuery, List<PaymentDto>>
{
    public async Task<List<PaymentDto>> Handle(GetPaymentHistoryByBookingIdQuery request, CancellationToken cancellationToken)
    {
        var payments = await paymentRepository.GetHistoryByBookingIdAsync(request.BookingId, cancellationToken);
        return mapper.Map<List<PaymentDto>>(payments);
    }
}
