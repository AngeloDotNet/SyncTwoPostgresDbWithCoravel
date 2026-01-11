namespace SyncTwoDatabase.Entities;

public class SyncState
{
	public int Id { get; set; }
	public string Name { get; set; } = null!; // es: "Default" o nome del job
	public DateTime LastSyncedUtc { get; set; }
}