namespace RecipeTracker.Core.Ports;

/// <summary>
/// Saves, loads and lists records on this device. Each record type lives in its own table.
/// </summary>
public interface IRecordStore
{
    /// <summary>Inserts the record, or replaces the one with the same id.</summary>
    Task SaveAsync<T>(string table, T record) where T : StoredRecord;

    /// <summary>Saves every record (inserting or replacing each) in one go: if any can't be saved, none are.</summary>
    Task SaveAllAsync(IReadOnlyList<RecordToSave> records);

    Task<T?> LoadAsync<T>(string table, string id) where T : StoredRecord;

    Task<IReadOnlyList<T>> ListAsync<T>(string table) where T : StoredRecord;
}
