namespace RealEstate.Application.Interfaces;

public record ChatTurn(string Role, string Content);

public interface IChatCompletionService
{
    Task<string> CompleteAsync(string systemPrompt, IReadOnlyList<ChatTurn> conversation, CancellationToken ct = default);
}
