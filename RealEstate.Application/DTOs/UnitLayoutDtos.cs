namespace RealEstate.Application.DTOs;

public class UnitSnapshotDto
{
    public string UnitNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

public class UnitLayoutDto
{
    public string Id { get; set; } = string.Empty;
    public string UnitId { get; set; } = string.Empty;
    public string PropertyId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public UnitSnapshotDto UnitSnapshot { get; set; } = new();
    public string LayoutType { get; set; } = string.Empty;
    public int Rooms { get; set; }
    public int Bathrooms { get; set; }
    public int Balconies { get; set; }
    public string LayoutImageUrl { get; set; } = string.Empty;
    public string FloorPlanUrl { get; set; } = string.Empty;
    public List<string> Features { get; set; } = [];
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Dimensions { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateUnitLayoutDto
{
    public string UnitId { get; set; } = string.Empty;
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

public class UpdateUnitLayoutDto
{
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
