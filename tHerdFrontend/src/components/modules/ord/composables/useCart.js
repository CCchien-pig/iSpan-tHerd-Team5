import { ref, computed } from 'vue'
import axios from 'axios'

const API_BASE_URL = 'https://localhost:7103'

export function useCart() {
  const cartItems = ref([])
  const loading = ref(false)
  
  // 取得 SessionId (如果沒有就產生一個)
  const getSessionId = () => {
    let sessionId = localStorage.getItem('cart_session_id')
    if (!sessionId) {
      sessionId = 'guest_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9)
      localStorage.setItem('cart_session_id', sessionId)
    }
    return sessionId
  }

  // 取得使用者ID (從登入狀態取得)
  const getUserId = () => {
    const user = JSON.parse(localStorage.getItem('user') || 'null')
    return user?.userNumberId || null
  }

  // 計算總數量
  const totalQty = computed(() => {
    return cartItems.value.reduce((sum, item) => sum + item.quantity, 0)
  })

  // 計算總金額
  const subtotal = computed(() => {
    return cartItems.value.reduce((sum, item) => sum + (item.unitPrice * item.quantity), 0)
  })

  // 載入購物車
  const loadCart = async () => {
    loading.value = true
    try {
      const sessionId = getSessionId()
      const userNumberId = getUserId()
      
      const params = { sessionId }
      if (userNumberId) {
        params.userNumberId = userNumberId
      }

      const response = await axios.get(`${API_BASE_URL}/api/ord/Cart/items`, { params })
      
      if (response.data.success) {
        cartItems.value = response.data.data.items
      }
    } catch (error) {
      console.error('載入購物車失敗:', error)
      alert('載入購物車失敗: ' + error.message)
    } finally {
      loading.value = false
    }
  }

  // 加入購物車
  const addToCart = async (productId, skuId, quantity = 1) => {
    loading.value = true
    try {
      const response = await axios.post(`${API_BASE_URL}/api/ord/Cart/add`, {
        sessionId: getSessionId(),
        userNumberId: getUserId(),
        productId,
        skuId,
        quantity
      })

      if (response.data.success) {
        await loadCart() // 重新載入購物車
        alert('已加入購物車')
        return true
      } else {
        alert(response.data.message)
        return false
      }
    } catch (error) {
      console.error('加入購物車失敗:', error)
      alert('加入購物車失敗: ' + error.message)
      return false
    } finally {
      loading.value = false
    }
  }

  // 更新數量
  const updateQuantity = async (cartItemId, quantity) => {
    loading.value = true
    try {
      const response = await axios.put(
        `${API_BASE_URL}/api/ord/Cart/update/${cartItemId}`, 
        { quantity }
      )

      if (response.data.success) {
        await loadCart()
        return true
      } else {
        alert(response.data.message)
        return false
      }
    } catch (error) {
      console.error('更新失敗:', error)
      alert('更新失敗: ' + error.message)
      return false
    } finally {
      loading.value = false
    }
  }

  // 移除商品
  const removeItem = async (cartItemId) => {
    if (!confirm('確定要移除此商品?')) return

    loading.value = true
    try {
      const response = await axios.delete(
        `${API_BASE_URL}/api/ord/Cart/remove/${cartItemId}`
      )

      if (response.data.success) {
        await loadCart()
        alert('已移除商品')
        return true
      } else {
        alert(response.data.message)
        return false
      }
    } catch (error) {
      console.error('移除失敗:', error)
      alert('移除失敗: ' + error.message)
      return false
    } finally {
      loading.value = false
    }
  }

  // 清空購物車
  const clearCart = async () => {
    if (!confirm('確定要清空購物車?')) return

    loading.value = true
    try {
      const sessionId = getSessionId()
      const userNumberId = getUserId()
      
      const params = { sessionId }
      if (userNumberId) {
        params.userNumberId = userNumberId
      }

      const response = await axios.delete(`${API_BASE_URL}/api/ord/Cart/clear`, { params })

      if (response.data.success) {
        cartItems.value = []
        alert('購物車已清空')
        return true
      } else {
        alert(response.data.message)
        return false
      }
    } catch (error) {
      console.error('清空失敗:', error)
      alert('清空失敗: ' + error.message)
      return false
    } finally {
      loading.value = false
    }
  }

  return {
    cartItems,
    loading,
    totalQty,
    subtotal,
    loadCart,
    addToCart,
    updateQuantity,
    removeItem,
    clearCart
  }
}