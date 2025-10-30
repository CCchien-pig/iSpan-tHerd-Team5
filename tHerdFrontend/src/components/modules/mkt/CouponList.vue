<template>
  <div class="coupon-grid" v-if="couponList.length > 0">
    <CouponCard
      v-for="c in couponList"
      :key="c.couponId"
      :coupon="c"
      @receive="handleReceive"
    />
  </div>

  <div v-else class="text-center py-3 text-muted">
    目前沒有可領取的優惠券
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import http from '@/api/http'          // ✅ 自動附帶 JWT 的 axios
import Swal from 'sweetalert2'
import CouponCard from '@/components/modules/mkt/CouponCard.vue'

// ✅ Auth 狀態
const auth = useAuthStore()
const user = computed(() => auth.user)
const isLogin = computed(() => auth.isAuthenticated)

// ✅ 優惠券列表
const couponList = ref([])

// 🚀 初始化：撈取目前可領取優惠券
onMounted(async () => {
  try {
    const { data } = await http.get('/mkt/coupon') // ← 自動附 JWT（若已登入）
    couponList.value = data
  } catch (err) {
    console.error('[CouponPage] 載入失敗', err)
  }
})

// 🚀 領取優惠券
async function handleReceive(coupon) {
  if (!isLogin.value) {
    // ⚠️ 未登入 → 導向登入頁
    await Swal.fire({
      title: '請先登入會員',
      text: '登入後即可領取優惠券',
      icon: 'info',
      confirmButtonText: '前往登入',
      confirmButtonColor: '#007083'
    })
    window.location.href = '/login'
    return
  }

  try {
    // ✅ 呼叫後端領取 API（不需手動帶 token）
    const { data } = await http.post('/mkt/coupon/receive', {
      couponId: coupon.couponId
    })

    coupon.isReceived = true // 標記前端狀態
    await Swal.fire({
      title: '領取成功！',
      html: `<strong>${coupon.couponName}</strong> 已加入您的優惠券錢包`,
      icon: 'success',
      confirmButtonColor: 'rgb(0,112,131)',
    })
  } catch (err) {
    const msg = err.response?.data?.message ?? '系統發生錯誤'
    await Swal.fire({
      title: '領取失敗',
      text: msg,
      icon: 'error',
      confirmButtonText: '我知道了',
      confirmButtonColor: '#d33'
    })
  }
}
</script>

<style scoped>
.coupon-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 16px;
  margin-top: 16px;
}

@media (max-width: 768px) {
  .coupon-grid {
    grid-template-columns: 1fr;
  }
}
</style>
