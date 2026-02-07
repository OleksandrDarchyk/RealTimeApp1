namespace dataccess.Entities;

// Represents a message sent to a room
public class Message
{
    // Primary key
    public Guid Id { get; set; }

    // Foreign key to Room
    public string RoomId { get; set; } = string.Empty;

    // Message content
    public string Content { get; set; } = string.Empty;

    // Optional sender name (no auth yet)
    public string? From { get; set; }

    // When the message was created
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation back to Room
    public Room? Room { get; set; }
}
