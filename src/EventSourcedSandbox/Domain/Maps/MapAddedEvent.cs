namespace EventSourcedSandbox.Domain.Maps;

public record MapAddedEvent(DateTimeOffset Timestamp, Guid MapId, string MapName);
