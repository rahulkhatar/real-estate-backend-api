namespace RealEstate.Application.DTOs;

public class AgentSnapshotDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
}

public class BookingDto
{
    public string Id { get; set; } = string.Empty;
    public string UnitId { get; set; } = string.Empty;
    public string PropertyId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;

    public UnitSnapshotDto UnitSnapshot { get; set; } = new();
    public PropertySnapshotDto PropertySnapshot { get; set; } = new();
    public ProjectSnapshotDto ProjectSnapshot { get; set; } = new();
    public AgentSnapshotDto AgentSnapshot { get; set; } = new();

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;

    public DateTime BookingDate { get; set; }
    public decimal BookingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string CancellationReason { get; set; } = string.Empty;
    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateBookingDto
{
    public string UnitId { get; set; } = string.Empty;

    /// <summary>Which agent this booking is credited to — only an Admin creates bookings, and picks the agent explicitly.</summary>
    public string AgentId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public decimal BookingAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class UpdateBookingStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string CancellationReason { get; set; } = string.Empty;
}

public class BookingQueryParams
{
    public string? AgentId { get; set; }
    public string? UnitId { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
