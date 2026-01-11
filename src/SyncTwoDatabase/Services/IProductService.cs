using OperationResults;
using SyncTwoDatabase.Entities;

namespace SyncTwoDatabase.Services;

public interface IProductService
{
	Task<Result<Product>> GetProductByIdAsync(Guid id);
	Task<Result<Product>> SaveProductAsync(Product product);
}