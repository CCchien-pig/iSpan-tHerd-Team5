using FlexBackend.Core.Interfaces.SUP;
using FlexBackend.Infra.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlexBackend.Services.ORD
{
    /// <summary>
    /// 訂單服務 - 處理訂單相關業務邏輯
    /// </summary>
    public class OrderService
    {
        private readonly tHerdDBContext _db;
        private readonly IStockService _stockService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            tHerdDBContext db,
            IStockService stockService,
            ILogger<OrderService> logger)
        {
            _db = db;
            _stockService = stockService;
            _logger = logger;
        }

        /// <summary>
        /// 處理訂單付款成功後的庫存扣減
        /// </summary>
        /// <param name="orderId">訂單編號</param>
        /// <param name="userId">操作人員 ID</param>
        /// <returns>(成功與否, 訊息)</returns>
        public async Task<(bool success, string message)> ProcessStockDeductionAsync(int orderId, int userId = 0)
        {
            try
            {
                _logger.LogInformation($"開始處理訂單 {orderId} 的庫存扣減");

                // 1. 取得訂單明細
                var orderItems = await _db.OrdOrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .ToListAsync();

                if (!orderItems.Any())
                {
                    _logger.LogWarning($"訂單 {orderId} 沒有明細資料");
                    return (false, "訂單明細不存在");
                }

                // 2. 逐一扣減每個 SKU 的庫存 (使用 FIFO)
                foreach (var item in orderItems)
                {
                    _logger.LogInformation($"處理 OrderItemId: {item.OrderItemId}, SkuId: {item.SkuId}, Qty: {item.Qty}");

                    var result = await _stockService.AdjustStockAsync(
                        batchId: 0,                              // 不指定批號,自動 FIFO
                        skuId: item.SkuId,
                        changeQty: item.Qty,
                        isAdd: false,                            // 扣減庫存
                        movementType: "Sale",                    // 銷售出庫
                        reviserId: userId,
                        remark: $"訂單 {orderId} 銷售出庫",
                        orderItemId: item.OrderItemId            // 綁定訂單明細
                    );

                    if (!result.Success)
                    {
                        _logger.LogError($"訂單 {orderId} SKU {item.SkuId} 扣庫存失敗: {result.Message}");
                        return (false, $"商品 SKU {item.SkuId} 扣庫存失敗: {result.Message}");
                    }

                    _logger.LogInformation(
                        $"訂單 {orderId} SKU {item.SkuId} 扣減成功 - " +
                        $"扣減數量: {result.AdjustedQty}, " +
                        $"剩餘庫存: {result.TotalStock}");
                }

                _logger.LogInformation($"訂單 {orderId} 所有商品庫存扣減完成");
                return (true, "庫存扣減成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"處理訂單 {orderId} 庫存扣減時發生錯誤");
                return (false, $"系統錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 處理訂單取消後的庫存釋放
        /// </summary>
        /// <param name="orderId">訂單編號</param>
        /// <param name="userId">操作人員 ID</param>
        /// <returns>(成功與否, 訊息)</returns>
        public async Task<(bool success, string message)> ProcessStockReleaseAsync(int orderId, int userId = 0)
        {
            try
            {
                _logger.LogInformation($"開始處理訂單 {orderId} 的庫存還原");

                // 1. 取得訂單明細
                var orderItems = await _db.OrdOrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .ToListAsync();

                if (!orderItems.Any())
                {
                    _logger.LogWarning($"訂單 {orderId} 沒有明細資料");
                    return (false, "訂單明細不存在");
                }

                // 2. 逐一還原每個 SKU 的庫存
                foreach (var item in orderItems)
                {
                    _logger.LogInformation($"還原 OrderItemId: {item.OrderItemId}, SkuId: {item.SkuId}, Qty: {item.Qty}");

                    var result = await _stockService.AdjustStockAsync(
                        batchId: 0,                              // 不指定批號,由服務端找原批次
                        skuId: item.SkuId,
                        changeQty: item.Qty,
                        isAdd: true,                             // 增加庫存
                        movementType: "Return",                  // 退貨入庫
                        reviserId: userId,
                        remark: $"訂單 {orderId} 取消還原庫存",
                        orderItemId: item.OrderItemId            // 綁定訂單明細
                    );

                    if (!result.Success)
                    {
                        _logger.LogError($"訂單 {orderId} SKU {item.SkuId} 還原庫存失敗: {result.Message}");
                        return (false, $"商品 SKU {item.SkuId} 還原庫存失敗: {result.Message}");
                    }

                    _logger.LogInformation(
                        $"訂單 {orderId} SKU {item.SkuId} 還原成功 - " +
                        $"還原數量: {result.AdjustedQty}, " +
                        $"目前庫存: {result.TotalStock}");
                }

                _logger.LogInformation($"訂單 {orderId} 所有商品庫存還原完成");
                return (true, "庫存還原成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"處理訂單 {orderId} 庫存還原時發生錯誤");
                return (false, $"系統錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 驗證訂單庫存是否充足
        /// </summary>
        /// <param name="orderId">訂單編號</param>
        /// <returns>(是否充足, 訊息)</returns>
        public async Task<(bool sufficient, string message)> ValidateOrderStockAsync(int orderId)
        {
            try
            {
                var orderItems = await _db.OrdOrderItems
                    .Include(oi => oi.Sku)
                    .Where(oi => oi.OrderId == orderId)
                    .ToListAsync();

                if (!orderItems.Any())
                {
                    return (false, "訂單明細不存在");
                }

                foreach (var item in orderItems)
                {
                    if (item.Sku.StockQty < item.Qty)
                    {
                        return (false, $"商品 {item.Sku.SkuCode} 庫存不足 (需要: {item.Qty}, 可用: {item.Sku.StockQty})");
                    }
                }

                return (true, "庫存充足");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"驗證訂單 {orderId} 庫存時發生錯誤");
                return (false, $"系統錯誤: {ex.Message}");
            }
        }
    }
}