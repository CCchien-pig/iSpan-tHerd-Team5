namespace tHerdBackend.Core.DTOs.USER;

public record CouponWalletItemDto(
	int CouponWalletId,
	int CouponId,
	DateTime ClaimedDate,
	DateTime? UsedDate,
	string Status,
	bool Usable
);
