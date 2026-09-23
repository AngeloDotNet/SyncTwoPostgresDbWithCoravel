using OperationResults;
using SyncTwoDatabase.Entities;

namespace SyncTwoDatabase.Services.Interfaces;

public interface IProductService
{
	Task<Result<Product>> GetProductByIdAsync(Guid id);
	Task<Result<Product>> SaveProductAsync(Product product);
}