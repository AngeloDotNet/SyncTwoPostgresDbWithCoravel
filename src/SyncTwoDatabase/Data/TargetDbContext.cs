using Microsoft.EntityFrameworkCore;
using SyncTwoDatabase.Entities;

namespace SyncTwoDatabase.Data;

public class TargetDbContext(DbContextOptions<TargetDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<SyncState> SyncStates { get; set; } = null!;
}