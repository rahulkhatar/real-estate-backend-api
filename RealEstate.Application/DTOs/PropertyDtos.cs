using RealEstate.Application.DTOs.Common;

namespace RealEstate.Application.DTOs;

public class ProjectSnapshotDto
{
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

public class PropertyDto
{
    public string Id { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public ProjectSnapshotDto ProjectSnapshot { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<ImageAssetDto> Images { get; set; } = [];
    public string Status { get; set; } = string.Empty;
    public int TotalUnits { get; set; }
    public int SoldUnits { get; set; }
    public decimal BasePrice { get; set; }
    public decimal TotalPrice { get; set; }
    public int Floors { get; set; }
    public List<string> Amenities { get; set; } = [];
    public List<string> Features { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreatePropertyDto
{
    public string ProjectId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "Residential";
    public List<ImageAssetDto> Images { get; set; } = [];
    public string Status { get; set; } = "Available";
    public decimal BasePrice { get; set; }
    public decimal TotalPrice { get; set; }
    public int Floors { get; set; }
    public List<string> Amenities { get; set; } = [];
    public List<string> Features { get; set; } = [];
}

public class UpdatePropertyDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "Residential";
    public List<ImageAssetDto> Images { get; set; } = [];
    public string Status { get; set; } = "Available";
    public decimal BasePrice { get; set; }
    public decimal TotalPrice { get; set; }
    public int Floors { get; set; }
    public List<string> Amenities { get; set; } = [];
    public List<string> Features { get; set; } = [];
}

public class PropertyQueryParams
{
    public string? ProjectId { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
