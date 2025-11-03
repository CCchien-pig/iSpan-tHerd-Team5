namespace tHerdBackend.Core.DTOs.USER;

public record CouponWalletItemDto(
	int CouponWalletId,
	int CouponId,
	DateTime ClaimedDate,
	DateTime? UsedDate,
	string Status,
	//bool Usable,
	bool IsUsable,
	tHerdBackend.Core.DTOs.MKT.MktCouponDto coupon // ★ 內嵌券 DTO
);
