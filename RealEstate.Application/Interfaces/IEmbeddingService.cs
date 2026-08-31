namespace RealEstate.Application.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);

    /// <summary>Batched embedding — cheaper than N calls to EmbedAsync when indexing many listings at once.</summary>
    Task<List<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken ct = default);
}
