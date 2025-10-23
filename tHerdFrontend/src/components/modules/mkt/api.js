import axios from 'axios'

// ✅ 共用基底網址
const baseAddress = 'https://localhost:7103'

// ✅ 建立 axios 實例
const api = axios.create({
  baseURL: baseAddress,
})

// ✅ 你也可以在這裡加上攔截器（非必要）
api.interceptors.response.use(
  (response) => response,
  (error) => Promise.reject(error),
)

export { baseAddress } // 需要時可以單獨用
export default api // API 呼叫統一用這個
