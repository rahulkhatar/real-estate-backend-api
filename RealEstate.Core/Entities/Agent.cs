using RealEstate.Core.Common;
using RealEstate.Core.Enums;

namespace RealEstate.Core.Entities;

public class Agent : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string ProfileImageUrl { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;

    public List<string> Specialization { get; set; } = [];
    public int YearsOfExperience { get; set; }

    public int TotalBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal CommissionPercentage { get; set; }

    public AgentStatus Status { get; set; } = AgentStatus.Active;
    public bool IsVerified { get; set; }

    /// <summary>Role claim baked into the JWT; "Agent" or "Admin".</summary>
    public string Role { get; set; } = "Agent";

    public DateTime? LastLogin { get; set; }
}
