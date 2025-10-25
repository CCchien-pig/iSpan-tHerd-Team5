using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.Interfaces.SUP;

namespace tHerdBackend.Infra.Repository.SUP
{
	public class SupplierService : ISupplierService
	{
		private readonly ISupplierRepository _repository;

		public SupplierService(ISupplierRepository repository)
		{
			_repository = repository;
		}
		public async Task<List<SupSupplier>> GetAllSuppliersAsync()
		{
			return await _repository.GetAllSuppliersAsync();
		}

		public async Task<List<SupSupplier>> GetActiveSuppliersAsync()
		{
			return await _repository.GetActiveSuppliersAsync();
		}
	}
}
