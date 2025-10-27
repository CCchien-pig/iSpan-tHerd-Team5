using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.Models;
using SupSupplier = tHerdBackend.Core.DTOs.SUP.SupSupplier;

namespace tHerdBackend.Infra.Repository.SUP
{
	public class SupplierRepository : ISupplierRepository
	{
		private readonly tHerdDBContext _context;

		public SupplierRepository(tHerdDBContext context)
		{
			_context = context;
		}

		public async Task<List<SupSupplier>> GetAllSuppliersAsync()
		{
			return await _context.SupSuppliers
				.Select(s => new SupSupplier
				{
					SupplierId = s.SupplierId,
					SupplierName = s.SupplierName,
					IsActive = s.IsActive
				}).ToListAsync();
		}

		public async Task<List<SupSupplier>> GetActiveSuppliersAsync()
		{
			return await _context.SupSuppliers
				.Where(s => s.IsActive)
				.Select(s => new SupSupplier
				{
					SupplierId = s.SupplierId,
					SupplierName = s.SupplierName,
					IsActive = s.IsActive
				})
				.ToListAsync();
		}
	}
}
