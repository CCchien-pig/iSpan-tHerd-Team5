// 集中管理 /api/sup/Brands 相關呼叫，支援帶查詢參數與授權需求（預設前台不帶 auth）
// api 的 baseURL 已指向 import.meta.env.VITE_API_BASE_URL（https://localhost:7103/api），這裡只需填相對路徑 /sup/Brands 與 /sup/Brands/grouped
// 若 API 需要 token，再在呼叫處加入 { auth: true }，攔截器會自動加上 Bearer

// src/core/api/modules/sup/supBrands.js
import api from '@/core/api/api'

// 精選品牌：對應後端 GetFeaturedBrands()
// GET /api/sup/Brands/featured
export const getFeaturedBrands = (params = {}) => {
  const query = { isFeatured: true, ...params }
  return api.get('/sup/Brands/featured', { params: query })
}

// A–Z 品牌清單：對應後端 GetBrandsGroupedByFirstLetter()
// GET /sup/Brands/grouped
export const getGroupedBrands = (params = {}) => {
  // 可帶 isActive, isDiscountActive, isFeatured
  return api.get('/sup/Brands/grouped', { params })
}
