// src/api/modules/cnt/cntArticlesApi.js
import baseApi from '@/api/baseApi'

class CntArticlesApi {
    path = '/cnt'

    // 建立 / 取得文章購買訂單
    async createPurchase(pageId, paymentMethod = 'LINEPAY') {
        const res = await baseApi.post(
            `${this.path}/articles/${pageId}/purchase`,
            { paymentMethod }
        )

        if (!res.success) {
            // 這裡可以再客製化丟錯
            throw new Error(res.message || '建立訂單失敗')
        }

        // 這裡的 res.data 就是後端的 PurchaseSummaryDto
        return res.data
    }

    // 取得「我買過的文章列表」
    async getMyPurchasedArticles() {
        const res = await baseApi.get(`${this.path}/member/purchased-articles`)

        if (!res.success) {
            throw new Error(res.message || '取得購買文章清單失敗')
        }

        // 這裡的 res.data 就是 PurchasedArticleDto[]
        return res.data
    }
}

const cntArticlesApi = new CntArticlesApi()
export default cntArticlesApi
