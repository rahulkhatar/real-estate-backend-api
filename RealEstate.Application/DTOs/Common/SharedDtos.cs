using System.ComponentModel.DataAnnotations;

namespace RealEstate.Application.DTOs.Common;

public class CoordinatesDto
{
    public double Lat { get; set; }
    public double Lng { get; set; }
}

public class LocationDto
{
    [Required, MaxLength(200)]
    public string Address { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string State { get; set; } = string.Empty;

    [MaxLength(20)]
    public string ZipCode { get; set; } = string.Empty;

    public CoordinatesDto? Coordinates { get; set; }
}

public class ImageAssetDto
{
    [Required]
    public string Url { get; set; } = string.Empty;

    public string Alt { get; set; } = string.Empty;
}

public class SizeInfoDto
{
    public decimal Value { get; set; }
    public string Unit { get; set; } = "Sqft";
}
