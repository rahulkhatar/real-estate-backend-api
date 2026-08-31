namespace RealEstate.Application.DTOs;

public class ChatMessageDto
{
    /// <summary>"user" or "assistant".</summary>
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class AskChatDto
{
    public string Message { get; set; } = string.Empty;

    /// <summary>Prior turns of this conversation, oldest first (client keeps the transcript — no server-side session).</summary>
    public List<ChatMessageDto> History { get; set; } = [];
}

public class ChatResponseDto
{
    public string Reply { get; set; } = string.Empty;
    public List<ListingMatchDto> Matches { get; set; } = [];
}

public class ListingMatchDto
{
    public string UnitId { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class ReindexResultDto
{
    public int IndexedCount { get; set; }
}
