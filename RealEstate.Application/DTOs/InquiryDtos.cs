namespace RealEstate.Application.DTOs;

public class InquiryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public string? ListingType { get; set; }
    public string? ListingId { get; set; }
    public string ListingLabel { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateInquiryDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>"Project" | "Property" | "Unit" -- set together with ListingId when submitted via
    /// an "Enquire" button on a listing detail page. Both left null for a general Contact Us submission.</summary>
    public string? ListingType { get; set; }
    public string? ListingId { get; set; }
}

public class UpdateInquiryStatusDto
{
    public string Status { get; set; } = string.Empty;
}

public class InquiryQueryParams
{
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
