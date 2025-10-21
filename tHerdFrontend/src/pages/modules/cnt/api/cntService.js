// src/pages/modules/cnt/api/cntService.js
import axios from "axios";

// 📌 統一 API 基底位址
const API_BASE = "https://localhost:7103/api/cnt";

/* ----------------------------------------------------
 * 📰 文章 Article API
 * -------------------------------------------------- */

/**
 * 取得文章清單
 * 對應後端：GET /api/cnt/list
 * 可傳參數：
 * { q, categoryId, page, pageSize }
 */
export async function getArticleList({
    q = "",
    categoryId = null,
    page = 1,
    pageSize = 9,
} = {}) {
    try {
        const params = { q, page, pageSize };
        if (categoryId) params.categoryId = categoryId;
        const { data } = await axios.get(`${API_BASE}/list`, { params });
        return data; // { items, total, page, pageSize }
    } catch (err) {
        console.error("getArticleList 錯誤:", err);
        return { items: [], total: 0, page: 1, pageSize: 0 };
    }
}

/**
 * 取得單篇文章詳細 + 同分類推薦
 * 對應後端：GET /api/cnt/articles/{id}
 */
export async function getArticleDetail(id) {
    try {
        const { data } = await axios.get(`${API_BASE}/articles/${id}`);
        return data; // { canViewFullContent, data, recommended }
    } catch (err) {
        console.error("getArticleDetail 錯誤:", err);
        return null;
    }
}

/* ----------------------------------------------------
 * 🧪 營養 Nutrition API
 * -------------------------------------------------- */

/**
 * 取得營養清單
 * 對應後端：GET /api/cnt/nutrition/list
 */
export async function getNutritionList({
    all = false,
    keyword = "",
    categoryId = null,
    sort = "name",
    page = 1,
    pageSize = 12,
} = {}) {
    try {
        const params = { page, pageSize, keyword, categoryId, sort, all };
        const { data } = await axios.get(`${API_BASE}/nutrition/list`, { params });
        return data; // { items, total, page, pageSize }
    } catch (err) {
        console.error("getNutritionList 錯誤:", err);
        return { items: [], total: 0 };
    }
}

/**
 * 取得單筆營養詳情
 * 對應後端：GET /api/cnt/nutrition/{id}
 */
export async function getNutritionDetail(id) {
    try {
        const { data } = await axios.get(`${API_BASE}/nutrition/${id}`);
        return data;
    } catch (err) {
        console.error("getNutritionDetail 錯誤:", err);
        return null;
    }
}

/**
 * 取得食物分類
 * 對應後端：GET /api/cnt/nutrition/foodcategories
 */
export async function getFoodCategories() {
    try {
        const { data } = await axios.get(`${API_BASE}/nutrition/foodcategories`);
        return data;
    } catch (err) {
        console.error("getFoodCategories 錯誤:", err);
        return [];
    }
}

/**
 * 營養比較
 * 對應後端：GET /api/cnt/nutrition/compare
 * @param {string} sampleIds 逗號分隔的 SampleId 清單
 * @param {string} analyteIds 逗號分隔的 AnalyteId 清單
 */
export async function getNutritionCompare(sampleIds, analyteIds) {
    try {
        const { data } = await axios.get(`${API_BASE}/nutrition/compare`, {
            params: { sampleIds, analyteIds },
        });
        return data;
    } catch (err) {
        console.error("getNutritionCompare 錯誤:", err);
        return { groups: [] };
    }
}
