using tHerdBackend.Core.DTOs.SUP;

namespace tHerdBackend.Core.Interfaces.SUP
{
	public interface ISupplierRepository
	{
		Task<List<SupSupplier>> GetAllSuppliersAsync();
		Task<List<SupSupplier>> GetActiveSuppliersAsync();
	}
}
