using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Exceptions;

namespace RealEstate.Infrastructure.ExternalServices.OpenAi;

/// <summary>Calls OpenAI's REST API directly (no SDK dependency) — same rationale as the Razorpay gateway.</summary>
public class OpenAiEmbeddingService(HttpClient http, IOptions<OpenAiSettings> options) : IEmbeddingService
{
    private readonly OpenAiSettings _settings = options.Value;

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var results = await EmbedManyAsync([text], ct);
        return results[0];
    }

    public async Task<List<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new AiNotConfiguredException();

        using var request = new HttpRequestMessage(HttpMethod.Post, "embeddings")
        {
            Content = JsonContent.Create(new { model = _settings.EmbeddingModel, input = texts }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        var response = await http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new ConflictException($"OpenAI embeddings request failed: {body}");

        using var doc = JsonDocument.Parse(body);

        // Sort by the API's own "index" field rather than assuming array order matches input order.
        return doc.RootElement.GetProperty("data").EnumerateArray()
            .Select(item => (
                Index: item.GetProperty("index").GetInt32(),
                Vector: item.GetProperty("embedding").EnumerateArray().Select(e => e.GetSingle()).ToArray()))
            .OrderBy(x => x.Index)
            .Select(x => x.Vector)
            .ToList();
    }
}
