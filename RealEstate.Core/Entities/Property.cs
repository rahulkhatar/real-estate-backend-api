using RealEstate.Core.Common;
using RealEstate.Core.Enums;
using RealEstate.Core.ValueObjects;

namespace RealEstate.Core.Entities;

public class Property : BaseEntity
{
    public string ProjectId { get; set; } = string.Empty;
    public ProjectSnapshot ProjectSnapshot { get; set; } = new();

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PropertyType Type { get; set; } = PropertyType.Residential;
    public List<ImageAsset> Images { get; set; } = [];
    public PropertyStatus Status { get; set; } = PropertyStatus.Available;

    public int TotalUnits { get; set; }
    public int SoldUnits { get; set; }

    public decimal BasePrice { get; set; }
    public decimal TotalPrice { get; set; }

    public int Floors { get; set; }
    public List<string> Amenities { get; set; } = [];
    public List<string> Features { get; set; } = [];
}
