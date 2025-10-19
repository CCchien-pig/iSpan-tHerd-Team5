/**
 * ProductsApi.js - 商品相關 API
 * 功能：封裝所有商品相關的 API 請求
 * 模組：prod（產品模組）
 *
 * 注意：商品相關的輔助方法（formatPrice、calculateDiscount 等）
 * 已移至 @/utils/productUtils.js
 */

import baseApi from '../../baseApi'

/**
 * 商品 API 類別
 * 提供商品查詢、評價、問答、收藏等功能
 */
class productsApi {
  path = '/prod'
  // ==================== 商品查詢 ====================

  /**
   * 查詢商品列表
   * @param {Object} params - 查詢參數
   * @param {string} params.keyword - 關鍵字搜尋（名稱、品牌）
   * @param {number} params.brandId - 篩選品牌 ID
   * @param {number} params.attrId - 篩選屬性 ID
   * @param {string} params.sort - 排序方式 (price_asc, price_desc, rating_desc, date_desc)
   * @param {number} params.page - 頁碼（預設：1）
   * @param {number} params.pageSize - 每頁筆數（預設：20）
   * @returns {Promise} API 回應
   * @example
   * const result = await productsApi.getProductList({
   *   keyword: '魚油',
   *   brandId: 5,
   *   page: 1,
   *   pageSize: 20
   * })
   */
  async getProductList(params = {}) {
    return await baseApi.get(`${this.path}/Products`, params)
  }

  /**
   * 查詢商品詳細資訊
   * @param {number} productId - 商品 ID
   * @returns {Promise} API 回應，包含完整商品資訊
   * @example
   * const result = await productsApi.getProductDetail(85180)
   */
  async getProductDetail(productId) {
    // console.log(await baseApi.get(`${this.path}/Products/${productId}`))

    return await baseApi.get(`${this.path}/Products/${productId}`)
  }

  // ==================== 屬性與成分 ====================

  /**
   * 查詢商品屬性清單
   * @returns {Promise} API 回應，包含所有屬性分類（功效、性別、年齡等）
   * @example
   * const result = await productsApi.getAttributes()
   */
  async getAttributes() {
    return await baseApi.get(`${this.path}/attributes`)
  }

  /**
   * 查詢成分清單
   * @returns {Promise} API 回應，包含所有成分資料
   * @example
   * const result = await productsApi.getIngredients()
   */
  async getIngredients() {
    return await baseApi.get(`${this.path}/ingredients`)
  }

  // ==================== 問答系統 ====================

  /**
   * 查詢商品問答列表
   * @param {number} productId - 商品 ID
   * @returns {Promise} API 回應，包含該商品的所有問答
   * @example
   * const result = await productsApi.getQuestions(85180)
   */
  async getQuestions(productId) {
    return await baseApi.get(`${this.path}/questions/${productId}`)
  }

  /**
   * 提交問題
   * @param {Object} data - 問題資料
   * @param {number} data.productId - 商品 ID
   * @param {string} data.questionContent - 問題內容
   * @returns {Promise} API 回應
   * @example
   * const result = await productsApi.submitQuestion({
   *   productId: 85180,
   *   questionContent: '請問保存期限是多久？'
   * })
   */
  async submitQuestion(data) {
    return await baseApi.post(`${this.path}/questions`, data)
  }

  /**
   * 回覆問題
   * @param {Object} data - 回覆資料
   * @param {number} data.questionId - 問題 ID
   * @param {string} data.answerContent - 回覆內容
   * @returns {Promise} API 回應
   * @example
   * const result = await productsApi.submitAnswer({
   *   questionId: 3001,
   *   answerContent: '保存期限標示在瓶身上，通常為 2 年。'
   * })
   */
  async submitAnswer(data) {
    return await baseApi.post(`${this.path}/answers`, data)
  }

  // ==================== 評價系統 ====================

  /**
   * 查詢商品評價列表
   * @param {number} productId - 商品 ID
   * @param {Object} params - 查詢參數
   * @param {number} params.page - 頁碼
   * @param {number} params.pageSize - 每頁筆數
   * @returns {Promise} API 回應，包含評價列表
   * @example
   * const result = await productsApi.getReviews(85180, { page: 1, pageSize: 10 })
   */
  async getReviews(productId, params = {}) {
    return await baseApi.get(`${this.path}/reviews/${productId}`, params)
  }

  /**
   * 提交商品評價
   * @param {Object} data - 評價資料
   * @param {number} data.productId - 商品 ID
   * @param {number} data.skuId - SKU ID
   * @param {number} data.rating - 評分（1-5）
   * @param {string} data.title - 評價標題
   * @param {string} data.content - 評價內容
   * @param {Array<File>} data.images - 評價圖片（選填）
   * @returns {Promise} API 回應
   * @example
   * const result = await productsApi.submitReview({
   *   productId: 85180,
   *   skuId: 9001,
   *   rating: 5,
   *   title: '很好吞！',
   *   content: '魚油沒有腥味，下次會回購！'
   * })
   */
  async submitReview(data) {
    return await baseApi.post(`${this.path}/reviews`, data)
  }

  // ==================== 收藏功能 ====================

  /**
   * 收藏商品（加入我的最愛）
   * @param {Object} data - 收藏資料
   * @param {number} data.productId - 商品 ID
   * @returns {Promise} API 回應
   * @example
   * const result = await productsApi.addFavorite({ productId: 85180 })
   */
  async addFavorite(data) {
    return await baseApi.post(`${this.path}/favorite`, data)
  }

  /**
   * 取消收藏商品（移除我的最愛）
   * @param {number} productId - 商品 ID
   * @returns {Promise} API 回應
   * @example
   * const result = await productsApi.removeFavorite(85180)
   */
  async removeFavorite(productId) {
    return await baseApi.delete(`${this.path}/favorite/${productId}`)
  }

  /**
   * 查詢使用者的收藏清單
   * @param {Object} params - 查詢參數
   * @param {number} params.page - 頁碼
   * @param {number} params.pageSize - 每頁筆數
   * @returns {Promise} API 回應
   * @example
   * const result = await productsApi.getFavoriteList({ page: 1, pageSize: 20 })
   */
  async getFavoriteList(params = {}) {
    return await baseApi.get(`${this.path}/favorite`, params)
  }

  // ==================== 按讚功能 ====================

  /**
   * 按讚商品
   * @param {Object} data - 按讚資料
   * @param {number} data.productId - 商品 ID
   * @returns {Promise} API 回應
   * @example
   * const result = await productsApi.likeProduct({ productId: 85180 })
   */
  async likeProduct(data) {
    return await baseApi.post(`${this.path}/like`, data)
  }

  /**
   * 取消按讚商品
   * @param {number} productId - 商品 ID
   * @returns {Promise} API 回應
   * @example
   * const result = await productsApi.unlikeProduct(85180)
   */
  async unlikeProduct(productId) {
    return await baseApi.delete(`${this.path}/like/${productId}`)
  }
}

// 建立並匯出 productsApi 實例
const ProductsApi = new productsApi()

export default ProductsApi

/**
 * 也可以匯出類別，讓使用者自行建立實例
 */
export { productsApi }
