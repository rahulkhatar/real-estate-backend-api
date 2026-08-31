using RealEstate.Core.Common;
using RealEstate.Core.Enums;
using RealEstate.Core.ValueObjects;

namespace RealEstate.Core.Entities;

public class Booking : BaseEntity
{
    public string UnitId { get; set; } = string.Empty;
    public string PropertyId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;

    public UnitSnapshot UnitSnapshot { get; set; } = new();
    public PropertySnapshot PropertySnapshot { get; set; } = new();
    public ProjectSnapshot ProjectSnapshot { get; set; } = new();
    public AgentSnapshot AgentSnapshot { get; set; } = new();

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;

    public DateTime BookingDate { get; set; } = DateTime.UtcNow;

    /// <summary>Token/booking amount actually paid so far.</summary>
    public decimal BookingAmount { get; set; }

    /// <summary>Snapshot of the unit's price at booking time.</summary>
    public decimal TotalAmount { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public string Notes { get; set; } = string.Empty;
    public string CancellationReason { get; set; } = string.Empty;
    public DateTime? CancelledAt { get; set; }
}
