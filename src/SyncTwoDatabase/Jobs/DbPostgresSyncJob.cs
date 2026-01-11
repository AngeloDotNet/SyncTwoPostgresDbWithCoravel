using Coravel.Invocable;
using Microsoft.EntityFrameworkCore;
using SyncTwoDatabase.Data;
using SyncTwoDatabase.Entities;
using SyncTwoDatabase.Enums;

namespace SyncTwoDatabase.Jobs;

public class DbPostgresSyncJob(WriteDbContext writeDb, ReadDbContext readDb, ILogger<DbPostgresSyncJob> logger) : IInvocable
{
    public async Task Invoke()
    {
        logger.LogInformation("SyncJob started at {time}", DateTimeOffset.UtcNow);

        var syncStateName = nameof(SyncStateEnum.Products);

        // Recupera lo stato dell'ultimo sync (memorizzato nel read DB)
        //var state = await readDb.SyncStates.FirstOrDefaultAsync(s => s.Name == "Default");
        var state = await readDb.SyncStates.FirstOrDefaultAsync(s => s.Name == syncStateName);
        var lastSynced = state?.LastSyncedUtc ?? DateTime.MinValue;

        try
        {
            // Prendi i record modificati dopo lastSynced dal write DB (esempio: Products)
            var changed = await writeDb.Products
                .AsNoTracking()
                .Where(p => p.UpdatedAtUtc > lastSynced)
                .OrderBy(p => p.UpdatedAtUtc)
                .ToListAsync();

            if (changed.Count == 0)
            {
                logger.LogInformation("No changes to sync.");
            }
            else
            {
                logger.LogInformation("Found {count} changed entities to sync.", changed.Count);

                using var tx = await readDb.Database.BeginTransactionAsync();

                foreach (var entity in changed)
                {
                    // Upsert into read DB (idempotent)
                    var existing = await readDb.Products.FirstOrDefaultAsync(p => p.Id == entity.Id);

                    if (existing == null)
                    {
                        var copy = new Product
                        {
                            Id = entity.Id,
                            Name = entity.Name,
                            Price = entity.Price,
                            UpdatedAtUtc = entity.UpdatedAtUtc
                        };
                        readDb.Products.Add(copy);
                    }
                    else
                    {
                        existing.Name = entity.Name;
                        existing.Price = entity.Price;
                        existing.UpdatedAtUtc = entity.UpdatedAtUtc;
                        readDb.Products.Update(existing);
                    }
                }

                // Aggiorna lo stato "last synced" con il valore massimo dell'UpdatedAtUtc sincronizzato
                var maxUpdated = changed.Max(c => c.UpdatedAtUtc);

                if (state == null)
                {
                    //state = new SyncState { Name = "Default", LastSyncedUtc = maxUpdated };
                    state = new SyncState { Name = syncStateName, LastSyncedUtc = maxUpdated };
                    readDb.SyncStates.Add(state);
                }
                else
                {
                    state.LastSyncedUtc = maxUpdated;
                    readDb.SyncStates.Update(state);
                }

                await readDb.SaveChangesAsync();
                await tx.CommitAsync();

                logger.LogInformation("Synced up to {time}", maxUpdated);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while running SyncJob");
            // Considera retry/backoff o inviare alert
            throw;
        }
    }
}