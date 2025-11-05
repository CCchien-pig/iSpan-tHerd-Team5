// composables/useAddToCart.js
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { warning, toast, error as showError } from '@/utils/sweetalert'
import ProductsApi from '@/api/modules/prod/ProductsApi'

/**
 * 共用「加入購物車」邏輯
 * - 自動處理會員 / 訪客身分
 * - 呼叫後端 API
 * - 顯示提示訊息
 */
export function useAddToCart() {
  const auth = useAuthStore()
  const user = computed(() => auth.user)
  const isLogin = computed(() => auth.isAuthenticated)

  /**
   * 加入購物車流程
   * @param {Object} product - 商品主資料
   * @param {Object} selectedSpec - 已選擇的規格
   * @param {number} qty - 數量
   */
  const addToCart = async (product, selectedSpec, qty = 1) => {
    if (!selectedSpec) {
      warning('請先選擇商品規格')
      return
    }

    try {
      // Step 1️⃣ 初始化 Auth（建立 guest / 取得 user）
      await auth.init()

      // Step 2️⃣ 組 payload
      const sessionId = await auth.ensureGuest()

      const payload = {
        skuId: selectedSpec.skuId,
        qty,
        unitPrice: selectedSpec.billingPrice,
        userNumberId: isLogin.value && user.value ? user.value.userNumberId : null,
        sessionId, // 訪客與會員都記錄 SessionId
      }

      // Step 3️⃣ 呼叫 API
      await ProductsApi.addToCart(payload)

      // Step 4️⃣ 提示成功
      toast(
        `已加入${payload.userNumberId ? '會員' : '訪客'}購物車：${product.productName} - ${selectedSpec.optionName} x ${qty}`,
        'success',
        2000
      )
    } catch (err) {
      console.error('❌ 加入購物車失敗', err)
      showError('加入購物車失敗，請稍後再試')
    }
  }

  return { addToCart }
}
