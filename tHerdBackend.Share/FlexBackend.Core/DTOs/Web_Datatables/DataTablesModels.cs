using System.Text.Json.Serialization;

namespace FlexBackend.Core.Web_Datatables
{
	// -- DataTables (v1/v2 相容)：搜尋、排序、欄位描述 ------------------------
	public sealed class DtSearch
	{
		[JsonPropertyName("value")] public string? Value { get; set; }
		[JsonPropertyName("regex")] public bool Regex { get; set; }
	}

	public sealed class DtOrder
	{
		[JsonPropertyName("column")] public int Column { get; set; }
		[JsonPropertyName("dir")] public string Dir { get; set; } = "asc"; // "asc" | "desc"
	}

	public sealed class DtColumn
	{
		// 要對應前端 columns[i].data
		[JsonPropertyName("data")] public string? Data { get; set; }
		[JsonPropertyName("orderable")] public bool Orderable { get; set; }
		[JsonPropertyName("search")] public DtSearch? Search { get; set; }
	}

	// -- 伺服器端接收請求 ------------------------
	public sealed class DataTableRequest
	{
		[JsonPropertyName("draw")] public int Draw { get; set; }
		[JsonPropertyName("start")] public int Start { get; set; }
		[JsonPropertyName("length")] public int Length { get; set; }
		[JsonPropertyName("search")] public DtSearch? Search { get; set; }
		[JsonPropertyName("order")] public List<DtOrder>? Order { get; set; }
		[JsonPropertyName("columns")] public List<DtColumn>? Columns { get; set; }
	}

	// -- 回傳給 DataTables 的標準格式（泛型） ------------------------
	public sealed class DataTableResponse<T>
	{
		[JsonPropertyName("draw")] public int Draw { get; set; }
		[JsonPropertyName("recordsTotal")] public int RecordsTotal { get; set; }
		[JsonPropertyName("recordsFiltered")] public int RecordsFiltered { get; set; }
		[JsonPropertyName("data")] public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
		[JsonPropertyName("error")] public string? Error { get; set; }
	}
}