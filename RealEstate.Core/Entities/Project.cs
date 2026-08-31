using RealEstate.Core.Common;
using RealEstate.Core.Enums;
using RealEstate.Core.ValueObjects;

namespace RealEstate.Core.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PropertyType Type { get; set; } = PropertyType.Residential;
    public Location Location { get; set; } = new();
    public List<ImageAsset> Images { get; set; } = [];
    public ProjectStatus Status { get; set; } = ProjectStatus.Upcoming;
    public int TotalProperties { get; set; }
    public int SoldProperties { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime CompletionDate { get; set; }
    public List<string> Amenities { get; set; } = [];
}
