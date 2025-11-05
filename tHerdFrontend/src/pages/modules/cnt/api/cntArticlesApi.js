// src/api/modules/cnt/cntArticlesApi.js

import baseApi from '@/api/baseApi'   // 路徑比照 ProductsApi 那一層

class CntArticlesApi {
    path = '/cnt'

    /** 建立 / 重新使用購買紀錄 */
    async createPurchase(pageId, paymentMethod = 'LINEPAY') {
        const res = await baseApi.post(
            `${this.path}/articles/${pageId}/purchase`,
            { paymentMethod }
        )
        // 後端直接回 DTO，不是 { success, data }，所以 baseApi 已經幫你把 data unwrap 了
        return res.data   // => PurchaseSummaryDto
    }

    /** 會員中心：我買過的文章列表 */
    async getMyPurchasedArticles() {
        const res = await baseApi.get(`${this.path}/member/purchased-articles`)
        return res.data   // => PurchasedArticleDto[]
    }
}

const cntArticlesApi = new CntArticlesApi()
export default cntArticlesApi
