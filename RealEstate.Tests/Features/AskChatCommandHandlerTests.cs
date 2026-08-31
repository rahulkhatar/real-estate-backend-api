using FluentAssertions;
using Moq;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Chat.Commands;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Entities;
using RealEstate.Core.Interfaces;
using Xunit;

namespace RealEstate.Tests.Features;

public class AskChatCommandHandlerTests
{
    private readonly Mock<IEmbeddingService> _embeddingService = new();
    private readonly Mock<IChatCompletionService> _chatService = new();
    private readonly Mock<IListingEmbeddingRepository> _embeddingRepo = new();

    private AskChatCommandHandler CreateHandler() =>
        new(_embeddingService.Object, _chatService.Object, _embeddingRepo.Object);

    private static ListingEmbedding Listing(string unitId, float[] vector, string unitNumber = "A-1") => new()
    {
        UnitId = unitId,
        UnitNumber = unitNumber,
        SourceText = $"Unit {unitNumber} description",
        Vector = vector,
        Status = "Available",
    };

    [Fact]
    public async Task Handle_ReturnsClosestMatchesByCosineSimilarity()
    {
        var queryVector = new float[] { 1, 0 };
        var closeMatch = Listing("u1", [1, 0], "Close");
        var farMatch = Listing("u2", [0, 1], "Far");

        _embeddingService.Setup(s => s.EmbedAsync("2bhk near beach", It.IsAny<CancellationToken>())).ReturnsAsync(queryVector);
        _embeddingRepo.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([farMatch, closeMatch]);
        _chatService.Setup(s => s.CompleteAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatTurn>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Here's what I found.");

        var handler = CreateHandler();
        var result = await handler.Handle(new AskChatCommand(new AskChatDto { Message = "2bhk near beach" }), CancellationToken.None);

        result.Reply.Should().Be("Here's what I found.");
        result.Matches.Should().HaveCount(2);
        result.Matches[0].UnitNumber.Should().Be("Close"); // identical vector -> highest cosine similarity, ranked first
    }

    [Fact]
    public async Task Handle_PassesUserMessageAndTrimmedHistoryToChat()
    {
        _embeddingService.Setup(s => s.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new float[] { 1 });
        _embeddingRepo.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        IReadOnlyList<ChatTurn>? capturedConversation = null;
        _chatService.Setup(s => s.CompleteAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatTurn>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyList<ChatTurn>, CancellationToken>((_, conv, _) => capturedConversation = conv)
            .ReturnsAsync("ok");

        var dto = new AskChatDto
        {
            Message = "What about villas?",
            History = [new ChatMessageDto { Role = "user", Content = "Hi" }, new ChatMessageDto { Role = "assistant", Content = "Hello!" }],
        };

        var handler = CreateHandler();
        await handler.Handle(new AskChatCommand(dto), CancellationToken.None);

        capturedConversation.Should().HaveCount(3);
        capturedConversation![^1].Should().Be(new ChatTurn("user", "What about villas?"));
    }

    [Fact]
    public async Task Handle_NoIndexedListings_StillAsksChatWithEmptyContext()
    {
        _embeddingService.Setup(s => s.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new float[] { 1 });
        _embeddingRepo.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _chatService.Setup(s => s.CompleteAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatTurn>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("No listings match that yet.");

        var handler = CreateHandler();
        var result = await handler.Handle(new AskChatCommand(new AskChatDto { Message = "anything" }), CancellationToken.None);

        result.Matches.Should().BeEmpty();
        result.Reply.Should().Be("No listings match that yet.");
    }
}
