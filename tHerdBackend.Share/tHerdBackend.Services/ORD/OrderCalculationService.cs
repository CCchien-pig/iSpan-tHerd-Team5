using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using tHerdBackend.Infra.Models;
using tHerdBackend.Core.DTOs.ORD;
using System.Net.Http.Json;

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
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<CalculationResult> CalculateAsync(int? userNumberId, List<OrderItemDto> items, string? couponCode)
        {
            // 1. 計算商品小計
            decimal subtotal = items.Sum(x => x.SalePrice * x.Quantity);

            // 2. 計算運費（呼叫 SUP API）- 滿1500免運
            decimal shippingFee = 0;
            if (subtotal < 1500 && items.Any())
            {
                try
                {
                    var firstItem = items.First();
                    var response = await _httpClient.PostAsJsonAsync("/api/sup/LogisticsRate/order-shipping-fee", new
                    {
                        skuId = firstItem.SkuId,
                        qty = items.Sum(x => x.Quantity),
                        logisticsId = 1000
                    });

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<SupShippingResponse>();
                        shippingFee = result?.Data?.Data?.ShippingFee ?? 100;
                    }
                    else
                    {
                        shippingFee = 100;
                    }
                }
                catch
                {
                    shippingFee = 100;
                }
            }

            // 3. 優惠券折扣（呼叫 MKT API）
            decimal discount = 0;
            string? appliedCouponCode = null;

            if (!string.IsNullOrEmpty(couponCode))
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync("/api/promotion/calculate", new
                    {
                        userNumberId = userNumberId ?? 0,
                        subtotal,
                        couponId = couponCode
                    });

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<MktPromotionResponse>();
                        if (result?.Success == true)
                        {
                            discount = result.Data?.DiscountAmount ?? 0;
                            appliedCouponCode = couponCode;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"MKT優惠券API錯誤: {ex.Message}");
                }
            }

            // 4. 總金額
            decimal total = subtotal + shippingFee - discount;

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