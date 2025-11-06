// src/pages/modules/cnt/api/cntArticlesApi.js
import { http } from '@/api/http'   // ✅ 改用 http，不要再用 baseApi

class CntArticlesApi {
    basePath = '/cnt'

    // 建立 / 取得文章購買訂單
    async createPurchase(pageId, paymentMethod = 'LINEPAY') {
        // http 的回傳是 axios 原始 response
        const { data: res } = await http.post(
            `${this.basePath}/articles/${pageId}/purchase`,
            { paymentMethod }
        )

        // 這裡假設後端統一回 { success, message, data }
        if (!res.success) {
            throw new Error(res.message || '建立訂單失敗')
        }

        // res.data 就是後端的 PurchaseSummaryDto
        return res.data
    }

    // 取得「我買過的文章列表」
    async getMyPurchasedArticles() {
        const { data: res } = await http.get(
            `${this.basePath}/member/purchased-articles`
        )

        if (!res.success) {
            throw new Error(res.message || '取得購買文章清單失敗')
        }

        // res.data 是 PurchasedArticleDto[]
        return res.data
    }
}

export default new CntArticlesApi()
