/**
 * mockConfig.js - Mock 配置檔案
 * 功能：控制哪些 API 使用 Mock 資料
 * 特色：可以單獨控制每個 API 的 Mock 狀態
 */

/**
 * Mock 配置
 * 設定為 true 的 API 會使用 Mock 資料
 * 設定為 false 的 API 會發送真實請求
 */
export const mockConfig = {
  // ==================== 全域設定 ====================
  // 主開關：是否啟用 Mock（從環境變數讀取）
  enabled: import.meta.env.VITE_ENABLE_MOCK === 'true',

  // Mock 延遲時間（毫秒）- 模擬網路延遲
  delay: 1000,

  // ==================== 商品相關 API ====================
  product: {
    getProductList: false, // 查詢商品列表
    getProductDetail: false, // 查詢商品詳細
    getAttributes: true, // 查詢屬性清單
    getIngredients: true, // 查詢成分清單
    getQuestions: true, // 查詢問答列表
    submitQuestion: true, // 提交問題
    submitAnswer: true, // 回覆問題
    getReviews: true, // 查詢評價列表
    submitReview: true, // 提交評價
    addFavorite: true, // 收藏商品
    removeFavorite: true, // 取消收藏
    getFavoriteList: true, // 查詢收藏清單
    likeProduct: true, // 按讚商品
    unlikeProduct: true, // 取消按讚
  },
}

/**
 * 檢查特定 API 是否啟用 Mock
 * @param {string} module - 模組名稱（如：'product', 'user'）
 * @param {string} api - API 名稱（如：'getProductList'）
 * @returns {boolean} 是否啟用 Mock
 */
export function isMockEnabled(module, api, forceMock = false) {
  // 單支強制 mock（即使全域關閉）
  if (forceMock === true) {
    return true
  }

  // 如果全域未啟用，直接返回 false
  if (!mockConfig.enabled) {
    return false
  }

  // 檢查模組和 API 是否存在且啟用
  return mockConfig[module]?.[api] === true
}
/**
 * 顯示目前的 Mock 配置狀態
 */
export function showMockConfig() {
  console.group('🎭 Mock 配置狀態')
  console.log('全域啟用:', mockConfig.enabled)
  console.log('延遲時間:', mockConfig.delay + 'ms')

  Object.keys(mockConfig).forEach((key) => {
    if (key !== 'enabled' && key !== 'delay' && typeof mockConfig[key] === 'object') {
      console.group(`📦 ${key} 模組`)
      Object.entries(mockConfig[key]).forEach(([api, enabled]) => {
        console.log(`  ${api}:`, enabled ? '✅ 啟用' : '❌ 停用')
      })
      console.groupEnd()
    }
  })

  console.groupEnd()
}

export default mockConfig
