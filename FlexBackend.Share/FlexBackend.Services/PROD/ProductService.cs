using FlexBackend.Core.DTOs.PROD;
using FlexBackend.Core.DTOs.SYS;
using FlexBackend.Core.Exceptions;
using FlexBackend.Core.Interfaces.PROD;
using FlexBackend.Core.Interfaces.Products;
using FlexBackend.Core.Interfaces.SYS;

namespace FlexBackend.Services.PROD
{
    public class ProductService : IProductService
    {
        private readonly IProdProductRepository _repo;
        private readonly IProdProductQueryRepository _qrepo;
        private readonly ISysCodeRepository _srepo;

        public ProductService(IProdProductRepository repo, IProdProductQueryRepository qrepo, ISysCodeRepository srepo)
        {
            _repo = repo;
            _qrepo = qrepo;
            _srepo = srepo;
        }

		public async Task<(bool IsValid, string ErrorMessage)> ValidateProductAsync(ProdProductDto dto)
		{
			string errorMessage = string.Empty;

			// === A. 主檔必填 ===
			if (dto.BrandId <= 0)
				return (false, "品牌必須選擇！");
			if (string.IsNullOrWhiteSpace(dto.ProductName))
				return (false, "商品名稱為必填！");

			// === B. 商品分類檢查 ===
			if (dto.Types == null || !dto.Types.Any())
				return (false, "至少必須選擇一個商品分類！");

			if (!dto.Types.Any(t => t.IsPrimary))
				return (false, "商品分類中必須設定一個主分類！ (可右鍵設定)");

			// === C. 商品名稱唯一性檢查 ===
			var existing = await _repo.GetByProductNameAsync(dto.ProductName.Trim(), dto.ProductId);
			if (existing)
			{
				return (false, $"商品名稱「{dto.ProductName}」已存在，不可重複！");
			}

			// === B. SKU 必填 ===
			foreach (var (sku, i) in dto.Skus.Select((x, i) => (x, i)))
			{
				if (string.IsNullOrWhiteSpace(sku.SpecCode))
					return (false, $"第 {i + 1} 筆 SKU 缺少規格碼！");
				if (sku.ListPrice == null || sku.ListPrice <= 0)
					return (false, $"第 {i + 1} 筆 SKU 缺少原價！");
				if (sku.UnitPrice == null || sku.UnitPrice <= 0)
					return (false, $"第 {i + 1} 筆 SKU 缺少單價！");
				if (sku.SalePrice == null)
					return (false, $"第 {i + 1} 筆 SKU 缺少優惠價！");
				if (sku.CostPrice == null || sku.CostPrice <= 0)
					return (false, $"第 {i + 1} 筆 SKU 缺少成本價！");
				if (sku.SafetyStockQty == null || sku.SafetyStockQty <= 0)
					return (false, $"第 {i + 1} 筆 SKU 缺少安全庫存量！");
				if (sku.ReorderPoint == null || sku.ReorderPoint <= 0)
					return (false, $"第 {i + 1} 筆 SKU 缺少再訂購點！");
				if (sku.MaxStockQty == null || sku.MaxStockQty <= 0)
					return (false, $"第 {i + 1} 筆 SKU 缺少最大庫存量！");
				if (sku.StartDate == DateTime.MinValue)
					return (false, $"第 {i + 1} 筆 SKU 缺少上架日期！");

				// 檢查Sku碼的規格
				foreach (var (specConfig, k) in dto.SpecConfigs.Select((a, k) => (a, k)))
				{
					var spec_value = specConfig.SpecOptions
						.FirstOrDefault(o => o.SkuId == sku.SkuId)?.OptionName;

					if (string.IsNullOrWhiteSpace(spec_value))
					{
						return (false, $"第 {i + 1} 筆 SKU 缺少規格值（規格 {k + 1}）！");
					}
				}
			}

			// === 1. 檢查 SKU 庫存層級 ===
			foreach (var (sku, i) in dto.Skus.Select((x, i) => (x, i)))
			{
				if (sku.MaxStockQty < sku.ReorderPoint || sku.ReorderPoint < sku.SafetyStockQty)
				{
					errorMessage = $"第 {i + 1} 筆 SKU 的庫存設定錯誤：最大庫存 > 再訂點 > 安全庫存！";
					return (false, errorMessage);
				}

				if (!(sku.ListPrice > sku.UnitPrice && sku.UnitPrice > sku.SalePrice && sku.SalePrice > sku.CostPrice))
				{
					errorMessage = $"第 {i + 1} 筆 SKU 的價格設定錯誤：必須符合 原價 > 單價 > 優惠價 > 成本價！";
					return (false, errorMessage);
				}
			}

   //         var barcodesInForm = dto.Skus.Where(s => !string.IsNullOrWhiteSpace(s.Barcode))
   //             .Select(s => s.Barcode.Trim())
   //             .ToList();

			//// === 2. 檢查表單內條碼是否重複 ===
			//var barcode_existing = await _repo.CheckUniqulByBarcodeAsync(barcodesInForm);
			//if (string.IsNullOrWhiteSpace(barcode_existing)==false)
			//{
			//	return (false, $"條碼「{barcode_existing}」已存在，不可重複！");
			//}

			// === 3. 檢查 DB 是否有重複條碼 ===
			var excludeSkuIds = dto.Skus.Select(s => s.SkuId).ToList();
			var barcodes = dto.Skus
				.Where(s => !string.IsNullOrWhiteSpace(s.Barcode))
				.Select(s => s.Barcode.Trim())
				.ToList();

			if (barcodes.Any())
			{
				var dupDb = await _repo.GetDuplicateBarcodesAsync(barcodes, excludeSkuIds);
				if (dupDb.Any())
				{
					errorMessage = $"條碼已存在於資料庫：{string.Join("、", dupDb)}，請輸入其他值！";
					return (false, errorMessage);
				}
			}

			return (true, errorMessage);
		}

