using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Core.ValueObjects;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Services.SUP
{
	/// <summary>品牌收藏服務實作</summary>
	public class BrandFavoriteService : IBrandFavoriteService
	{
		private readonly IBrandFavoriteRepository _repo;
		private readonly tHerdDBContext _db;
		private readonly ILogger<BrandFavoriteService> _logger;
		public BrandFavoriteService(
			IBrandFavoriteRepository repo,
			tHerdDBContext db,
			ILogger<BrandFavoriteService> logger)
		{
			_repo = repo;
			_db = db;
			_logger = logger;
		}


		/// <summary>
		/// 新增品牌收藏
		/// 驗證：使用者存在且啟用、品牌存在且啟用、未重複收藏
		/// 成功訊息：收藏成功
		/// </summary>
		public async Task<ApiResponse<bool>> AddAsync(int userNumberId, int brandId, CancellationToken ct = default)
		{
			try
			{
				// 使用者存在與啟用
				var userExists = await _db.AspNetUsers.AsNoTracking()
					.AnyAsync(u => u.UserNumberId == userNumberId && u.IsActive, ct);
				if (!userExists)
					return ApiResponse<bool>.Fail("無此使用者或已停用");

				// 品牌存在與啟用
				var brandExists = await _db.SupBrands.AsNoTracking()
					.AnyAsync(b => b.BrandId == brandId && b.IsActive, ct);
				if (!brandExists)
					return ApiResponse<bool>.Fail("無此品牌或已停用");

				// 重複收藏
				var exists = await _repo.ExistsAsync(userNumberId, brandId, ct);
				if (exists) return ApiResponse<bool>.Fail("已收藏過該品牌");

				// 寫入
				await _repo.AddAsync(new BrandFavoriteRequestDto
				{
					UserNumberId = userNumberId,
					BrandId = brandId
				}, ct);

				_logger.LogInformation("BrandFavorite added. user={UserNumberId}, brand={BrandId}", userNumberId, brandId);
				return ApiResponse<bool>.Ok(true);
			}
			catch (DbUpdateException dbx) when (dbx.InnerException?.Message?.Contains("UQ_SupBrandFavorite_User_Brand") == true)
			{
				_logger.LogWarning(dbx, "BrandFavorite duplicate. user={UserNumberId}, brand={BrandId}", userNumberId, brandId);
				return ApiResponse<bool>.Fail("已收藏過該品牌");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "BrandFavorite add error. user={UserNumberId}, brand={BrandId}", userNumberId, brandId);
				return ApiResponse<bool>.Fail("新增收藏失敗");
			}
		}

		/// <summary>
		/// 取消品牌收藏
		/// 驗證：品牌存在且啟用
		/// 成功訊息：取消收藏成功
		/// </summary>
		public async Task<ApiResponse<bool>> RemoveAsync(int userNumberId, int brandId, CancellationToken ct = default)
		{
			try
			{
				var brandExists = await _db.SupBrands.AsNoTracking()
					.AnyAsync(b => b.BrandId == brandId && b.IsActive, ct);
				if (!brandExists)
					return ApiResponse<bool>.Fail("無此品牌或已停用");

				await _repo.RemoveAsync(new BrandFavoriteRequestDto
				{
					UserNumberId = userNumberId,
					BrandId = brandId
				}, ct);

				_logger.LogInformation("BrandFavorite removed. user={UserNumberId}, brand={BrandId}", userNumberId, brandId);
				return ApiResponse<bool>.Ok(true);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "BrandFavorite remove error. user={UserNumberId}, brand={BrandId}", userNumberId, brandId);
				return ApiResponse<bool>.Fail("取消收藏失敗");
			}
		}

		/// <summary>
		/// 查詢是否已收藏
		/// 成功訊息：已收藏/未收藏
		/// </summary>
		public async Task<ApiResponse<bool>> ExistsAsync(int userNumberId, int brandId, CancellationToken ct = default)
		{
			try
			{
				var exists = await _repo.ExistsAsync(userNumberId, brandId, ct);
				return ApiResponse<bool>.Ok(exists);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "BrandFavorite exists error. user={UserNumberId}, brand={BrandId}", userNumberId, brandId);
				return ApiResponse<bool>.Fail("查詢收藏狀態失敗");
			}
		}

		/// <summary>
		/// 取得我的品牌收藏清單
		/// 成功訊息：取得成功
		/// </summary>
		public async Task<ApiResponse<List<BrandFavoriteItemDto>>> GetMyListAsync(int userNumberId, CancellationToken ct = default)
		{
			try
			{
				var list = await _repo.GetMyListAsync(userNumberId, ct);
				return ApiResponse<List<BrandFavoriteItemDto>>.Ok(list);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "BrandFavorite list error. user={UserNumberId}", userNumberId);
				return ApiResponse<List<BrandFavoriteItemDto>>.Fail("取得收藏清單失敗");
			}
		}
	}
}
