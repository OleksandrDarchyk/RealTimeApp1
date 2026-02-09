namespace api.dto;

public record MessageDto(Guid Id, string Content, string? From, DateTimeOffset CreatedAt);