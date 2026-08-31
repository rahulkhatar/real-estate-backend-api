using RealEstate.Core.Entities;

namespace RealEstate.Application.Features.Chat;

/// <summary>Shared between the bulk reindex and the single-unit auto-index so both describe a unit identically.</summary>
internal static class ListingTextBuilder
{
    public static string Build(Unit unit) =>
        $"Unit {unit.UnitNumber}, a {unit.Type} in {unit.PropertySnapshot.Name} at {unit.ProjectSnapshot.Name} " +
        $"({unit.ProjectSnapshot.City}). {unit.Rooms} rooms, {unit.Bathrooms} bathrooms, {unit.Balconies} balconies, " +
        $"{unit.Size.Value} {unit.Size.Unit}, floor {unit.Floor}, facing {unit.Facing}. Price: {unit.Price:N0} INR " +
        $"({unit.PricePerSqft:N0}/sqft). Status: {unit.Status}." +
        (unit.Amenities.Count > 0 ? $" Amenities: {string.Join(", ", unit.Amenities)}." : string.Empty) +
        (unit.Features.Count > 0 ? $" Features: {string.Join(", ", unit.Features)}." : string.Empty);
}
