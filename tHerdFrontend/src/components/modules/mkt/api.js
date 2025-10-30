// ✅ 統一使用全域 http 實例（會自動附 JWT Token）
import http from '@/api/http'

// =============================
// 👇 優惠券 API 封裝
// =============================

/**
 * 取得優惠券列表
 */
export const getCouponList = async () => {
  try {
    const { data } = await http.get('/mkt/coupon')
    return data
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
    const { data } = await http.post('/mkt/coupon/receive', { couponId })
    return data
  } catch (error) {
    console.error('❌ 領取優惠券失敗', error)
    throw error
  }
}

export default {
  getCouponList,
  receiveCoupon,
}
