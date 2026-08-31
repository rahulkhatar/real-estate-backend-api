using FluentValidation;
using MediatR;
using RealEstate.Application.DTOs;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Entities;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Chat.Commands;

public record AskChatCommand(AskChatDto Dto) : IRequest<ChatResponseDto>;

public class AskChatCommandValidator : AbstractValidator<AskChatCommand>
{
    public AskChatCommandValidator()
    {
        RuleFor(x => x.Dto.Message).NotEmpty().MaximumLength(1000);
    }
}

/// <summary>
/// Retrieval-augmented chat: embeds the user's message, finds the closest listings by cosine
/// similarity over every indexed unit (brute-force — cheap at this catalog size and avoids
/// depending on Atlas Search/vector-index availability, which varies by cluster tier), then asks
/// the chat model to answer grounded in only those listings.
/// </summary>
public class AskChatCommandHandler(
    IEmbeddingService embeddingService,
    IChatCompletionService chatCompletionService,
    IListingEmbeddingRepository embeddingRepository) : IRequestHandler<AskChatCommand, ChatResponseDto>
{
    private const int TopK = 5;
    private const int MaxHistoryTurns = 10;

    public async Task<ChatResponseDto> Handle(AskChatCommand request, CancellationToken cancellationToken)
    {
        var queryVector = await embeddingService.EmbedAsync(request.Dto.Message, cancellationToken);

        var allEmbeddings = await embeddingRepository.ListAllAsync(cancellationToken);
        var topMatches = allEmbeddings
            .Select(e => (Embedding: e, Score: CosineSimilarity(queryVector, e.Vector)))
            .OrderByDescending(x => x.Score)
            .Take(TopK)
            .Select(x => x.Embedding)
            .ToList();

        var systemPrompt = BuildSystemPrompt(topMatches);

        var conversation = request.Dto.History
            .TakeLast(MaxHistoryTurns)
            .Select(h => new ChatTurn(h.Role, h.Content))
            .Append(new ChatTurn("user", request.Dto.Message))
            .ToList();

        var reply = await chatCompletionService.CompleteAsync(systemPrompt, conversation, cancellationToken);

        return new ChatResponseDto
        {
            Reply = reply,
            Matches = topMatches.Select(m => new ListingMatchDto
            {
                UnitId = m.UnitId,
                UnitNumber = m.UnitNumber,
                PropertyName = m.PropertyName,
                ProjectName = m.ProjectName,
                City = m.City,
                Type = m.Type,
                Price = m.Price,
                Status = m.Status,
                ImageUrl = m.ImageUrl,
            }).ToList(),
        };
    }

    private static string BuildSystemPrompt(List<ListingEmbedding> matches)
    {
        var context = matches.Count > 0
            ? string.Join("\n---\n", matches.Select(m => m.SourceText))
            : "No listings matched this query.";

        return "You are a helpful real estate assistant for an Indian property platform. Answer the user's " +
               "question using ONLY the listing information below — never invent a price, unit number, or " +
               "amenity that isn't stated. If nothing here fits what they're asking for, say so honestly and " +
               "suggest they browse all listings instead. Prefer Available units over Booked/Sold ones unless " +
               "asked specifically about the latter.\n\n" +
               "The matching listings are already shown to the user as visual cards below your reply, so do NOT " +
               "restate their full details (no per-unit price/room/amenity dumps, no numbered or bulleted listing " +
               "recaps). Instead write a short, plain-language, conversational reply — a sentence or two framing " +
               "what you found or answering their question directly — and only call out a specific unit number " +
               "or property/project name when it's needed to answer them, not as a recap.\n\n" +
               "Write in plain prose only: no markdown syntax at all (no **bold**, no #headings, no bullet or " +
               "numbered lists, no backticks).\n\nMatching listings:\n" + context;
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0) return 0;

        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return normA == 0 || normB == 0 ? 0 : dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
