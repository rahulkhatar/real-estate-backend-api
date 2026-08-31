using MongoDB.Driver;
using RealEstate.Core.Entities;
using RealEstate.Core.Interfaces;

namespace RealEstate.Infrastructure.Persistence.Repositories;

public class AgentRepository(IMongoDbContext context)
    : GenericRepository<Agent>(context, CollectionNames.Agents), IAgentRepository
{
    public async Task<Agent?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await Collection.Find(a => a.Email == email && !a.IsDeleted).FirstOrDefaultAsync(ct);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        await Collection.Find(a => a.Email == email && !a.IsDeleted).AnyAsync(ct);

    public async Task<bool> PhoneExistsAsync(string phone, CancellationToken ct = default) =>
        await Collection.Find(a => a.Phone == phone && !a.IsDeleted).AnyAsync(ct);

    public async Task<bool> LicenseNumberExistsAsync(string licenseNumber, CancellationToken ct = default) =>
        await Collection.Find(a => a.LicenseNumber == licenseNumber && !a.IsDeleted).AnyAsync(ct);
}
