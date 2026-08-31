namespace RealEstate.Core.Enums;

public enum ProjectStatus
{
    Upcoming,
    Active,
    Sold
}

public enum PropertyType
{
    Residential,
    Commercial
}

public enum PropertyStatus
{
    Available,
    Sold
}

public enum UnitType
{
    Studio,
    OneBhk,
    TwoBhk,
    ThreeBhk,
    FourBhk,
    Penthouse,
    Villa,
    Office,
    Shop
}

public enum UnitStatus
{
    Available,
    Booked,
    Sold
}

public enum SizeUnit
{
    Sqft,
    Sqm
}

public enum AgentStatus
{
    Active,
    Inactive,
    Suspended
}

public enum BookingStatus
{
    Pending,
    Confirmed,
    Completed,
    Cancelled
}

public enum PaymentProvider
{
    Stripe,
    Razorpay,

    /// <summary>An offline payment (cash, bank transfer, cheque) recorded directly by an agent/admin — no external gateway involved.</summary>
    Manual
}

public enum PaymentStatus
{
    Created,
    Pending,
    Succeeded,
    Failed,
    Refunded
}
