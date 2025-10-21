import { ref, computed } from 'vue'
import axios from 'axios'

// ⚠️ 用你自己本機或 ngrok 的 API URL
const API_BASE_URL = 'https://localhost:7103'

export function useCart() {
  const cartItems = ref([])
  const loading = ref(false)

  // 取得 SessionId (若無則建立)
  const getSessionId = () => {
    let sessionId = localStorage.getItem('cart_session_id')
    if (!sessionId) {
      sessionId = 'guest_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9)
      localStorage.setItem('cart_session_id', sessionId)
    }
    return sessionId
  }

  // 取得使用者ID (登入時寫入 localStorage)
  const getUserId = () => {
    const user = JSON.parse(localStorage.getItem('user') || 'null')
    return user?.userNumberId || null
  }

  // 計算總數量
  const totalQty = computed(() => cartItems.value.reduce((sum, item) => sum + item.quantity, 0))

  // 計算總金額
  const subtotal = computed(() => cartItems.value.reduce((sum, item) => sum + (item.unitPrice * item.quantity), 0))

  // ==============================
  // 載入購物車
  // ==============================
  const loadCart = async () => {
    loading.value = true
    try {
      const sessionId = getSessionId()
      const userNumberId = getUserId()
      const params = { sessionId }
      if (userNumberId) params.userNumberId = userNumberId

      const res = await axios.get(`${API_BASE_URL}/api/ord/Cart/items`, { params })
      if (res.data.success) cartItems.value = res.data.data.items
    } catch (err) {
      console.error('載入購物車失敗:', err)
      alert('載入購物車失敗: ' + err.message)
    } finally {
      loading.value = false
    }
  }

  // ==============================
  // 新增商品
  // ==============================
  const addToCart = async (productId, skuId, quantity = 1) => {
    loading.value = true
    try {
      const res = await axios.post(`${API_BASE_URL}/api/ord/Cart/add`, {
        sessionId: getSessionId(),
        userNumberId: getUserId(),
        productId,
        skuId,
        quantity
      })
      if (res.data.success) {
        await loadCart()
        alert('已加入購物車')
      } else alert(res.data.message)
    } catch (err) {
      console.error('加入購物車失敗:', err)
      alert('加入購物車失敗: ' + err.message)
    } finally {
      loading.value = false
    }
  }

  // ==============================
  // ✅ 結帳 + 綠界金流整合
  // ==============================
  const checkout = async () => {
    loading.value = true
    try {
      const userNumberId = getUserId() || 1001
      console.log('🛒 Checkout start, userNumberId:', userNumberId)

      // 1️⃣ 建立訂單
      const orderRes = await axios.post(`${API_BASE_URL}/ORD/CartTest/CheckoutFromCart`, { userNumberId })
      console.log('🧾 CheckoutFromCart response:', orderRes.data)

      if (!orderRes.data.success) {
        alert(orderRes.data.message || '建立訂單失敗')
        return
      }

      const orderId = orderRes.data.orderId
      console.log('✅ 訂單建立成功，orderId:', orderId)

      // 2️⃣ 呼叫金流 API
      console.log('💳 呼叫 ECPay API...')
      const paymentRes = await axios.post(
        `${API_BASE_URL}/api/ord/payment/ecpay/create`,
        {
          orderId: orderId.toString(),
          totalAmount: subtotal.value || 1000,
          itemName: 'tHerd 測試商品'
        },
        { responseType: 'text' } // ⚡ 關鍵：接收 HTML
      )

      console.log('💰 Payment API 回傳型別:', typeof paymentRes.data)
      console.log('💰 回傳內容預覽:', paymentRes.data.substring(0, 100))

      if (!paymentRes.data.includes('<form')) {
        console.error('❌ 沒有 form HTML，請檢查後端是否正確回傳 HTML')
        alert('付款表單生成失敗')
        return
      }

      // 3️⃣ 插入 HTML 並跳轉
      submitECPayForm(paymentRes.data)
    } catch (error) {
      console.error('❌ 結帳錯誤:', error)
      alert('結帳失敗: ' + error.message)
    } finally {
      loading.value = false
    }
  }

  // ==============================
  // ✅ 注入並提交 ECPay form
  // ==============================
  const submitECPayForm = (htmlString) => {
    console.log('📦 執行 submitECPayForm()...')
    try {
      // 建立一個新的 window 或 tab，避免 Vue sandbox 阻擋
      const newWin = window.open('', '_blank')
      newWin.document.open()
      newWin.document.write(htmlString)
      newWin.document.close()
      console.log('🚀 表單已寫入新分頁，準備自動 submit')
    } catch (err) {
      console.error('⚠️ submitECPayForm 錯誤:', err)
      alert('無法開啟綠界付款頁，請允許瀏覽器彈出視窗。')
    }
  }

  return {
    cartItems,
    loading,
    totalQty,
    subtotal,
    loadCart,
    addToCart,
    checkout // ✅ 新增 checkout 方法
  }
}
