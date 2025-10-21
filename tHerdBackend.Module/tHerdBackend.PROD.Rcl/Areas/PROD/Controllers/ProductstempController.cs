using ExcelDataReader;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.PROD.Rcl.Areas.PROD.Controllers
{
	[Area("PROD")]
	public class ProductstempController : Controller
	{

		private readonly tHerdDBContext _db;
		private readonly IWebHostEnvironment _env;

		public ProductstempController(tHerdDBContext repo, IWebHostEnvironment env)
		{
			_db = repo;
			_env = env;
		}

		public async Task<IActionResult> Index_ex_datatable(CancellationToken ct)
		{
            // await SeedSkuDataAsync();

            // await LoadImgsFromCSV(); // 匯入圖片測試

            var products = await _db.ProdProducts.ToListAsync(ct);

			var dtos = products.Select(p => new ProdProductDetailDto
			{
				ProductId = p.ProductId,
				BrandId = p.BrandId,
				BrandName = p.Brand?.BrandName,
				ProductCode = p.ProductCode,
				ProductName = p.ProductName,
				ShortDesc = p.ShortDesc,
				FullDesc = p.FullDesc,
				IsPublished = p.IsPublished
			}).ToList();

			return View(dtos); // 型別跟 View 宣告一致
		}

        public async Task LoadImgsFromCSV()
        {
            string csvPath = @"D:\iSpanProj\專題文件\PROD_imgs-20251019T145911Z-1-001\PROD_imgs\PROD_imgs_35-2.csv";

            if (!System.IO.File.Exists(csvPath))
            {
                Console.WriteLine($"❌ 找不到檔案：{csvPath}");
                return;
            }

            try
            {
                // 用 FileStream + FileShare.ReadWrite 允許共用讀取
                using (var fs = new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                {
                    // 跳過標題列
                    reader.ReadLine();

                    while (!reader.EndOfStream)
                    {
                        string? line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        // 分割第一個逗號（避免 URL 內含逗號被切錯）
                        var parts = line.Split(',', 2);
                        if (parts.Length < 2)
                            continue;

                        if (!int.TryParse(parts[0].Trim(), out int productId))
                        {
                            Console.WriteLine($"⚠️ 無法解析 ProductId：{parts[0]}");
                            continue;
                        }

                        productId = productId - 1000 + 14882;

                        string imgUrl = parts[1].Trim().Trim('"').Trim('\\');
                        if (string.IsNullOrEmpty(imgUrl))
                            continue;

                        // 1️ 檢查產品是否存在
                        var prod = await _db.ProdProducts.FirstOrDefaultAsync(p => p.ProductId == productId);
                        if (prod == null)
                        {
                            Console.WriteLine($"⚠️ 找不到 ProductId={productId}，略過。");
                            continue;
                        }

                        // 2️ 取得或建立 SysSeoMeta
                        var seoMeta = await _db.SysSeoMeta
                            .FirstOrDefaultAsync(s => s.RefTable == "Products" && s.RefId == productId);

                        if (seoMeta == null)
                        {
                            seoMeta = new SysSeoMetum
                            {
                                RefTable = "Products",
                                RefId = productId,
                                SeoTitle = prod.ProductName,
								SeoSlug = $"prod-{productId}",
                                SeoDesc = prod.ShortDesc ?? ""
                            };
                            _db.SysSeoMeta.Add(seoMeta);

                            await _db.SaveChangesAsync(); // 🔑 產生 SeoId
                        }

                        prod.SeoId = seoMeta.SeoId;

                        // 3️ 解析檔案資訊
                        var cleanUrl = imgUrl.Split('?')[0];
                        var ext = Path.GetExtension(cleanUrl)?.Trim('.').ToLower() ?? "jpg";
                        string mimeType = ext switch
                        {
                            "jpg" or "jpeg" => "image/jpeg",
                            "png" => "image/png",
                            "gif" => "image/gif",
                            "webp" => "image/webp",
                            _ => "application/octet-stream"
                        };

						var file = await _db.SysAssetFiles.FirstOrDefaultAsync(f => f.FileUrl == imgUrl);
						if (file == null) {
                            file = new SysAssetFile
                            {
                                FileKey = $"prod-{productId}-{Guid.NewGuid():N}".Substring(0, 20),
                                IsExternal = true,
                                FileUrl = imgUrl,
                                FileExt = ext,
                                MimeType = mimeType,
                                IsActive = true
                            };

                            await _db.SysAssetFiles.AddAsync(file);
                            await _db.SaveChangesAsync(); // 🔑 產生 FileId
                        }

                        // 4️ 建立關聯表 SysSeoMetaAsset
						var link = await _db.SysSeoMetaAssets
							.FirstOrDefaultAsync(s => s.SeoId == seoMeta.SeoId && s.FileId == file.FileId);
						if (link == null) {
                            link = new SysSeoMetaAsset
                            {
                                SeoId = seoMeta.SeoId,
                                FileId = file.FileId,
                                Role = "pr",
                                OrderSeq = 1,
                                IsPrimary = false,
                                CreatedDate = DateTime.Now,
                                IsActive = true
                            };

                            await _db.SysSeoMetaAssets.AddAsync(link);
                            await _db.SaveChangesAsync();
                        }
                        Console.WriteLine($"✅ ProductId={productId} 已新增圖片 {imgUrl}");
                    }
                }

                Console.WriteLine($"🎉 匯入完成。");
            }
            catch (IOException ioEx)
            {
                Console.WriteLine($"⚠️ 無法開啟 CSV（可能被佔用）：{ioEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 發生未預期錯誤：{ex.Message}");
            }
        }

        public async Task<int> SeedSpecificationsAsync(CancellationToken ct = default)
		{
			var products = await _db.ProdProducts
				.AsNoTracking()
				.Take(20) // 選 20 個商品來掛規格
				.ToListAsync(ct);

			if (!products.Any()) return 0;

			var random = new Random();
			var inserted = 0;

			foreach (var product in products)
			{
				// 建立規格群組（例如「容量」「口味」）
				var groups = new[]
				{
			"容量", "口味", "顏色"
		};

				foreach (var group in groups)
				{
					var config = new ProdSpecificationConfig
					{
						ProductId = product.ProductId,
						GroupName = group,
						OrderSeq = random.Next(1, 10)
					};
					_db.ProdSpecificationConfigs.Add(config);
					await _db.SaveChangesAsync(ct);

					// 建立規格選項
					var optionNames = group switch
					{
						"容量" => new[] { "250ml", "500ml", "1000ml" },
						"口味" => new[] { "香草", "巧克力", "草莓" },
						"顏色" => new[] { "紅色", "藍色", "黑色" },
						_ => Array.Empty<string>()
					};

					foreach (var name in optionNames)
					{
						var option = new ProdSpecificationOption
						{
							SpecificationConfigId = config.SpecificationConfigId,
							OptionName = name,
							OrderSeq = random.Next(1, 10)
						};
						_db.ProdSpecificationOptions.Add(option);
						await _db.SaveChangesAsync(ct);

						// 取得該商品的一些 SKU 來綁定這個選項
						var skus = await _db.ProdProductSkus
							.Where(s => s.ProductId == product.ProductId)
							.OrderBy(x => Guid.NewGuid())
							.Take(2) // 每個選項綁 2 個 SKU
							.ToListAsync(ct);

						//foreach (var sku in skus)
						//{
						//	var mapping = new ProdSkuSpecificationValue
						//	{
						//		SkuId = sku.SkuId,
						//		SpecificationOptionId = option.SpecificationOptionId
						//	};
						//	_db.ProdSkuSpecificationValues.Add(mapping);
						//	inserted++;
						//}
					}
				}
			}

			await _db.SaveChangesAsync(ct);
			return inserted;
		}


		public async Task<int> SeedSkuDataAsync(CancellationToken ct = default)
		{
			var products = await _db.ProdProducts
				.AsNoTracking()
				.Take(50) // 從商品中隨機取一些，避免只有一個商品被塞滿
				.ToListAsync(ct);

			if (!products.Any()) return 0;

			var random = new Random();
			var newSkus = new List<ProdProductSku>();

			for (int i = 0; i < 200; i++)
			{
				var product = products[random.Next(products.Count)];

				var specCode = $"SP{random.Next(100, 999)}";
				var skuCode = $"SKU-{product.ProductId}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";
				var barcode = $"BC{random.Next(100000000, 999999999)}";

				var sku = new ProdProductSku
				{
					SpecCode = specCode,
					SkuCode = skuCode,
					ProductId = product.ProductId,
					Barcode = barcode,

					CostPrice = (decimal)(random.Next(100, 500) + random.NextDouble()),
					ListPrice = (decimal)(random.Next(500, 1000) + random.NextDouble()),
					UnitPrice = (decimal)(random.Next(200, 800) + random.NextDouble()),
					SalePrice = (decimal)(random.Next(150, 700) + random.NextDouble()),

					StockQty = random.Next(0, 500),
					SafetyStockQty = random.Next(10, 50),
					ReorderPoint = random.Next(20, 100),
					MaxStockQty = random.Next(100, 1000),

					IsAllowBackorder = random.Next(0, 2) == 1,
					ShelfLifeDays = random.Next(30, 365),
					StartDate = DateTime.UtcNow,
					EndDate = null,
					IsActive = true
				};

				newSkus.Add(sku);
			}

			await _db.ProdProductSkus.AddRangeAsync(newSkus, ct);
			var affected = await _db.SaveChangesAsync(ct);
			return affected;
		}


		public async Task<IActionResult> importTypeExcelAsync(CancellationToken ct = default)
		{

			var Types = LoadTypesFromExcel();

			foreach (var t in Types)
			{
				// 先檢查是否已存在相同名稱 (ParentId 可能為 null)
				bool exists = await _db.ProdProductTypeConfigs
					.AnyAsync(x => x.ParentId == t.ParentId && x.ProductTypeName == t.ProductTypeName, ct);

				if (exists)
				{
					// 跳過或記錄 Log
					continue;
				}

				await _db.AddAsync(t, ct);
				await _db.SaveChangesAsync(ct);
			}			

			return Ok(new { success = true, count = Types.Count });
		}

		public List<ProdProductTypeConfig> LoadTypesFromExcel()
		{
			var ex = new List<ProdProductTypeConfig>();

			var path = @"C:\Prod_Fake.xlsx";

			if (!System.IO.File.Exists(path))
			{
				throw new FileNotFoundException($"找不到檔案: {path}");
			}

			using var stream = System.IO.File.Open(path, FileMode.Open, FileAccess.Read);


			System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

			// 這行要在方法內，不是最上面的 using 區塊
			using var reader = ExcelReaderFactory.CreateReader(stream);

			var dataSet = reader.AsDataSet();
			var table = dataSet.Tables[2];
			var now = DateTime.Now;

			for (int i = 1; i < table.Rows.Count; i++)
			{
				var row = table.Rows[i];

				var random = new Random();

				// 產生 1~4 位大寫英文亂數
				int len = random.Next(1, 5); // 1,2,3,4
				const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
				string code = new string(Enumerable.Repeat(chars, len)
					.Select(s => s[random.Next(s.Length)]).ToArray());

				// 產生一個整數亂數 (範例: 1~9999)
				int orderSeq = random.Next(1, 10000);

				var dto = new ProdProductTypeConfig
				{
					ProductTypeCode = code,
					ProductTypeName = row[1]?.ToString().Trim(),
					OrderSeq = orderSeq,
					IsActive = true
				};
				ex.Add(dto);
			}

			return ex;
		}

		public async Task ImportProductExcelAsync(CancellationToken ct = default)
		{
			var products = LoadProductsFromExcel("Prod_Fake.xlsx");

			foreach (var product in products)
			{
				if (string.IsNullOrWhiteSpace(product.ProductName)) continue;

				// 先清理字串避免空白/隱藏字元造成重複
				product.ProductName = product.ProductName.Trim();

				// 資料庫查詢是否已存在
				bool exists = await _db.ProdProducts
					.AnyAsync(p => p.ProductName == product.ProductName, ct);

				if (exists)
				{
					// 已存在就跳過，不新增
					continue;
				}

				// BrandId 邏輯
				if (product.BrandId > 1074)
				{
					var random = new Random();
					product.BrandId = random.Next(1000, 1075);
				}

				// ShortDesc 截斷
				int maxLength = 100; // 根據 DB 欄位限制
				product.ShortDesc = product.ShortDesc?.Length > maxLength
					? product.ShortDesc.Substring(0, maxLength)
					: product.ShortDesc;

				// 逐筆新增 & 逐筆提交
				await _db.AddAsync(product, ct);
				await _db.SaveChangesAsync(ct); // 每筆單獨 commit
			}
		}

		public static List<ProdProduct> LoadProductsFromExcel(string path)
		{
			var products = new List<ProdProduct>();

			System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

			// 這行要在方法內，不是最上面的 using 區塊
			using var stream = System.IO.File.Open(path, FileMode.Open, FileAccess.Read);
			using var reader = ExcelReaderFactory.CreateReader(stream);

			var dataSet = reader.AsDataSet();
			var table = dataSet.Tables[0];
			var now = DateTime.Now;
			for (int i = 1; i < table.Rows.Count; i++)
			{
				var row = table.Rows[i];
				var dto = new ProdProduct
				{
					BrandId = ToInt(row[2]),
					ProductName = row[3]?.ToString().Trim(),
					ShortDesc = row[6]?.ToString(),
					FullDesc = row[4]?.ToString(),
					VolumeCubicMeter = ToInt(row[2]),
					Weight = ToInt(row[2]),
					CreatedDate = now,
					IsPublished = true,
					VolumeUnit = "cm",
					Creator = 1003,
					ProductCode = $"M-{i}"

				};
				products.Add(dto);
			}

			return products;
		}

		//public async Task ImportCsvAsync(string path, CancellationToken ct = default)
		//{
		//	var products = LoadProductsFromExcel(path);

		//	foreach (var product in products)
		//	{
		//		if (string.IsNullOrWhiteSpace(product.ProductName)) continue;

		//		// 先清理字串避免空白/隱藏字元造成重複
		//		product.ProductName = product.ProductName.Trim();

		//		// 資料庫查詢是否已存在
		//		bool exists = await _db.ProdProducts
		//			.AnyAsync(p => p.ProductName == product.ProductName, ct);

		//		if (exists)
		//		{
		//			// 已存在就跳過，不新增
		//			continue;
		//		}

		//		if (product.BrandId>1074)
		//		{
		//			var random = new Random();
		//			product.BrandId = random.Next(1000, 1075);
		//		}

		//		int maxLength = 100; // 根據 DB 欄位限制
		//		product.ShortDesc = product.ShortDesc?.Length > maxLength
		//			? product.ShortDesc.Substring(0, maxLength)
		//			: product.ShortDesc;

		//		await _db.AddAsync(product, ct);
		//	}

		//	await _db.SaveChangesAsync(ct);
		//}

		private static int ToInt(object cellValue, int defaultValue = 0)
		{
			return int.TryParse(cellValue?.ToString(), out var result)
				? result
				: defaultValue;
		}


		/// <summary>
		///	產生隨機的規格群組與選項
		/// </summary>
		private void SpecRandom()
		{
			Random r = new Random();
			string[] groupNames = new string[] { "口味", "產地", "功率", "聯名", "贊助商", "原物料來源" };

			for (int i = 0; i < _db.ProdSpecificationConfigs.Count(); i++)
			{
				if (r.Next(100) < 30)
				{
					var groupName = groupNames[r.Next(groupNames.Length)];
					_db.ProdSpecificationConfigs.FirstOrDefault(x => x.SpecificationConfigId == 1000 + i).GroupName = groupName;
					_db.SaveChanges();
				}
			}
			

			for (int i = 1000; i < 1159; i++)
			{
				var insertCount = r.Next(4);
				for(int j=0;j<insertCount;j++)
				{
					var productId = _db.ProdProducts.FirstOrDefault(x => x.ProductId == i).ProductId;
					var config = new ProdSpecificationConfig()
					{
						ProductId = productId,
						GroupName = groupNames[r.Next(groupNames.Length)],
						OrderSeq = _db.ProdSpecificationConfigs.Where(x => x.ProductId == productId).Max(x => x.OrderSeq) + 1,
					};
					_db.ProdSpecificationConfigs.Add(config);
				}
			}
			_db.SaveChanges();
		}
	}
}
