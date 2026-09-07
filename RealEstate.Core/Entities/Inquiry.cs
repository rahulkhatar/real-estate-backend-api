using RealEstate.Core.Common;
using RealEstate.Core.Enums;

namespace RealEstate.Core.Entities;

public class Inquiry : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    // Set when submitted via an "Enquire" button on a Project/Property/Unit detail page rather
    // than the general Contact Us form. ListingLabel is resolved server-side at creation time
    // (never trusted from the client) so it stays a readable snapshot even if the listing is
    // later edited or removed.
    public InquiryListingType? ListingType { get; set; }
    public string? ListingId { get; set; }
    public string ListingLabel { get; set; } = string.Empty;

    public InquiryStatus Status { get; set; } = InquiryStatus.New;
}
