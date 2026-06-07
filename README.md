# SyncDemo (.NET 8) — sincronizzazione automatica Postgres con Coravel

Questo progetto demo mostra come sincronizzare automaticamente due database PostgreSQL (source -> target) usando:

- .NET 8 Minimal API
- Entity Framework Core con Npgsql
- Coravel per scheduling dei job

Prerequisiti

- .NET 8 SDK
- Docker & Docker Compose

Come eseguire (modo rapido)

1. Avvia i DB:
   docker-compose up -d

2. Esegui l'app (dal folder del progetto):
   dotnet restore
   dotnet run

3. L'app esporrà:
   - GET / -> stato
   - POST /sync -> trigger manuale sincronizzazione

Il processo di scheduling di Coravel eseguirà la sincronizzazione automaticamente ogni 1 minuto (configurazione in `Program.cs`).

Cosa fa la sincronizzazione

- Cerca in `source_db` i Product con `UpdatedAt > lastRun` (lastRun salvato in target.SyncStates).
- Per ogni record trovato, esegue upsert nel `target_db`.
- Aggiorna `SyncStates` con l'ora dell'ultima sincronizzazione.

Note e miglioramenti possibili

- Per produzione: usare migration EF Core e non `EnsureCreated`.
- Aggiungere logging dettagliato e metriche.
- Gestire cancellazioni (soft-delete/flag) o conflitti in caso di sincronizzazione bidirezionale.
- Proteggere endpoint e gestire resilienza / retry in caso di errori di rete.