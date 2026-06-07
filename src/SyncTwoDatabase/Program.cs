using Coravel;
using Coravel.Scheduling.Schedule.Interfaces;
using Microsoft.EntityFrameworkCore;
using OperationResults.AspNetCore.Http;
using SyncTwoDatabase.Data;
using SyncTwoDatabase.Entities;
using SyncTwoDatabase.Services;
using TinyHelpers.AspNetCore.Extensions;
using TinyHelpers.AspNetCore.OpenApi;

namespace SyncTwoDatabase;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHttpContextAccessor();
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        builder.Services.AddDbContextFactory<SourceDbContext>(options
            => options.UseNpgsql(builder.Configuration.GetConnectionString("Source")));

        builder.Services.AddDbContextFactory<TargetDbContext>(options
            => options.UseNpgsql(builder.Configuration.GetConnectionString("Target")));

        builder.Services.AddTransient<SyncService>();
        builder.Services.AddScheduler();

        builder.Services.AddOperationResult(options =>
        {
            options.ErrorResponseFormat = ErrorResponseFormat.List;
        });

        builder.Services.AddDefaultProblemDetails();
        builder.Services.AddDefaultExceptionHandler();

        builder.Services.AddOpenApi(options =>
        {
            options.RemoveServerList();

            options.AddAcceptLanguageHeader();
            options.AddDefaultProblemDetailsResponse();
        });

        var app = builder.Build();
        await ConfigureDatabaseAsync(app.Services, app);

        //app.UseHttpsRedirection();

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", app.Environment.ApplicationName);
        });

        app.UseRouting();
        //app.UseCors();

        var scheduler = app.Services.GetRequiredService<IScheduler>();
        scheduler.Schedule(async () =>
        {
            using var scope = app.Services.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<SyncService>();

            await svc.RunAsync();
        }).Cron("*/2 * * * *"); // Every 2 minutes

        #region "API Endpoints for Write Database"

        // Health Check Endpoint
        app.MapGet("/", () => Results.Ok(new { message = "SyncDemo running" }));

        // Manual Sync Endpoint
        app.MapPost("/sync", async (IServiceProvider sp) =>
        {
            using var scope = sp.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<SyncService>();

            await svc.RunAsync();
            return Results.Ok(new { syncedAt = DateTime.UtcNow });
        });

        #endregion

        app.Run();

        static async Task ConfigureDatabaseAsync(IServiceProvider serviceProvider, WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var sourceFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SourceDbContext>>();
            var targetFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TargetDbContext>>();

            using var sourceDb = sourceFactory.CreateDbContext();
            using var targetDb = targetFactory.CreateDbContext();

            sourceDb.Database.EnsureCreated();
            targetDb.Database.EnsureCreated();

            if (!sourceDb.Products.Any())
            {
                sourceDb.Products.AddRange(new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Apple",
                    Price = 0.9m,
                    UpdatedAt = DateTime.UtcNow.AddMinutes(-10)
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Banana",
                    Price = 0.5m,
                    UpdatedAt = DateTime.UtcNow.AddMinutes(-9)
                });

                sourceDb.SaveChanges();
            }
        }
    }
}