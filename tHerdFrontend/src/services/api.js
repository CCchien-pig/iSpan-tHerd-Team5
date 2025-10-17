/**
 * api.js - Axios 實例配置
 * 功能：建立和配置 Axios 實例，統一處理 HTTP 請求
 * 特色：攔截器、錯誤處理、請求/回應轉換
 */

import axios from 'axios'

/**
 * 建立 Axios 實例
 * 配置：基礎URL、逾時時間、請求標頭等
 */
const apiClient = axios.create({
  // API 基礎URL - 從環境變數取得，預設為本地開發位址
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:3000/api',

  // 請求逾時時間（毫秒）
  timeout: 10000,

  // 預設請求標頭
  headers: {
    'Content-Type': 'application/json',
    Accept: 'application/json',
  },
})

/**
 * 請求攔截器
 * 功能：在請求發送前新增 token、修改配置等
 */
apiClient.interceptors.request.use(
  (config) => {
    // TODO: 於此處加入取得 token 的邏輯

    // 新增請求時間戳記（用於除錯）
    config.metadata = { startTime: new Date() }

    return config
  },
  (error) => {
    // 請求錯誤處理
    console.error('❌ Request Error:', error)
    return Promise.reject(error)
  }
)

export default apiClient
