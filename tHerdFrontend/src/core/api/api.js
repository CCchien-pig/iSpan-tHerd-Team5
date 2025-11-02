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
  baseURL: import.meta.env.VITE_API_BASE_URL || 'https://localhost:7103/api',

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
    // 若 API 模組呼叫時指定 auth: true，才加上 Token
    if (config.auth === true) {
      const token = localStorage.getItem('access_token')
      if (token) config.headers.Authorization = `Bearer ${token}`
    }
    
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

apiClient.interceptors.response.use(
  (response) => {
    try {
      // console.log('[✅ Response Interceptor Triggered]', response)
      // 判斷是否有 metadata
      const start = response?.config?.metadata?.startTime
      const url = response?.config?.url || '(unknown URL)'

      if (start instanceof Date) {
        const duration = new Date() - start
        // console.info(`✅ [${response.status}] ${url} (${duration}ms)`)
      } else {
        console.info(`✅ [API] ${url} (no timing data)`)
      }
    } catch (err) {
      console.error('❌ Response Interceptor Error:', err)
    }

    // console.log(response)

    // 若沒有 data 屬性，直接回傳原始 response（for mock）
    return response ?? {}
  },

  (error) => {
    try {
      // 有 response：代表伺服器回應了
      if (error.response) {
        console.log('[❌ Error Interceptor Triggered]', error)

        const { status } = error.response
        const url = error.response?.config?.url || '(unknown URL)'
        console.error(`❌ [API Error ${status}] ${url}`)

        switch (status) {
          case 401:
            console.warn('🔐 Token 過期或未登入')
            break
          case 403:
            console.error('🚫 無權限存取資源')
            break
          case 500:
            console.error('💥 伺服器內部錯誤')
            break
          default:
            console.error(`❗ 未處理的狀態碼: ${status}`)
        }
      }
      // 沒 response：代表網路錯誤、CORS、mock 錯誤
      else {
        console.error('📡 無法連線到伺服器或請求被中斷')
        console.debug('[Error details]', error.message)
      }
    } catch (err) {
      console.error('❌ Response Error Handler Exception:', err)
    }

    return Promise.reject(error)
  }
)

export default apiClient
