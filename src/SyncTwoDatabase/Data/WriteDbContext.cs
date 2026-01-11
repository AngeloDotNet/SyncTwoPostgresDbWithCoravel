using Microsoft.EntityFrameworkCore;
using SyncTwoDatabase.Entities;

namespace SyncTwoDatabase.Data;

public class WriteDbContext(DbContextOptions<WriteDbContext> options) : DbContext(options)
{
	public DbSet<Product> Products { get; set; } = null!;
	// altre entità del write model
}