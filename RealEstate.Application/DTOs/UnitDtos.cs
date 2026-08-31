using RealEstate.Application.DTOs.Common;

namespace RealEstate.Application.DTOs;

public class PropertySnapshotDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class UnitDto
{
    public string Id { get; set; } = string.Empty;
    public string PropertyId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public ProjectSnapshotDto ProjectSnapshot { get; set; } = new();
    public PropertySnapshotDto PropertySnapshot { get; set; } = new();
    public string UnitNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string Facing { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public SizeInfoDto Size { get; set; } = new();
    public decimal Price { get; set; }
    public decimal PricePerSqft { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Rooms { get; set; }
    public int Bathrooms { get; set; }
    public int Balconies { get; set; }
    public List<string> Amenities { get; set; } = [];
    public List<string> Features { get; set; } = [];
    public List<ImageAssetDto> Images { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateUnitDto
{
    public string PropertyId { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string Facing { get; set; } = string.Empty;
    public string Type { get; set; } = "TwoBhk";
    public SizeInfoDto Size { get; set; } = new();
    public decimal Price { get; set; }
    public decimal PricePerSqft { get; set; }
    public string Status { get; set; } = "Available";
    public int Rooms { get; set; }
    public int Bathrooms { get; set; }
    public int Balconies { get; set; }
    public List<string> Amenities { get; set; } = [];
    public List<string> Features { get; set; } = [];
    public List<ImageAssetDto> Images { get; set; } = [];
}

public class UpdateUnitDto
{
    public int Floor { get; set; }
    public string Facing { get; set; } = string.Empty;
    public string Type { get; set; } = "TwoBhk";
    public SizeInfoDto Size { get; set; } = new();
    public decimal Price { get; set; }
    public decimal PricePerSqft { get; set; }
    public string Status { get; set; } = "Available";
    public int Rooms { get; set; }
    public int Bathrooms { get; set; }
    public int Balconies { get; set; }
    public List<string> Amenities { get; set; } = [];
    public List<string> Features { get; set; } = [];
    public List<ImageAssetDto> Images { get; set; } = [];
}

public class UnitQueryParams
{
    public string? PropertyId { get; set; }
    public string? ProjectId { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
