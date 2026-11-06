namespace RecipeTracker.Core.Ports;

/// <summary>One record in a <see cref="IRecordStore.SaveAllAsync"/>, with the table it goes in.</summary>
public sealed record RecordToSave(string Table, StoredRecord Record);
