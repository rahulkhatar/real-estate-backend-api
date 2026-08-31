using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Entities;
using RealEstate.Core.Interfaces;

namespace RealEstate.Infrastructure.Persistence;

/// <summary>
/// Bootstraps exactly one Admin account from configuration (env vars in production) so that
/// [Authorize(Roles = "Admin")] endpoints are reachable without a separate promotion flow.
/// Configure Admin:Email / Admin:Password / Admin:Name; skipped (with a warning) if unset.
/// No-ops if an Admin already exists.
/// </summary>
public static class AdminSeeder
{
    public static async Task SeedAsync(
        IAgentRepository agentRepository,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken ct = default)
    {
        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Admin:Email / Admin:Password not configured — skipping admin bootstrap. " +
                               "Set them (e.g. via environment variables or user-secrets) to create the first Admin account.");
            return;
        }

        var existing = await agentRepository.GetByEmailAsync(email.Trim().ToLowerInvariant(), ct);
        if (existing is not null)
        {
            if (existing.Role != "Admin")
            {
                existing.Role = "Admin";
                await agentRepository.UpdateAsync(existing, ct);
                logger.LogInformation("Promoted existing agent {Email} to Admin.", email);
            }
            return;
        }

        var admin = new Agent
        {
            Name = configuration["Admin:Name"] ?? "Administrator",
            Email = email.Trim().ToLowerInvariant(),
            Phone = configuration["Admin:Phone"] ?? "0000000000",
            LicenseNumber = "ADMIN",
            PasswordHash = passwordHasher.Hash(password),
            Role = "Admin",
            Status = Core.Enums.AgentStatus.Active,
            IsVerified = true
        };

        await agentRepository.AddAsync(admin, ct);
        logger.LogInformation("Seeded initial Admin account {Email}.", email);
    }
}
