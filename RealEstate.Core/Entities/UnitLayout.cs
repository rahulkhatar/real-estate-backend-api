using RealEstate.Core.Common;
using RealEstate.Core.ValueObjects;

namespace RealEstate.Core.Entities;

public class UnitLayout : BaseEntity
{
    public string UnitId { get; set; } = string.Empty;
    public string PropertyId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public UnitSnapshot UnitSnapshot { get; set; } = new();

    public string LayoutType { get; set; } = string.Empty;
    public int Rooms { get; set; }
    public int Bathrooms { get; set; }
    public int Balconies { get; set; }

    public string LayoutImageUrl { get; set; } = string.Empty;
    public string FloorPlanUrl { get; set; } = string.Empty;

    public List<string> Features { get; set; } = [];
    public string Description { get; set; } = string.Empty;

    public Dictionary<string, string> Dimensions { get; set; } = [];
}
