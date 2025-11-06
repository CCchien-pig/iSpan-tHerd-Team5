// composables/modules/prod/cartStore.js
import { defineStore } from 'pinia'
import ProductsApi from '@/api/modules/prod/ProductsApi'
import { useAuthStore } from '@/stores/auth'

export const useCartStore = defineStore('cart', {
  state: () => ({
    totalCount: 0,
  }),

  actions: {
    async refreshCartCount() {
      try {
        const auth = useAuthStore()
        await auth.init()
        const  sessionId= await auth.ensureGuest()
        const userNumberId = auth.user?.userNumberId || null
        const summary = await ProductsApi.getCartSummary(userNumberId, sessionId)
        this.totalCount = summary?.ItemCount || 0
      } catch (err) {
        console.error('🚨 載入購物車摘要失敗:', err)
      }
    },

    async addItem(data) {
      const result = await ProductsApi.addToCart(data)
      if (result?.success) {
        await this.refreshCartCount()
      }
    },
  },
})
