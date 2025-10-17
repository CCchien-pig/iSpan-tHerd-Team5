// src/pages/modules/cnt/api/cntService.js
import axios from 'axios'

// 後端 CNT API 根路徑（依你後端埠號調整）
const API_BASE = 'https://localhost:7103/api/cnt'

/**
 * 取得文章列表
 * @param {Number} page 頁碼
 * @param {Number} pageSize 每頁筆數
 */
export async function getArticleList(page = 1, pageSize = 12) {
    const res = await axios.get(`${API_BASE}/list`, {
        params: { page, pageSize }
    })
    return res.data // { items, total, page, pageSize }
}

/**
 * 取得文章詳細
 * @param {Number} pageId 文章 ID
 */
export async function getArticleDetail(pageId) {
    const res = await axios.get(`${API_BASE}/detail/${pageId}`)
    return res.data // { canViewFullContent, data }
}
