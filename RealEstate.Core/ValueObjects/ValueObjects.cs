using RealEstate.Core.Enums;

namespace RealEstate.Core.ValueObjects;

public class Coordinates
{
    public double Lat { get; set; }
    public double Lng { get; set; }
}

public class Location
{
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public Coordinates? Coordinates { get; set; }
}

public class ImageAsset
{
    public string Url { get; set; } = string.Empty;
    public string Alt { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public class SizeInfo
{
    public decimal Value { get; set; }
    public SizeUnit Unit { get; set; } = SizeUnit.Sqft;
}

/// <summary>
/// Point-in-time snapshot of a parent entity's display fields, embedded on the child
/// for read performance. Not authoritative — refetch the parent for current data.
/// </summary>
public class ProjectSnapshot
{
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

public class PropertySnapshot
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class UnitSnapshot
{
    public string UnitNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

public class AgentSnapshot
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
}
