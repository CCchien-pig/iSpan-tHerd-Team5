namespace tHerdBackend.Core.Interfaces.SUP
{
	public interface IBrandLogoService
	{
		Task<IDictionary<string, string>> BuildLogoMapAsync(CancellationToken ct = default);
		string? TryResolve(string brandName, string? brandCode = null);
	}
}
