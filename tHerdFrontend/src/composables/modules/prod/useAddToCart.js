// composables/useAddToCart.js
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/composables/modules/prod/cartStore'
import { warning, toast, error as showError } from '@/utils/sweetalert'
import ProductsApi from '@/api/modules/prod/ProductsApi'

/**
 * 共用「加入購物車」邏輯
 * - 自動處理會員 / 訪客身分
 * - 呼叫後端 API
 * - 立即刷新購物車紅點
 */
export function useAddToCart() {
  const auth = useAuthStore()
  const cartStore = useCartStore()
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
      // 初始化 Auth（建立 guest / 取得 user）
      await auth.init()
      const sessionId = await auth.ensureGuest()

      // 組 payload
      const payload = {
        skuId: selectedSpec.skuId,
        qty,
        unitPrice: selectedSpec.billingPrice,
        userNumberId: isLogin.value && user.value ? user.value.userNumberId : null,
        sessionId, // 訪客也有 sessionId
      }

      // 呼叫 API
      const res = await ProductsApi.addToCart(payload)

      // 立即刷新購物車紅點（Pinia）
      await cartStore.refreshCartCount()

      // 提示成功
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
