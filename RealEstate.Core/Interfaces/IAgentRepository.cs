using RealEstate.Core.Entities;

namespace RealEstate.Core.Interfaces;

public interface IAgentRepository : IRepository<Agent>
{
    Task<Agent?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<bool> PhoneExistsAsync(string phone, CancellationToken ct = default);
    Task<bool> LicenseNumberExistsAsync(string licenseNumber, CancellationToken ct = default);
}
