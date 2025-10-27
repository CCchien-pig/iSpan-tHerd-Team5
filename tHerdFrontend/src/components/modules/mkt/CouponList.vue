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
import { ref, onMounted } from 'vue'
import api from '@/components/modules/mkt/api'
import Swal from 'sweetalert2'
import CouponCard from './CouponCard.vue'

const couponList = ref([])

onMounted(async () => {
  const { data } = await api.get('/api/mkt/coupon')
  couponList.value = data
})

async function handleReceive(coupon) {
  try {
    const res = await api.post('/api/mkt/coupon/receive', { couponId: coupon.couponId })
    coupon.isReceived = true

    await Swal.fire({
      title: '領取成功！',
      html: `<strong>${coupon.couponName}</strong> 已成功加入您的優惠券錢包`,
      icon: 'success',
      confirmButtonText: 'OK',
      confirmButtonColor: 'rgb(0,112,131)'
    })
  } catch (err) {
    await Swal.fire({
      title: '領取失敗',
      text: err.response?.data?.message ?? '系統發生錯誤',
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
