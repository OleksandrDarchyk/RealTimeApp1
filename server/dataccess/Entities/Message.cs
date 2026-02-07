namespace dataccess.Entities;


// Represents a chat room (e.g. "room1")
public class Room
{
    // Primary key (room identifier used in URLs)
    public string Id { get; set; } = string.Empty;

    // When the room was created
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation: all messages in this room
    public List<Message> Messages { get; set; } = new();
}
