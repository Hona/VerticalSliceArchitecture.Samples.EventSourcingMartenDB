namespace EventSourcedSandbox.Domain.Records;

public static class RecordService
{
    public static List<Record> TryInsertOrUpdateRecord(
        List<Record> records,
        PlayerAchievedMapRecordEvent @event
    )
    {
        // Convert the grouped records into a dictionary for easier manipulation
        var recordsByPlayerAndMap = records
            .GroupBy(record => new { record.PlayerId, record.MapId })
            .ToDictionary(g => g.Key, g => g.ToList());

        var key = new { @event.PlayerId, @event.MapId };

        if (recordsByPlayerAndMap.TryGetValue(key, out var group))
        {
            var existingRecord = group.FirstOrDefault(r =>
                r.PlayerId == @event.PlayerId && r.MapId == @event.MapId
            );

            if (existingRecord == null || @event.Duration < existingRecord.Duration)
            {
                // Replace with the new record if it's faster or if no existing record
                group.RemoveAll(r => r.PlayerId == @event.PlayerId && r.MapId == @event.MapId);
                group.Add(new Record(@event.PlayerId, @event.MapId, @event.Duration));
            }
        }
        else
        {
            // Add a new group with the new record
            recordsByPlayerAndMap[key] = new List<Record>
            {
                new Record(@event.PlayerId, @event.MapId, @event.Duration)
            };
        }

        // Flatten the dictionary back into a list and order by duration
        var updatedRecords = recordsByPlayerAndMap
            .SelectMany(pair => pair.Value)
            .OrderBy(x => x.Duration)
            .ToList();

        // Return the updated MapAggregate
        return updatedRecords;
    }
}
