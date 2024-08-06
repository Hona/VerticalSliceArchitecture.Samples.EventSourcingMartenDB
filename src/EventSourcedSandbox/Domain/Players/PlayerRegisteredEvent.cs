namespace EventSourcedSandbox.Domain.Players;

public record PlayerRegisteredEvent(Guid PlayerId, string PlayerName);
