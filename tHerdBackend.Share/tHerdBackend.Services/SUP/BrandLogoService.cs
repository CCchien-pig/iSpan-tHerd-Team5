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
		foreach (var a in _cache!)
		{
			var key = Normalize(a.AltText);
			if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(a.FileUrl))
			{
				// 只保留最新
				if (!map.ContainsKey(key) || map[key] != a.FileUrl!)
					map[key] = a.FileUrl!;
			}
		}
		return map;
	}

	public string? TryResolve(string brandName, string? brandCode = null)
	{
		if (_cache is null) return null; // 呼叫前建議先 BuildLogoMapAsync

		var nameKey = Normalize(brandName);
		var codeKey = Normalize(brandCode ?? "");

		// 完整匹配
		var dict = _cache!
			.Where(a => !string.IsNullOrWhiteSpace(a.FileUrl))
			.ToDictionary(a => Normalize(a.AltText), a => a.FileUrl!, StringComparer.OrdinalIgnoreCase);

		if (dict.TryGetValue(nameKey, out var url)) return url;
		if (!string.IsNullOrEmpty(codeKey) && dict.TryGetValue(codeKey, out url)) return url;

		// 模糊包含
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
