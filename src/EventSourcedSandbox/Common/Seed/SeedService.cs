using EventSourcedSandbox.Domain.Maps;
using EventSourcedSandbox.Domain.Players;
using EventSourcedSandbox.Domain.Records;
using Marten;

namespace EventSourcedSandbox.Common.Seed;

public class SeedService
{
    public async Task SeedSampleDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

        await store.Advanced.ResetAllData();

        await using var session = store.LightweightSession();

        var rootTimeStamp = DateTimeOffset.Now;

        // Add some maps

        var map1 = Guid.NewGuid();
        var map2 = Guid.NewGuid();

        var mapRegistered1 = new MapAddedEvent(map1, "jump_beef");
        var mapRegistered2 = new MapAddedEvent(map2, "jump_ice");

        session.Events.StartStream(map1, mapRegistered1);
        session.Events.StartStream(map2, mapRegistered2);

        // Save the pending changes to db
        await session.SaveChangesAsync();

        // Add some players

        var player1 = Guid.NewGuid();
        var player2 = Guid.NewGuid();
        var player3 = Guid.NewGuid();
        var player4 = Guid.NewGuid();
        var player5 = Guid.NewGuid();

        var playerRegistered1 = new PlayerRegisteredEvent(player1, "Alice");
        var playerRegistered2 = new PlayerRegisteredEvent(player2, "Bob");
        var playerRegistered3 = new PlayerRegisteredEvent(player3, "Charlie");
        var playerRegistered4 = new PlayerRegisteredEvent(player4, "David");
        var playerRegistered5 = new PlayerRegisteredEvent(player5, "Eve");

        session.Events.StartStream(player1, playerRegistered1);
        session.Events.StartStream(player2, playerRegistered2);
        session.Events.StartStream(player3, playerRegistered3);
        session.Events.StartStream(player4, playerRegistered4);
        session.Events.StartStream(player5, playerRegistered5);

        // Save the pending changes to db
        await session.SaveChangesAsync();

        // Register some records

        var record1 = new PlayerAchievedMapRecordEvent(map1, player1, TimeSpan.FromSeconds(1000));
        var record2 = new PlayerAchievedMapRecordEvent(map1, player2, TimeSpan.FromSeconds(900));
        var record3 = new PlayerAchievedMapRecordEvent(map1, player3, TimeSpan.FromSeconds(800));
        var record4 = new PlayerAchievedMapRecordEvent(map1, player4, TimeSpan.FromSeconds(700));
        var record5 = new PlayerAchievedMapRecordEvent(map1, player5, TimeSpan.FromSeconds(600));
        var record6 = new PlayerAchievedMapRecordEvent(map2, player1, TimeSpan.FromSeconds(500));
        var record7 = new PlayerAchievedMapRecordEvent(map2, player2, TimeSpan.FromSeconds(400));
        var record8 = new PlayerAchievedMapRecordEvent(map2, player3, TimeSpan.FromSeconds(300));
        var record9 = new PlayerAchievedMapRecordEvent(map2, player4, TimeSpan.FromSeconds(200));
        var record10 = new PlayerAchievedMapRecordEvent(map2, player5, TimeSpan.FromSeconds(100));

        session.Events.Append(map1, record1);
        session.Events.Append(map1, record2);
        session.Events.Append(map1, record3);
        session.Events.Append(map1, record4);
        session.Events.Append(map1, record5);
        session.Events.Append(map2, record6);
        session.Events.Append(map2, record7);
        session.Events.Append(map2, record8);
        session.Events.Append(map2, record9);
        session.Events.Append(map2, record10);

        // Save the pending changes to db
        await session.SaveChangesAsync();
    }
}
