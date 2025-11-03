// 檔案建議：tHerdBackend.Services/Common/AssetFileResultHelper.cs
// 或 tHerdBackend.Core/Utilities/AssetFileResultHelper.cs

using System.Collections;
using System.Text.Json;

namespace tHerdBackend.Services.Common // 或 tHerdBackend.Core.Utilities
{
	/// <summary>
	/// 從未知形態(object/string/JsonElement/列舉)的回傳結果中，嘗試撈出第一筆 FileId / FileUrl。
	/// 過渡用：等你把 Repo 回傳改成強型別後，可以移除。
	/// </summary>
	public static class AssetFileResultHelper
	{
		public static bool TryPickFirstFile(object? result, out int fileId, out string? fileUrl)
		{
			fileId = 0;
			fileUrl = null;

			if (result == null) return false;

			// case 1: 物件有 data 屬性
			var dataProp = result.GetType().GetProperty("data");
			if (dataProp != null)
			{
				if (TryPickFromEnumerable(dataProp.GetValue(result) as IEnumerable, out fileId, out fileUrl))
					return true;
			}

			// case 2: 直接就是可列舉
			if (result is IEnumerable en)
			{
				if (TryPickFromEnumerable(en, out fileId, out fileUrl))
					return true;
			}

			// case 3: 字串 JSON
			if (result is string s && TryPickFromJsonString(s, out fileId, out fileUrl))
				return true;

			// case 4: JsonElement
			if (result is JsonElement je && TryPickFromJsonElement(je, out fileId, out fileUrl))
				return true;

			return false;
		}

		private static bool TryPickFromEnumerable(IEnumerable? en, out int fileId, out string? fileUrl)
		{
			fileId = 0;
			fileUrl = null;
			if (en == null) return false;

			foreach (var item in en)
			{
				if (item == null) continue;
				var t = item.GetType();
				var idProp = t.GetProperty("FileId");
				var urlProp = t.GetProperty("FileUrl");
				if (idProp != null && urlProp != null)
				{
					if (idProp.GetValue(item) is int id)
					{
						fileId = id;
						fileUrl = urlProp.GetValue(item) as string ?? string.Empty;
						return true;
					}
				}
			}
			return false;
		}

		private static bool TryPickFromJsonString(string s, out int fileId, out string? fileUrl)
		{
			fileId = 0; fileUrl = null;
			try
			{
				using var doc = JsonDocument.Parse(s);
				return TryPickFromJsonElement(doc.RootElement, out fileId, out fileUrl);
			}
			catch
			{
				return false;
			}
		}

		private static bool TryPickFromJsonElement(JsonElement root, out int fileId, out string? fileUrl)
		{
			fileId = 0; fileUrl = null;

			if (root.ValueKind == JsonValueKind.Object &&
				root.TryGetProperty("data", out var dataElem) &&
				dataElem.ValueKind == JsonValueKind.Array)
			{
				foreach (var item in dataElem.EnumerateArray())
				{
					if (item.TryGetProperty("FileId", out var idProp) &&
						idProp.TryGetInt32(out var id) &&
						item.TryGetProperty("FileUrl", out var urlProp) &&
						urlProp.ValueKind == JsonValueKind.String)
					{
						fileId = id;
						fileUrl = urlProp.GetString() ?? string.Empty;
						return true;
					}
				}
			}

			return false;
		}
	}
}
