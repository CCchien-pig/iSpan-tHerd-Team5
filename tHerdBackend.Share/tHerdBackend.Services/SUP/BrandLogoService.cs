using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.Interfaces.SUP;

public sealed class BrandLogoService : IBrandLogoService
{
	private readonly IBrandAssetFileRepository _repo;
	private IReadOnlyList<BrandLogoAssetDto>? _cache;
	private readonly object _lock = new();

	public BrandLogoService(IBrandAssetFileRepository repo)
	{
		_repo = repo;
	}

	public async Task<IDictionary<string, string>> BuildLogoMapAsync(CancellationToken ct = default)
	{
		if (_cache is null)
		{
			var data = await _repo.GetActiveBrandLogosAsync(56);
			lock (_lock)
			{
				_cache ??= data;
			}
		}

		// 以 AltText 建立簡單 map（可擴充正規化）
		// key 使用正規化後字串，value 為 FileUrl
		var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var g in _cache!
			.Where(a => !string.IsNullOrWhiteSpace(a.FileUrl))
			.GroupBy(a => Normalize(a.AltText)))
		{
			// 保留最新一筆
			var latest = g.OrderByDescending(x => x.CreatedDate).First();
			map[g.Key] = latest.FileUrl!;
		}

		return map;
	}

	public string? TryResolve(string brandName, string? brandCode = null)
	{
		if (_cache is null) return null; // 呼叫前建議先 BuildLogoMapAsync

		var nameKey = Normalize(brandName);
		var codeKey = Normalize(brandCode ?? "");

		// ✅ 改這裡：不再直接用 ToDictionary()，避免重複 Key
		var dict = _cache!
			.Where(a => !string.IsNullOrWhiteSpace(a.FileUrl))
			.GroupBy(a => Normalize(a.AltText))
			.ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedDate).First().FileUrl!, StringComparer.OrdinalIgnoreCase);

		if (dict.TryGetValue(nameKey, out var url)) return url;
		if (!string.IsNullOrEmpty(codeKey) && dict.TryGetValue(codeKey, out url)) return url;

		// 模糊包含（保留原邏輯）
		var hit = _cache!
			.Where(a => !string.IsNullOrWhiteSpace(a.FileUrl))
			.OrderByDescending(a => a.CreatedDate)
			.FirstOrDefault(a =>
			{
				var alt = Normalize(a.AltText);
				return alt.Contains(nameKey, StringComparison.OrdinalIgnoreCase)
					   || (!string.IsNullOrEmpty(codeKey) && alt.Contains(codeKey, StringComparison.OrdinalIgnoreCase));
			});

		return hit?.FileUrl;
	}

	private static string Normalize(string? s)
	{
		if (string.IsNullOrWhiteSpace(s)) return "";
		s = s.Trim();
		s = s.Replace('’', '\'').Replace('“', '"').Replace('”', '"');
		while (s.Contains("  ")) s = s.Replace("  ", " ");
		return s;
	}
}
