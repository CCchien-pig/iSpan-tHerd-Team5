using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using tHerdBackend.Core.DTOs.ORD;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Services.ORD
{
    public interface IOrderCalculationService
    {
        Task<CalculationResult> CalculateAsync(int? userNumberId, List<OrderItemDto> items, string? couponCode);
    }

    public class OrderCalculationService : IOrderCalculationService
    {
        private readonly tHerdDBContext _context;
        private readonly HttpClient _httpClient;

		public OrderCalculationService(tHerdDBContext context, IHttpClientFactory httpClientFactory)
		{
			_context = context;
			_httpClient = httpClientFactory.CreateClient("InternalApi"); // ← 改這裡
		}

		//public async Task<CalculationResult> CalculateAsync(int? userNumberId, List<OrderItemDto> items, string? couponCode)
		//      {
		//          // 1. 計算商品小計
		//          decimal subtotal = items.Sum(x => x.SalePrice * x.Quantity);

		//          // 2. 計算運費（呼叫 SUP API）- 滿1500免運
		//          decimal shippingFee = 0;
		//          if (subtotal < 1500 && items.Any())
		//          {
		//              try
		//              {
		//                  var firstItem = items.First();
		//                  var response = await _httpClient.PostAsJsonAsync("/api/sup/LogisticsRate/order-shipping-fee", new
		//                  {
		//                      skuId = firstItem.SkuId,
		//                      qty = items.Sum(x => x.Quantity),
		//                      logisticsId = 1000
		//                  });

		//                  if (response.IsSuccessStatusCode)
		//                  {
		//                      var result = await response.Content.ReadFromJsonAsync<SupShippingResponse>();
		//                      shippingFee = result?.Data?.Data?.ShippingFee ?? 100;
		//                  }
		//                  else
		//                  {
		//                      shippingFee = 100;
		//                  }
		//              }
		//              catch
		//              {
		//                  shippingFee = 100;
		//              }
		//          }

		//          // 3. 優惠券折扣（呼叫 MKT API）
		//          decimal discount = 0;
		//          string? appliedCouponCode = null;

		//          if (!string.IsNullOrEmpty(couponCode))
		//          {
		//              try
		//              {
		//                  var response = await _httpClient.PostAsJsonAsync("/api/promotion/calculate", new
		//                  {
		//				UserNumberId = userNumberId ?? 0,
		//                      subtotal,
		//				CouponCode = couponCode
		//                  });

		//                  if (response.IsSuccessStatusCode)
		//                  {
		//                      var result = await response.Content.ReadFromJsonAsync<MktPromotionResponse>();
		//                      if (result?.Success == true)
		//                      {
		//                          discount = result.Data?.DiscountAmount ?? 0;
		//                          appliedCouponCode = couponCode;
		//                      }
		//                  }
		//              }
		//              catch (Exception ex)
		//              {
		//                  Console.WriteLine($"MKT優惠券API錯誤: {ex.Message}");
		//              }
		//          }

		//          // 4. 總金額
		//          decimal total = subtotal + shippingFee - discount;

		//          return new CalculationResult
		//          {
		//              Subtotal = subtotal,
		//              ShippingFee = shippingFee,
		//              Discount = discount,
		//              Total = total,
		//              AppliedCouponCode = appliedCouponCode,
		//              FreeShippingThreshold = 1500,
		//              NeedMoreForFreeShipping = subtotal < 1500 ? 1500 - subtotal : 0
		//          };
		//      }

		public async Task<CalculationResult> CalculateAsync(int? userNumberId, List<OrderItemDto> items, string? couponCode)
		{
			decimal subtotal = items.Sum(x => x.SalePrice * x.Quantity);

			// === 運費（略，保留你現有邏輯） ===
			decimal shippingFee = 0;
			if (subtotal < 1500 && items.Any())
			{
				try
				{
					var firstItem = items.First();
					var respShip = await _httpClient.PostAsJsonAsync("api/sup/LogisticsRate/order-shipping-fee", new
					{
						skuId = firstItem.SkuId,
						qty = items.Sum(x => x.Quantity),
						logisticsId = 1000
					});
					if (respShip.IsSuccessStatusCode)
					{
						var shipJson = await respShip.Content.ReadAsStringAsync();
						using var sj = JsonDocument.Parse(shipJson);
						shippingFee = sj.RootElement
							.GetProperty("data").GetProperty("data")
							.TryGetProperty("shippingFee", out var feeEl) ? feeEl.GetDecimal() : 100m;
					}
					else shippingFee = 100;
				}
				catch { shippingFee = 100; }
			}

			// === 優惠 ===
			decimal discount = 0;
			string? appliedCouponCode = null;

			if (!string.IsNullOrWhiteSpace(couponCode))
			{
				try
				{
					// 同時帶上 CouponId（若服務端允許用任一欄位）
					var payload = new
					{
						userNumberId = userNumberId ?? 0,
						subtotal = subtotal,
						couponCode = couponCode,
						couponId = couponCode    // 若你的服務用 couponId，也吃得到。不要就忽略。
					};

					var resp = await _httpClient.PostAsJsonAsync("api/promotion/calculate", payload);
					var json = await resp.Content.ReadAsStringAsync();

					if (resp.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(json))
					{
						using var doc = JsonDocument.Parse(json);
						var root = doc.RootElement;

						bool ok = root.TryGetProperty("success", out var okEl) && okEl.GetBoolean();
						if (ok && root.TryGetProperty("data", out var data))
						{
							// 1) data.discountAmount 直接數值
							if (data.TryGetProperty("discountAmount", out var d1) && d1.ValueKind == JsonValueKind.Number)
								discount = d1.GetDecimal();

							// 2) data.discounts[] 陣列
							if (data.TryGetProperty("discounts", out var discountsEl) && discountsEl.ValueKind == JsonValueKind.Array)
							{
								foreach (var item in discountsEl.EnumerateArray())
								{
									if (item.TryGetProperty("discountAmount", out var dEl) && dEl.ValueKind == JsonValueKind.Number)
										discount += dEl.GetDecimal();
								}
							}

							// 3) data.appliedCouponCode（若服務有回）
							if (data.TryGetProperty("appliedCouponCode", out var acEl) && acEl.ValueKind == JsonValueKind.String)
								appliedCouponCode = acEl.GetString();

							// 4) 沒回 applied，但真的有折扣 → 以請求的 couponCode 當作 applied
							if (string.IsNullOrWhiteSpace(appliedCouponCode) && discount > 0)
								appliedCouponCode = couponCode;
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"MKT優惠券API錯誤: {ex.Message}");
				}
			}

			var total = subtotal + shippingFee - discount;

			return new CalculationResult
			{
				Subtotal = subtotal,
				ShippingFee = shippingFee,
				Discount = discount,
				Total = total,
				AppliedCouponCode = appliedCouponCode,
				FreeShippingThreshold = 1500,
				NeedMoreForFreeShipping = subtotal < 1500 ? 1500 - subtotal : 0
			};
		}

		// 內部 Response Models（只在這個 Service 用）
		private class SupShippingResponse
        {
            public bool Success { get; set; }
            public SupShippingData? Data { get; set; }
        }

        private class SupShippingData
        {
            public bool Success { get; set; }
            public SupShippingDetail? Data { get; set; }
        }

        private class SupShippingDetail
        {
            public decimal ShippingFee { get; set; }
        }

        private class MktPromotionResponse
        {
            public bool Success { get; set; }
            public MktPromotionData? Data { get; set; }
        }

        private class MktPromotionData
        {
            public decimal DiscountAmount { get; set; }
        }
    }
}