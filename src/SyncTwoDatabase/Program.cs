using Coravel;
using Coravel.Scheduling.Schedule.Interfaces;
using Microsoft.EntityFrameworkCore;
using OperationResults.AspNetCore.Http;
using SyncTwoDatabase.Data;
using SyncTwoDatabase.Entities;
using SyncTwoDatabase.Jobs;
using SyncTwoDatabase.Services;
using System.Net.Mime;
using TinyHelpers.AspNetCore.Extensions;
using TinyHelpers.AspNetCore.OpenApi;

namespace SyncTwoDatabase;

public class Program
{
	public static async Task Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Services.AddHttpContextAccessor();

		var writeDatabase = builder.Configuration.GetConnectionString("WriteDatabase");
		var readDatabase = builder.Configuration.GetConnectionString("ReadDatabase");

		builder.Services.AddDbContext<WriteDbContext>(options => options.UseNpgsql(writeDatabase));
		builder.Services.AddDbContext<ReadDbContext>(options => options.UseNpgsql(readDatabase));

		// Coravel + Job Scheduler
		builder.Services.AddScheduler();
		builder.Services.AddTransient<DbPostgresSyncJob>();

		//Job Scheduler - Using assembly scanning with Scrutor to register all jobs
		//builder.Services.Scan(scan => scan.FromAssemblyOf<DbPostgresSyncJob>()
		//	.AddClasses(classes => classes.Where(type => type.Name.EndsWith("Job")))
		//	.AsSelfWithInterfaces()
		//	.WithTransientLifetime());

		builder.Services.AddTransient<IProductService, ProductService>();
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
		await ConfigureDatabaseAsync(app.Services);

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
		var serviceProvider = app.Services;

		app.Services.UseScheduler(scheduler =>
		{
			scheduler
				.Schedule<DbPostgresSyncJob>()
				.Cron("*/2 * * * *") // Every 2 minutes
				.PreventOverlapping(nameof(DbPostgresSyncJob))
				.RunOnceAtStart();
		})
		.LogScheduledTaskProgress();

		#region "API Endpoints for Write Database"

		var writeApi = app.MapGroup("/api/writeDatabase").WithTags("WriteDatabase");

		writeApi.MapPost(string.Empty, async (Product product, IProductService productService, HttpContext httpContext) =>
		{
			var result = await productService.SaveProductAsync(product);
			var response = httpContext.CreateResponse(result, "GetProductById", new { id = result.Content?.Id });

			return response;
		})
		.WithName("CreateProduct")
		.WithDescription("Creates a new product in the write database.")
		.WithSummary("Creates a new product in the write database.")
		.Produces<Product>(StatusCodes.Status201Created)
		.ProducesProblem(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json);

		writeApi.MapGet("/{id:guid}", async (Guid id, IProductService productService, HttpContext httpContext) =>
		{
			var result = await productService.GetProductByIdAsync(id);
			var response = httpContext.CreateResponse(result);

			return response;
		})
		.WithName("GetProductById")
		.WithDescription("Retrieves a product by its ID from the write database.")
		.WithSummary("Retrieves a product by its ID from the write database.")
		.Produces<Product>()
		.ProducesProblem(StatusCodes.Status400BadRequest)
		.ProducesProblem(StatusCodes.Status404NotFound);

		#endregion

		app.Run();

		static async Task ConfigureDatabaseAsync(IServiceProvider serviceProvider)
		{
			await using var scope = serviceProvider.CreateAsyncScope();

			var dbReadContext = scope.ServiceProvider.GetRequiredService<ReadDbContext>();
			await dbReadContext.Database.MigrateAsync();

			var dbWriteContext = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
			await dbWriteContext.Database.MigrateAsync();
		}
	}
}