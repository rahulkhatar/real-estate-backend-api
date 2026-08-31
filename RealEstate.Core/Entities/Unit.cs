using RealEstate.Core.Common;
using RealEstate.Core.Enums;
using RealEstate.Core.ValueObjects;

namespace RealEstate.Core.Entities;

public class Unit : BaseEntity
{
    public string PropertyId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public ProjectSnapshot ProjectSnapshot { get; set; } = new();
    public PropertySnapshot PropertySnapshot { get; set; } = new();

    public string UnitNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string Facing { get; set; } = string.Empty;

    public UnitType Type { get; set; } = UnitType.TwoBhk;
    public SizeInfo Size { get; set; } = new();

    public decimal Price { get; set; }
    public decimal PricePerSqft { get; set; }

    public UnitStatus Status { get; set; } = UnitStatus.Available;

    public int Rooms { get; set; }
    public int Bathrooms { get; set; }
    public int Balconies { get; set; }

    public List<string> Amenities { get; set; } = [];
    public List<string> Features { get; set; } = [];
    public List<ImageAsset> Images { get; set; } = [];
}
