// stores/testcart.js
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export const useTestCartStore = defineStore('testCart', () => {
  // ================= 狀態 (State) =================
  // 1. 購物車商品 (這裡先用假資料，實際應從後端 API 取得)
  const cartItems = ref([
    {
      productId: 14246,
      skuId: 2680,
      productName: "Lake Avenue Nutrition, Omega-3 魚油",
      skuName: "30 粒",
      unitPrice: 500.0,
      salePrice: 346.0,
      quantity: 1,
      // subtotal 可以由 getter 計算，或像這樣存起來
    },
    // ... 其他商品
  ])

  // 2. 取貨門市資料 (核心重點!)
  const pickupStore = ref(null)

  // ================= 計算屬性 (Getters) =================
  const totalQuantity = computed(() => cartItems.value.reduce((sum, item) => sum + item.quantity, 0))
  const totalAmount = computed(() => cartItems.value.reduce((sum, item) => sum + (item.salePrice * item.quantity), 0))

  // ================= 動作 (Actions) =================
  
  // --- 物流相關 ---
  function setPickupStore(storeData) {
    pickupStore.value = storeData
    // 同步到 localStorage，防止重新整理後資料遺失
    try {
      localStorage.setItem('test_pickupStore', JSON.stringify(storeData))
    } catch (e) {
      console.error('無法儲存門市資料到 localStorage', e)
    }
  }

  function clearPickupStore() {
    pickupStore.value = null
    localStorage.removeItem('test_pickupStore')
  }

  // --- 購物車初始化 ---
  function initCart() {
    // 1. 從 localStorage 恢復門市資料
    const savedStore = localStorage.getItem('test_pickupStore')
    if (savedStore) {
      try {
        pickupStore.value = JSON.parse(savedStore)
      } catch (e) {
        console.error('解析門市資料失敗', e)
        localStorage.removeItem('test_pickupStore')
      }
    }

    // 2. (實際專案) 這裡應該呼叫 API 載入使用者的購物車商品
    // fetchCartItems()
  }

  // --- 商品操作 (範例) ---
  function addItem(product) { /* ... */ }
  function removeItem(skuId) {
    cartItems.value = cartItems.value.filter(item => item.skuId !== skuId)
  }
  function updateQuantity(skuId, quantity) {
     const item = cartItems.value.find(i => i.skuId === skuId)
     if (item) item.quantity = quantity
  }

  return {
    // State
    cartItems,
    pickupStore,
    // Getters
    totalQuantity,
    totalAmount,
    // Actions
    setPickupStore,
    clearPickupStore,
    initCart,
    addItem,
    removeItem,
    updateQuantity
  }
})