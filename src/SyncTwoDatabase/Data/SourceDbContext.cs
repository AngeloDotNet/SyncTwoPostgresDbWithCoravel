using Microsoft.EntityFrameworkCore;
using SyncTwoDatabase.Entities;

namespace SyncTwoDatabase.Data;

public class SourceDbContext(DbContextOptions<SourceDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products { get; set; } = null!;
}