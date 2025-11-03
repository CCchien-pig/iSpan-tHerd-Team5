namespace tHerdBackend.Core.DTOs.USER;

public class MemberRankDto
{
	public string MemberRankId { get; set; } = default!;
	public string RankName { get; set; } = default!;
	public decimal TotalSpentForUpgrade { get; set; }
	public int OrderCountForUpgrade { get; set; }
	public decimal RebateRate { get; set; }
	public string? RankDescription { get; set; }
	public bool IsActive { get; set; }
}
