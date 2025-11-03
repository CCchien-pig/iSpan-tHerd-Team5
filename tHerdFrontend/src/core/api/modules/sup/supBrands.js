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
 * 品牌詳頁：組裝 Banner、Buttons、Accordion 等（建議後端一次組裝）
 * GET /sup/brands/{brandId}/detail
 * 後端對應：
 *  - Banner：SUP_Brand.ImgId -> SYS_AssetFile.FileUrl
 *  - Buttons：SUP_BrandProductTypeFilter（依 ButtonOrder 排序，回傳 text, order, id）
 *  - Accordions：SUP_BrandAccordionContent（依 Content 分組，組內依 OrderSeq 排序，回傳 title, body, order）
 *  - 後續可加入 orderedBlocks（由 SUP_BrandLayoutConfig 決定順序）
 *
 */
export const getBrandDetail = (brandId) => {
  return api.get(`/sup/brands/${brandId}/detail`)
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

/**
 *（可選）取得品牌對應的資產（若詳頁已組裝可不需要）
 * GET /sup/Brands/{brandId}/assets
 * 例如：LOGO、其他圖檔
 */
export const getBrandAssets = (brandId) => {
  return api.get(`/sup/Brands/${brandId}/assets`)
}

/**
 *（可選）取得品牌的商品類型過濾（若詳頁已組裝可不需要）
 * GET /sup/Brands/{brandId}/product-type-filters
 * 回傳：Array<{ id, text, order }>
 */
export const getBrandTypeFilters = (brandId) => {
  return api.get(`/sup/Brands/${brandId}/product-type-filters`)
}

/**
 *（可選）取得品牌的 Accordion 內容（若詳頁已組裝可不需要）
 * GET /sup/Brands/{brandId}/accordion-contents
 * 回傳：Array<{ contentKey, items:[{ title, body, order }] }>
 */
export const getBrandAccordionContents = (brandId) => {
  return api.get(`/sup/Brands/${brandId}/accordion-contents`)
}
