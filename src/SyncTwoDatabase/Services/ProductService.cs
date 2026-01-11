using Microsoft.EntityFrameworkCore;
using OperationResults;
using SyncTwoDatabase.Data;
using SyncTwoDatabase.Entities;

namespace SyncTwoDatabase.Services;

public class ProductService(WriteDbContext dbContext) : IProductService
{
	public async Task<Result<Product>> GetProductByIdAsync(Guid id)
	{
		var productResult = await dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

		if (productResult is null)
		{
			return Result.Fail(FailureReasons.ItemNotFound);
		}

		return productResult;
	}

	public async Task<Result<Product>> SaveProductAsync(Product product)
	{
		var dbProduct = new Product
		{
			Id = Guid.NewGuid(),
			Name = product.Name,
			Price = product.Price,
			UpdatedAtUtc = DateTime.UtcNow
		};

		dbContext.Products.Add(dbProduct);
		await dbContext.SaveChangesAsync();

		return dbProduct;
	}
}