		/// <summary>
		/// 商品完整分類清單
		/// </summary>
		/// <returns></returns>
		public async Task<List<ProdProductTypeConfigDto>> GetAllProductTypesAsync(CancellationToken ct = default)
		{
			try
			{
				return await _repo.GetAllProductTypesAsync(ct);
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleErrorMsg(ex);
				throw;
			}
		}

		/// <summary>
		/// 商品完整資料清單
		/// </summary>
		/// <returns></returns>
		public async Task<IEnumerable<ProdProductQueryDto>> GetAllProductQueryListAsync(int ProductId)
        {
            try
            {
                return await _qrepo.GetAllProductQueryListAsync(ProductId);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }

        /// <summary>
        /// 商品基本資料清單
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<ProdProductDto>> GetAllAsync()
        {
            try
            {
                return await _repo.GetAllAsync();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }

        public async Task<ProdProductDto?> GetByIdAsync(int Id)
        {
            try
            {
                if (Id <= 0)
                    throw new ArgumentException("ProductId must be greater than zero.");

                return await _repo.GetByIdAsync(Id);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }

        public async Task<int> CreateAsync(ProdProductDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.ProductName))
                    throw new ArgumentException("Product name is required.");

                return await _repo.AddAsync(dto);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }

        public async Task UpdateAsync(ProdProductDto dto)
        {
            try
            {
                if (dto.ProductId <= 0)
                    throw new ArgumentException("Invalid ProductId.");

                await _repo.UpdateAsync(dto);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }

        public async Task DeleteAsync(int productId)
        {
            try
            {
                if (productId <= 0)
                    throw new ArgumentException("Invalid ProductId.");

                await _repo.DeleteAsync(productId);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }

        public async Task<IEnumerable<LoadBrandOptionDto>> LoadBrandOptionsAsync()
        {
            return await _repo.LoadBrandOptionsAsync();
        }

        public async Task<IEnumerable<SysCodeDto>> GetSysCodes(string Mod, List<string> ids)
        {
            return await _srepo.GetSysCodes(Mod, ids);
        }
    }
}
