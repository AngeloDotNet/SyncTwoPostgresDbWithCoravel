namespace SyncTwoDatabase.Entities;

// Simple key-value for persisting last run timestamps per sync job
public class SyncState
{
    public string Key { get; set; } = "";
    public DateTime LastRunUtc { get; set; }
}