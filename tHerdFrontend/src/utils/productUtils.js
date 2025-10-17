/**
 * productUtils.js - 商品相關工具函數
 * 功能：提供商品相關的輔助功能
 */

/**
 * 建構商品列表查詢參數
 * @param {Object} filters - 篩選條件
 * @param {string} filters.keyword - 關鍵字搜尋
 * @param {number} filters.brandId - 品牌 ID
 * @param {number} filters.attrId - 屬性 ID
 * @param {string} filters.sort - 排序方式
 * @param {number} filters.page - 頁碼
 * @param {number} filters.pageSize - 每頁筆數
 * @returns {Object} 格式化的查詢參數
 * @example
 * const params = buildProductListParams({
 *   keyword: '魚油',
 *   brandId: 5,
 *   page: 1
 * })
 */
export function buildProductListParams(filters = {}) {
  const params = {}

  // 關鍵字搜尋
  if (filters.keyword) {
    params.keyword = filters.keyword
  }

  // 品牌篩選
  if (filters.brandId) {
    params.brandId = filters.brandId
  }

  // 屬性篩選
  if (filters.attrId) {
    params.attrId = filters.attrId
  }

  // 排序
  if (filters.sort) {
    params.sort = filters.sort
  }

  // 分頁
  params.page = filters.page || 1
  params.pageSize = filters.pageSize || 20

  return params
}

/**
 * 格式化價格顯示
 * @param {number} price - 價格
 * @param {string} currency - 貨幣符號（預設：NT$）
 * @returns {string} 格式化的價格字串
 * @example
 * formatPrice(1280) // "NT$ 1,280"
 * formatPrice(1280, 'USD') // "USD 1,280"
 */
export function formatPrice(price, currency = 'NT$') {
  if (price === null || price === undefined) return ''
  return `${currency} ${price.toLocaleString()}`
}

/**
 * 計算折扣百分比
 * @param {number} salePrice - 優惠價
 * @param {number} listPrice - 原價
 * @returns {number} 折扣百分比
 * @example
 * calculateDiscount(880, 1080) // 18 (表示折扣 18%)
 */
export function calculateDiscount(salePrice, listPrice) {
  if (!listPrice || listPrice === 0) return 0
  if (!salePrice) return 0
  return Math.round(((listPrice - salePrice) / listPrice) * 100)
}

/**
 * 檢查商品是否有優惠
 * @param {Object} product - 商品物件
 * @param {number} product.salePrice - 優惠價
 * @param {number} product.listPrice - 原價
 * @returns {boolean} 是否有優惠
 * @example
 * hasDiscount({ salePrice: 880, listPrice: 1080 }) // true
 * hasDiscount({ salePrice: 880, listPrice: 880 }) // false
 */
export function hasDiscount(product) {
  return product.salePrice && product.listPrice && product.salePrice < product.listPrice
}

/**
 * 計算折扣後的節省金額
 * @param {number} salePrice - 優惠價
 * @param {number} listPrice - 原價
 * @returns {number} 節省的金額
 * @example
 * calculateSavings(880, 1080) // 200
 */
export function calculateSavings(salePrice, listPrice) {
  if (!listPrice || !salePrice) return 0
  return Math.max(0, listPrice - salePrice)
}

/**
 * 格式化折扣顯示
 * @param {number} salePrice - 優惠價
 * @param {number} listPrice - 原價
 * @returns {string} 折扣顯示文字
 * @example
 * formatDiscount(880, 1080) // "省 18%"
 */
export function formatDiscount(salePrice, listPrice) {
  const discount = calculateDiscount(salePrice, listPrice)
  return discount > 0 ? `省 ${discount}%` : ''
}

/**
 * 格式化評分顯示
 * @param {number} rating - 評分（0-5）
 * @param {number} maxStars - 最大星數（預設：5）
 * @returns {string} 星星字串
 * @example
 * formatRating(4.5) // "⭐⭐⭐⭐⭐"
 */
export function formatRating(rating, maxStars = 5) {
  if (!rating || rating < 0) return '☆'.repeat(maxStars)
  const fullStars = Math.floor(rating)
  const halfStar = rating % 1 >= 0.5 ? 1 : 0
  const emptyStars = Math.max(0, maxStars - fullStars - halfStar)

  return '⭐'.repeat(fullStars) + (halfStar ? '⭐' : '') + '☆'.repeat(emptyStars)
}

/**
 * 檢查商品是否有庫存
 * @param {Object} product - 商品物件
 * @param {number} product.stock - 庫存數量
 * @param {boolean} product.isActive - 是否上架
 * @returns {boolean} 是否有庫存
 */
export function hasStock(product) {
  return product.isActive !== false && product.stock > 0
}

/**
 * 格式化商品規格選項名稱
 * @param {Object} spec - 規格物件
 * @param {string} spec.OptionName - 規格名稱
 * @returns {string} 格式化的規格名稱
 * @example
 * formatSpecName({ OptionName: "90 顆裝" }) // "90 顆裝"
 */
export function formatSpecName(spec) {
  return spec?.OptionName || '標準規格'
}

/**
 * 取得最便宜的規格
 * @param {Array} specs - 規格陣列
 * @returns {Object|null} 最便宜的規格
 */
export function getCheapestSpec(specs) {
  if (!specs || specs.length === 0) return null

  return specs.reduce((cheapest, current) => {
    const cheapestPrice = cheapest.SalePrice || cheapest.UnitPrice
    const currentPrice = current.SalePrice || current.UnitPrice
    return currentPrice < cheapestPrice ? current : cheapest
  })
}

/**
 * 檢查規格是否可購買
 * @param {Object} spec - 規格物件
 * @param {boolean} spec.IsActive - 是否可購買
 * @returns {boolean} 是否可購買
 */
export function isSpecAvailable(spec) {
  return spec?.IsActive === true
}

export default {
  buildProductListParams,
  formatPrice,
  calculateDiscount,
  hasDiscount,
  calculateSavings,
  formatDiscount,
  formatRating,
  hasStock,
  formatSpecName,
  getCheapestSpec,
  isSpecAvailable,
}
