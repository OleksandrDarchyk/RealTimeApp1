namespace api.dto;

public record RoomHistoryResponse(string RoomId, List<MessageDto> Messages)
{
    public string EventType { get; init; } = "RoomHistory";
}