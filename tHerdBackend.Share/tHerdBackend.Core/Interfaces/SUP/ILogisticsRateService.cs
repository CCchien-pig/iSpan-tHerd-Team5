using tHerdBackend.Core.DTOs.SUP;

public interface ILogisticsRateService
{
	/// <summary>
	/// 檢查物流商是否存在
	/// </summary>
	Task<bool> CheckLogisticsExistsAsync(int logisticsId);


	/// <summary>
	/// 計算運費
	/// </summary>
	Task<ShippingFeeDto.ShippingFeeResponseDto> CalculateShippingFeeAsync(
		ShippingFeeDto.ShippingFeeRequestDto req,
		CancellationToken ct = default);

	/// <summary>
	/// 依物流商ID取得所有運費率
	/// </summary>
	Task<List<LogisticsRateDto>> GetByLogisticsIdAsync(
		int logisticsId,
		CancellationToken ct = default);
}



