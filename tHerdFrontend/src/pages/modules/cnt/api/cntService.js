// src/pages/modules/cnt/api/cntService.js
import axios from 'axios'

// ✅ 使用你的後端路徑 (與 Swagger 一致)
const API_BASE = 'https://localhost:7103/api/cnt'

/**
 * 取得文章列表
 * 支援：分類(categoryId)、關鍵字(q)、分頁(page, pageSize)
 */
export async function getArticleList({ categoryId = null, q = "", page = 1, pageSize = 12 } = {}) {

    const params = {}
    if (categoryId) params.categoryId = categoryId;
    if (q && q.trim()) params.q = q.trim();
    params.page = page;
    params.pageSize = pageSize;

    const res = await axios.get(`${API_BASE}/list`, { params })
    return res.data  // { items, total, page, pageSize }
}

/**
 * 取得文章詳細
 * @param {Number} pageId 文章 ID
 */
export async function getArticleDetail(id) {
    const res = await axios.get(`${API_BASE}/articles/${id}`)
    return res.data  // { canViewFullContent, data }
}
