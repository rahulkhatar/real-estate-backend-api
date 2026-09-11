using MediatR;
using Microsoft.Extensions.Logging;
using RealEstate.Application.Common.Caching;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Units.Commands;

public record DeleteUnitCommand(string Id) : IRequest, IInvalidatesCache
{
    // Also bumps Property: deletion decrements the parent property's TotalUnits.
    public IReadOnlyCollection<CacheEntityType> AffectedEntityTypes => [CacheEntityType.Unit, CacheEntityType.Property];
}

public class DeleteUnitCommandHandler(
    IUnitRepository repository,
    IPropertyRepository propertyRepository,
    IUnitLayoutRepository unitLayoutRepository,
    IBookingRepository bookingRepository,
    IPaymentRepository paymentRepository,
    IListingEmbeddingRepository embeddingRepository,
    ILogger<DeleteUnitCommandHandler> logger) : IRequestHandler<DeleteUnitCommand>
{
    public async Task Handle(DeleteUnitCommand request, CancellationToken cancellationToken)
    {
        var unit = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Core.Entities.Unit), request.Id);

        await repository.DeleteAsync(request.Id, cancellationToken);

        var layouts = await unitLayoutRepository.GetByUnitIdAsync(request.Id, cancellationToken);
        foreach (var layout in layouts)
            await unitLayoutRepository.DeleteAsync(layout.Id, cancellationToken);

        var bookings = await bookingRepository.GetByUnitIdAsync(request.Id, cancellationToken);
        foreach (var booking in bookings)
        {
            var payments = await paymentRepository.GetHistoryByBookingIdAsync(booking.Id, cancellationToken);
            foreach (var payment in payments)
                await paymentRepository.DeleteAsync(payment.Id, cancellationToken);

            await bookingRepository.DeleteAsync(booking.Id, cancellationToken);
        }

        var property = await propertyRepository.GetByIdAsync(unit.PropertyId, cancellationToken);
        if (property is not null)
        {
            property.TotalUnits = Math.Max(0, property.TotalUnits - 1);
            await propertyRepository.UpdateAsync(property, cancellationToken);
        }

        try
        {
            await embeddingRepository.DeleteByUnitIdAsync(request.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to remove unit {UnitId} from the AI chat index.", request.Id);
        }
    }
}
