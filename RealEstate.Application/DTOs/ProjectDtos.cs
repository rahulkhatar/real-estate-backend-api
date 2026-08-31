using RealEstate.Application.DTOs.Common;

namespace RealEstate.Application.DTOs;

public class ProjectDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public LocationDto Location { get; set; } = new();
    public List<ImageAssetDto> Images { get; set; } = [];
    public string Status { get; set; } = string.Empty;
    public int TotalProperties { get; set; }
    public int SoldProperties { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime CompletionDate { get; set; }
    public List<string> Amenities { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "Residential";
    public LocationDto Location { get; set; } = new();
    public List<ImageAssetDto> Images { get; set; } = [];
    public string Status { get; set; } = "Upcoming";
    public DateTime StartDate { get; set; }
    public DateTime CompletionDate { get; set; }
    public List<string> Amenities { get; set; } = [];
}

public class UpdateProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "Residential";
    public LocationDto Location { get; set; } = new();
    public List<ImageAssetDto> Images { get; set; } = [];
    public string Status { get; set; } = "Upcoming";
    public DateTime StartDate { get; set; }
    public DateTime CompletionDate { get; set; }
    public List<string> Amenities { get; set; } = [];
}

public class ProjectQueryParams
{
    public string? City { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
