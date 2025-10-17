/**
 * baseApi.js - 基礎 API 方法封裝
 * 功能：封裝所有 HTTP methods，提供統一的 API 呼叫介面
 * 特色：統一錯誤處理、請求配置、回應格式化
 */

import apiClient from './api'

/**
 * 基礎 API 類別
 * 提供所有標準 HTTP methods 的封裝
 */
class BaseApi {
  /**
   * GET 請求
   * @param {string} url - 請求路徑
   * @param {object} params - 查詢參數
   * @param {object} config - 額外的 axios 配置
   * @returns {Promise} API 回應資料
   */
  async get(url, params = {}, config = {}) {
    const response = await apiClient.get(url, {
      params,
      ...config,
    })

    const { data, success, message } = response.data
    return { ...response, data: data, success: success, message: message }
  }

  /**
   * POST 請求
   * @param {string} url - 請求路徑
   * @param {object} data - 請求主體資料
   * @param {object} config - 額外的 axios 配置
   * @returns {Promise} API 回應資料
   */
  async post(url, data = {}, config = {}) {
    const response = await apiClient.post(url, data, config)
    const { data: responseData, success, message } = response.data
    return { ...response, data: responseData, success: success, message: message }
  }

  /**
   * PUT 請求
   * @param {string} url - 請求路徑
   * @param {object} data - 請求主體資料
   * @param {object} config - 額外的 axios 配置
   * @returns {Promise} API 回應資料
   */
  async put(url, data = {}, config = {}) {
    const response = await apiClient.put(url, data, config)
    const { data: responseData, success, message } = response.data
    return { ...response, data: responseData, success: success, message: message }
  }

  /**
   * PATCH 請求
   * @param {string} url - 請求路徑
   * @param {object} data - 請求主體資料
   * @param {object} config - 額外的 axios 配置
   * @returns {Promise} API 回應資料
   */
  async patch(url, data = {}, config = {}) {
    const response = await apiClient.patch(url, data, config)
    const { data: responseData, success, message } = response.data
    return { ...response, data: responseData, success: success, message: message }
  }

  /**
   * DELETE 請求
   * @param {string} url - 請求路徑
   * @param {object} config - 額外的 axios 配置
   * @returns {Promise} API 回應資料
   */
  async delete(url, config = {}) {
    const response = await apiClient.delete(url, config)
    const { data: responseData, success, message } = response.data
    return { ...response, data: responseData, success: success, message: message }
  }
}

// 建立並匯出 BaseApi 實例
const baseApi = new BaseApi()

export default baseApi
