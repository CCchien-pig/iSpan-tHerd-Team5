<script setup>
import { onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
// import { useCartStore } from '@/components/modules/ord/composables/useCart.js' // 假設您有購物車 store
import { useTestCartStore } from './testcart.js'

const route = useRoute()
const router = useRouter()
// const cartStore = useCartStore()
const cartStore = useTestCartStore()

onMounted(() => {
  const { storeId, storeName, address, type } = route.query

  if (storeId) {
    // 1. 將門市資料存起來
    const storeData = {
      logisticsType: 'CVS',
      subType: type,
      storeId,
      storeName,
      address,
    }
    console.log('收到門市資料:', storeData)

    // 範例：存到 localStorage 以便結帳頁讀取
    // localStorage.setItem('selectedStore', JSON.stringify(storeData))

    // 或存到 Pinia:
    cartStore.setPickupStore(storeData)
  }

  // 2. 跳轉回結帳頁
  //router.replace({ name: 'Checkout' }) // 請確認您的結帳頁路由名稱
  router.replace({ name: 'TestCart' }) // 👈 確認跳轉回測試購物車頁面
})
</script>

<template>
  <div>正在處理門市資料，請稍候...</div>
</template>
