using System.Text.Json;
using RecipeTracker.Core;
using RecipeTracker.Core.Ports;

namespace RecipeTracker.Core.Tests.Fakes;

/// <summary>
/// Keeps records as JSON, like the real store, so a caller can't change a stored record
/// by holding on to the object it saved.
/// </summary>
public sealed class InMemoryRecordStore : IRecordStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Dictionary<string, Dictionary<string, string>> _tables = new();

    /// <summary>When set, a save that includes a record this matches fails, as a full device might, and saves nothing.</summary>
    public Func<StoredRecord, bool>? FailSavingWhen { get; set; }

    public Task SaveAsync<T>(string table, T record) where T : StoredRecord
    {
        if (FailSavingWhen?.Invoke(record) == true)
        {
            throw new IOException("The record couldn't be saved.");
        }

        Put(table, record, JsonSerializer.Serialize(record, JsonOptions));
        return Task.CompletedTask;
    }

    public Task SaveAllAsync(IReadOnlyList<RecordToSave> records)
    {
        if (records.Any(r => FailSavingWhen?.Invoke(r.Record) == true))
        {
            throw new IOException("The records couldn't be saved.");
        }

        foreach (var (table, record) in records)
        {
            Put(table, record, JsonSerializer.Serialize(record, record.GetType(), JsonOptions));
        }

        return Task.CompletedTask;
    }

    private void Put(string table, StoredRecord record, string json)
    {
        if (!_tables.TryGetValue(table, out var rows))
        {
            rows = new Dictionary<string, string>();
            _tables[table] = rows;
        }

        rows[record.Id] = json;
    }

    public Task<T?> LoadAsync<T>(string table, string id) where T : StoredRecord
    {
        if (_tables.TryGetValue(table, out var rows) && rows.TryGetValue(id, out var json))
        {
            return Task.FromResult(JsonSerializer.Deserialize<T>(json, JsonOptions));
        }

        return Task.FromResult<T?>(null);
    }

    public Task<IReadOnlyList<T>> ListAsync<T>(string table) where T : StoredRecord
    {
        if (!_tables.TryGetValue(table, out var rows))
        {
            return Task.FromResult<IReadOnlyList<T>>([]);
        }

        IReadOnlyList<T> records = rows.Values
            .Select(json => JsonSerializer.Deserialize<T>(json, JsonOptions)!)
            .ToList();
        return Task.FromResult(records);
    }
}
