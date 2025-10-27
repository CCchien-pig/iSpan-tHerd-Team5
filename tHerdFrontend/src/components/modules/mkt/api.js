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

// =============================
// 👇 新增優惠券專用的 API 方法
// =============================

const couponBase = '/api/mkt/coupon'

/**
 * 取得優惠券列表
 */
export const getCouponList = async () => {
  try {
    const response = await api.get(couponBase)
    return response.data
  } catch (error) {
    console.error('❌ 取得優惠券列表失敗', error)
    return []
  }
}

/**
 * 領取優惠券
 */
export const receiveCoupon = async (couponId) => {
  try {
    const response = await api.post(`${couponBase}/receive`, { couponId })
    return response.data
  } catch (error) {
    console.error('❌ 領取優惠券失敗', error)
    throw error
  }
}

export { baseAddress } // 需要時可以單獨用
export default api // API 呼叫統一用這個
