using RealEstate.Core.Common;

namespace RealEstate.Core.Entities;

/// <summary>
/// A unit's vector embedding for the RAG chat assistant, plus a denormalized snapshot of the
/// fields the chat needs to render a result card — avoids joining back to Unit/Property/Project
/// on every chat request. Rebuilt via ReindexListingsCommand; not authoritative for listing data.
/// </summary>
public class ListingEmbedding : BaseEntity
{
    public string UnitId { get; set; } = string.Empty;
    public string PropertyId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>The text that was embedded — also used as LLM context for retrieved matches.</summary>
    public string SourceText { get; set; } = string.Empty;
    public float[] Vector { get; set; } = [];

    public string UnitNumber { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}
