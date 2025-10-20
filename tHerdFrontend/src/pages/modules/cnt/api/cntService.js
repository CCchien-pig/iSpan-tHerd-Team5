import axios from 'axios'

// ✅ 與 Swagger 一致的後端基底路徑
const API_BASE = 'https://localhost:7103/api/cnt'

/* ----------------------------------------------------
 * 📰 文章 Article API
 * --------------------------------------------------*/

/**
 * 取得文章列表
 * 支援：分類(categoryId)、關鍵字(q)、分頁(page, pageSize)
 */
export async function getArticleList({
    categoryId = null,
    q = "",
    page = 1,
    pageSize = 12
} = {}) {

    const params = {}
    if (categoryId) params.categoryId = categoryId
    if (q && q.trim()) params.q = q.trim()
    params.page = page
    params.pageSize = pageSize

    const res = await axios.get(`${API_BASE}/list`, { params })
    return res.data  // { items, total, page, pageSize }
}

/**
 * 取得文章詳細
 * @param {Number} id 文章 ID
 */
export async function getArticleDetail(id) {
    const res = await axios.get(`${API_BASE}/articles/${id}`)
    return res.data  // { canViewFullContent, data }
}


/* ----------------------------------------------------
 * 🧪 營養 Nutrition API
 * Base: /api/cnt/nutrition
 * --------------------------------------------------*/

/**
 * 取得食材列表
 * @param {Object} param0
 * keyword: 關鍵字, categoryId: 類別,
 * sort: name/newest/category/popular/nutrient:1104
 * page, pageSize
 */
export async function getNutritionList({
    keyword = '',
    categoryId = null,
    sort = 'name',
    page = 1,
    pageSize = 12
} = {}) {

    const params = { page, pageSize }
    if (keyword && keyword.trim()) params.keyword = keyword.trim()
    if (categoryId) params.categoryId = categoryId
    if (sort) params.sort = sort

    const res = await axios.get(`${API_BASE}/nutrition/list`, { params })
    return res.data  // { items, total, page, pageSize }
}

/**
 * 取得單筆食材營養明細
 * @param {Number} id SampleId
 */
export async function getNutritionDetail(id) {
    const res = await axios.get(`${API_BASE}/nutrition/${id}`)
    return res.data  // { sample: {...}, nutrients: [...] }
}
