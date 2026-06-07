using Microsoft.EntityFrameworkCore;
using SyncTwoDatabase.Data;
using SyncTwoDatabase.Entities;

namespace SyncTwoDatabase.Services;

public class SyncService(IDbContextFactory<SourceDbContext> sourceFactory, IDbContextFactory<TargetDbContext> targetFactory)
{
    private const string SyncKeyProducts = "ProductsSync";

    public async Task RunAsync()
    {
        using var source = sourceFactory.CreateDbContext();
        using var target = targetFactory.CreateDbContext();

        // Read last run (if any)
        var syncState = await target.SyncStates.FindAsync(SyncKeyProducts);
        var lastRun = syncState?.LastRunUtc ?? DateTime.MinValue;

        // Get changed products from source
        var changed = await source.Products
            .Where(p => p.UpdatedAt > lastRun)
            .AsNoTracking()
            .ToListAsync();

        if (changed.Count == 0)
        {
            // Nothing to do
            UpdateLastRun(target, syncState);
            return;
        }

        foreach (var src in changed)
        {
            var tgt = await target.Products.FindAsync(src.Id);

            if (tgt == null)
            {
                // Insert
                target.Products.Add(new Product
                {
                    Id = src.Id,
                    Name = src.Name,
                    Price = src.Price,
                    UpdatedAt = src.UpdatedAt
                });
            }
            else
            {
                // Update
                tgt.Name = src.Name;
                tgt.Price = src.Price;
                tgt.UpdatedAt = src.UpdatedAt;
                target.Products.Update(tgt);
            }
        }

        await target.SaveChangesAsync();

        // Update last run timestamp
        UpdateLastRun(target, syncState);
        await target.SaveChangesAsync();
    }

    private static void UpdateLastRun(TargetDbContext target, SyncState? syncState)
    {
        var now = DateTime.UtcNow;

        if (syncState == null)
        {
            target.SyncStates.Add(new SyncState { Key = SyncKeyProducts, LastRunUtc = now });
        }
        else
        {
            syncState.LastRunUtc = now;
            target.SyncStates.Update(syncState);
        }
    }
}