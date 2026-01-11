using Microsoft.EntityFrameworkCore;
using SyncTwoDatabase.Entities;

namespace SyncTwoDatabase.Data;

public class ReadDbContext(DbContextOptions<ReadDbContext> options) : DbContext(options)
{
	public DbSet<Product> Products { get; set; } = null!;
	public DbSet<SyncState> SyncStates { get; set; } = null!;
	// altre entità del read model
}