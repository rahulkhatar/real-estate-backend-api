namespace RealEstate.Application.DTOs;

public class AgentDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ProfileImageUrl { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public List<string> Specialization { get; set; } = [];
    public int YearsOfExperience { get; set; }
    public int TotalBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal CommissionPercentage { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UpdateAgentProfileDto
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ProfileImageUrl { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public List<string> Specialization { get; set; } = [];
    public int YearsOfExperience { get; set; }
}

public class UpdateAgentCommissionDto
{
    public decimal CommissionPercentage { get; set; }
}

public class AgentEarningsDto
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public decimal CommissionPercentage { get; set; }

    public int TotalDeals { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal AverageCommissionPerDeal { get; set; }

    public List<MonthlyEarningDto> MonthlyBreakdown { get; set; } = [];
    public List<EarningEntryDto> History { get; set; } = [];
}

public class MonthlyEarningDto
{
    /// <summary>"yyyy-MM"</summary>
    public string Month { get; set; } = string.Empty;
    public int Deals { get; set; }
    public decimal Revenue { get; set; }
    public decimal Commission { get; set; }
}

public class EarningEntryDto
{
    public string BookingId { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public DateTime CompletedAt { get; set; }
}
