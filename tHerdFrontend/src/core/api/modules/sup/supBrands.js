// src/core/api/modules/sup/supBrands.js
import api from '@/core/api/api'

/**
 * 精選品牌：對應後端 GetFeaturedBrands()
 * GET /sup/Brands/featured
 * 可帶參數：isActive, isFeatured（預設 true）, pageSize, pageIndex 等
 */
export const getFeaturedBrands = (params = {}) => {
  const query = { isFeatured: true, ...params }
  return api.get('/sup/Brands/featured', { params: query })
}

/**
 * A–Z 品牌清單：對應後端 GetBrandsGroupedByFirstLetter()
 * GET /sup/Brands/grouped
 * 可帶參數：isActive, isDiscountActive, isFeatured
 */
export const getGroupedBrands = (params = {}) => {
  return api.get('/sup/Brands/grouped', { params })
}

/**
 * 依品牌名稱（或關鍵字）搜尋品牌清單
 * GET /sup/Brands/search
 * @param {Object} params
 * @param {string} params.q 關鍵字（brandName/brandCode）
 * @param {boolean} [params.isActive]
 * @returns AxiosResponse<Array<{ brandId, brandName, logoUrl, ... }>>
 */
export const searchBrandsByName = (params = {}) => {
  return api.get('/sup/Brands/search', { params })
}

/**
 * 取得單一品牌基本資料（純品牌）
 * GET /sup/Brands/{brandId}
 * 回傳：{ brandId, brandName, logoUrl, imgId, ... }
 */
export const getBrand = (brandId) => {
  return api.get(`/sup/Brands/${brandId}`)
}

/**
 * 品牌詳頁：組裝 Banner、Buttons、Accordion 等
 * GET /sup/brands/{brandId}/detail
 * 後端對應：
 *  - Banner：SUP_Brand.ImgId -> SYS_AssetFile.FileUrl
 *  - Buttons：SUP_BrandProductTypeFilter（依 ButtonOrder 排序，回傳 text, order, id）
 *  - Accordions：SUP_BrandAccordionContent（依 Content 分組，組內依 OrderSeq 排序，回傳 title, body, order）
 *  - 後續可加入 orderedBlocks（由 SUP_BrandLayoutConfig 決定順序）
 *
 */
export const getBrandDetail = (brandId, config = {}) => {
  //   console.log('[API] getBrandDetail →', brandId)
  return api.get(`/sup/brands/${brandId}/detail`, { ...config })
}

// 取得品牌內容圖片（右側用，不分組）
// params 可帶 { folderId: 8, altText: '品牌名' }
export const getBrandContentImages = (brandId, params = {}, config = {}) => {
  // params 可帶 { folderId: 8, altText: 'Allmax' }
  //   console.log('[API] getBrandContentImages →', brandId, params)
  return api.get(`/sup/brands/${brandId}/content-images`, { params, ...config, auth: false })
}

/**
 *（可選）依 slug + brandId 取得詳頁（SEO 友善 URL）
 * GET /sup/brands/{slug}-{brandId}/detail 或 /sup/brands/slug/{slug}/detail
 * 若你的後端支援 slug，選擇對應一種即可。
 */
export const getBrandDetailBySlug = (slug, brandId) => {
  // 方案一（/sup/brands/allmax-1001/detail）
  // return api.get(`/sup/brands/${slug}-${brandId}/detail`)

  // 方案二（/sup/brands/slug/allmax/detail?brandId=1001）
  return api.get(`/sup/brands/slug/${slug}/detail`, { params: { brandId } })
}
