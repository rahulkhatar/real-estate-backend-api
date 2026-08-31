using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Exceptions;

namespace RealEstate.Infrastructure.ExternalServices.OpenAi;

public class OpenAiChatCompletionService(HttpClient http, IOptions<OpenAiSettings> options) : IChatCompletionService
{
    private readonly OpenAiSettings _settings = options.Value;

    public async Task<string> CompleteAsync(string systemPrompt, IReadOnlyList<ChatTurn> conversation, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new AiNotConfiguredException();

        var messages = new List<object> { new { role = "system", content = systemPrompt } };
        messages.AddRange(conversation.Select(t => (object)new { role = t.Role, content = t.Content }));

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(new { model = _settings.ChatModel, messages, temperature = 0.4 }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        var response = await http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new ConflictException($"OpenAI chat request failed: {body}");

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
               ?? string.Empty;
    }
}
