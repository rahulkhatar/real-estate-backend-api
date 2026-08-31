using MongoDB.Driver;
using RealEstate.Core.Entities;

namespace RealEstate.Infrastructure.Persistence;

/// <summary>Creates the indexes the query patterns in SKILL_DATABASE.md rely on. Idempotent — safe to run on every boot.</summary>
public static class IndexInitializer
{
    public static async Task InitializeAsync(IMongoDbContext context, CancellationToken ct = default)
    {
        var projects = context.GetCollection<Project>(CollectionNames.Projects);
        await projects.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<Project>(Builders<Project>.IndexKeys.Ascending(p => p.Status).Ascending("location.city")),
                new CreateIndexModel<Project>(Builders<Project>.IndexKeys.Descending(p => p.CreatedAt))
            ], ct);

        var properties = context.GetCollection<Property>(CollectionNames.Properties);
        await properties.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<Property>(Builders<Property>.IndexKeys.Ascending(p => p.ProjectId).Ascending(p => p.Status)),
                new CreateIndexModel<Property>(Builders<Property>.IndexKeys.Ascending(p => p.Type))
            ], ct);

        var units = context.GetCollection<Unit>(CollectionNames.Units);
        await units.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<Unit>(Builders<Unit>.IndexKeys.Ascending(u => u.PropertyId).Ascending(u => u.Status)),
                new CreateIndexModel<Unit>(Builders<Unit>.IndexKeys.Ascending(u => u.ProjectId).Ascending(u => u.Type)),
                new CreateIndexModel<Unit>(Builders<Unit>.IndexKeys.Ascending(u => u.Status))
            ], ct);

        var unitLayouts = context.GetCollection<UnitLayout>(CollectionNames.UnitLayouts);
        await unitLayouts.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<UnitLayout>(Builders<UnitLayout>.IndexKeys.Ascending(l => l.UnitId)),
                new CreateIndexModel<UnitLayout>(Builders<UnitLayout>.IndexKeys.Ascending(l => l.PropertyId))
            ], ct);

        var agents = context.GetCollection<Agent>(CollectionNames.Agents);
        var uniqueOptions = new CreateIndexOptions<Agent>
        {
            Unique = true,
            PartialFilterExpression = Builders<Agent>.Filter.Eq(a => a.IsDeleted, false)
        };
        await agents.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<Agent>(Builders<Agent>.IndexKeys.Ascending(a => a.Email), uniqueOptions),
                new CreateIndexModel<Agent>(Builders<Agent>.IndexKeys.Ascending(a => a.Phone), uniqueOptions),
                new CreateIndexModel<Agent>(Builders<Agent>.IndexKeys.Ascending(a => a.LicenseNumber), uniqueOptions),
                new CreateIndexModel<Agent>(Builders<Agent>.IndexKeys.Ascending(a => a.Status))
            ], ct);

        var bookings = context.GetCollection<Booking>(CollectionNames.Bookings);
        await bookings.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<Booking>(Builders<Booking>.IndexKeys.Ascending(b => b.UnitId)),
                new CreateIndexModel<Booking>(Builders<Booking>.IndexKeys.Ascending(b => b.AgentId).Ascending(b => b.Status)),
                new CreateIndexModel<Booking>(Builders<Booking>.IndexKeys.Ascending(b => b.Status)),
                new CreateIndexModel<Booking>(Builders<Booking>.IndexKeys.Descending(b => b.CreatedAt))
            ], ct);

        var payments = context.GetCollection<Payment>(CollectionNames.Payments);
        await payments.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<Payment>(Builders<Payment>.IndexKeys.Ascending(p => p.BookingId)),
                new CreateIndexModel<Payment>(Builders<Payment>.IndexKeys.Ascending(p => p.Provider).Ascending(p => p.ProviderOrderId)),
                new CreateIndexModel<Payment>(Builders<Payment>.IndexKeys.Ascending(p => p.Status))
            ], ct);

        var listingEmbeddings = context.GetCollection<ListingEmbedding>(CollectionNames.ListingEmbeddings);
        await listingEmbeddings.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<ListingEmbedding>(Builders<ListingEmbedding>.IndexKeys.Ascending(e => e.UnitId))
            ], ct);
    }
}
