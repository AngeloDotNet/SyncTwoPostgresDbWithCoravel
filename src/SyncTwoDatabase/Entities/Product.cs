namespace SyncTwoDatabase.Entities;

public class Product
{
	public Guid Id { get; set; }
	public string Name { get; set; } = null!;
	public decimal Price { get; set; }
	public DateTime UpdatedAtUtc { get; set; } // campo usato per l'incremental sync
}