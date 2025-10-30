<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import Swal from 'sweetalert2'
import CouponCard from '@/components/modules/mkt/CouponCard.vue'
import { http } from '@/api/http'

const auth = useAuthStore()
const couponList = ref([])

onMounted(async () => {
  await auth.init()

  // ✅ 撈優惠券列表（後端會根據登入會員回傳 isReceived 狀態）
  const { data } = await http.get('/api/mkt/coupon')
  couponList.value = data
})

async function handleReceive(coupon) {
  try {
    const { data } = await http.post('/api/mkt/coupon/receive', {
      couponId: coupon.couponId
    })

    coupon.isReceived = true

    await Swal.fire({
      title: '領取成功！',
      html: `<strong>${coupon.couponName}</strong> 已加入您的錢包`,
      icon: 'success',
      confirmButtonColor: 'rgb(0,112,131)'
    })
  } catch (err) {
    const msg = err.response?.data?.message ?? '領取失敗，請稍後再試'
    await Swal.fire({
      title: '領取失敗',
      text: msg,
      icon: 'error',
      confirmButtonColor: '#d33'
    })
  }
}
</script>

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
