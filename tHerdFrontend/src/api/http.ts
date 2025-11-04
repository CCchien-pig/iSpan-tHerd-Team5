// /src/api/http.ts
import axios, {
  AxiosError,
  AxiosRequestConfig,
  InternalAxiosRequestConfig,
} from 'axios'
import { useAuthStore } from '@/stores/auth'

// 一般 API 實例：會自動夾 Authorization
export const http = axios.create({
  baseURL: '/api', // 建議配合 vite proxy
})

// 專用 Refresh 實例：避免攔截器互相影響（不自動夾 Authorization）
const refreshHttp = axios.create({
  baseURL: '/api',
})

// ===== Request：自動夾 Authorization（保留你的寫法與除錯 log）=====
http.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const auth = useAuthStore()
  // ★ 冷啟/重新整理時，先把 storage 的值載回 store
  // ★ 取 token：先看 store，沒有就從兩種 storage 補
  const token =
    auth.accessToken ||
    localStorage.getItem('accessToken') ||
    sessionStorage.getItem('accessToken')
  if (token) {
    ;(config.headers ??= {} as any)
    ;(config.headers as any).Authorization = `Bearer ${token}`
    console.debug('[http] attach token', token.slice(0, 12) + '...')
  }
  return config
})

// ===== 401 Refresh 機制（最小整合）=====
let isRefreshing = false
let pendingQueue: Array<(token: string) => void> = []   // 等待刷新完成的請求（成功路徑）
let pendingRejects: Array<(err: any) => void> = []      // 等待刷新完成的請求（失敗路徑）

function enqueueRequest(resolve: (t: string) => void, reject: (e: any) => void) {
  pendingQueue.push(resolve)
  pendingRejects.push(reject)
}
function flushQueue(error: any, newToken?: string) {
  if (newToken) {
    pendingQueue.forEach(res => res(newToken))
  } else {
    pendingRejects.forEach(rej => rej(error))
  }
  pendingQueue = []
  pendingRejects = []
}

http.interceptors.response.use(
  (res) => res,
  async (error: AxiosError) => {
    const auth = useAuthStore()
    const original = error.config as (AxiosRequestConfig & { _retried?: boolean }) | undefined

    // 非 401、沒有回應、或已重試過 → 直接丟出
    if (!error.response || error.response.status !== 401 || !original || original._retried) {
      throw error
    }

     auth.loadFromStorage?.()

    // 沒有 refreshToken → 清空身分並丟出
    if (!auth.refreshToken) {
      auth.clear()
      throw error
    }

    // 若正在刷新 → 排隊等新 token，再用新 token 重試原請求
    if (isRefreshing) {
      return new Promise((resolve, reject) => {
        enqueueRequest((token) => {
          original._retried = true
          original.headers = original.headers ?? {}
          ;(original.headers as any).Authorization = `Bearer ${token}`
          resolve(http.request(original))
        }, reject)
      })
    }

    // 進入刷新流程
    isRefreshing = true
    try {
      const payload = { refreshToken: auth.refreshToken } // 你的 RefreshDto
      const { data } = await refreshHttp.post('/auth/refresh', payload)
      // 後端回傳格式：{ accessToken, accessExpiresAt, refreshToken }
      auth.setTokenPair(data.accessToken, data.accessExpiresAt, data.refreshToken)

      // 喚醒佇列（成功）
      flushQueue(null, data.accessToken)

      // 自己也重試一次
      original._retried = true
      original.headers = original.headers ?? {}
      ;(original.headers as any).Authorization = `Bearer ${data.accessToken}`
      return http.request(original)
    } catch (e) {
      // 刷新失敗 → 喚醒佇列（失敗），並清空身分
      flushQueue(e)
      auth.clear()
      throw e
    } finally {
      isRefreshing = false
    }
  }
)

export default http;
