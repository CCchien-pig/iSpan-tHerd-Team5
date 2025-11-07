// src/pages/modules/cnt/api/cntArticlesApi.js
import { http } from '@/api/http'

class CntArticlesApi {
    basePath = '/cnt'

    // 建立 / 取得文章購買訂單
    async createPurchase(pageId, paymentMethod = 'LINEPAY') {
        const { data } = await http.post(
            `${this.basePath}/articles/${pageId}/purchase`,
            { paymentMethod },
        )
        // 後端直接回 PurchaseSummaryDto，所以 data 就是訂單摘要
        return data
    }

    // ⭐ 開發用：呼叫 /mock-pay 把訂單標記為已付款
    async mockPay(purchaseId) {
        await http.post(`${this.basePath}/purchases/${purchaseId}/mock-pay`)
    }

    // 取得「我買過的文章列表」
    async getMyPurchasedArticles() {
        const { data } = await http.get(
            `${this.basePath}/member/purchased-articles`
        )
        // 後端回 IReadOnlyList<PurchasedArticleDto> → JSON 陣列
        return Array.isArray(data) ? data : []
    }
}

export default new CntArticlesApi()
