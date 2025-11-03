<template>
  <div class="coupon-grid" v-if="filteredCoupons.length > 0">
    <CouponCard
      v-for="c in filteredCoupons"
      :key="c.couponId"
      :coupon="c"
      @receive="handleReceive"
    />
  </div>

  <div v-else class="text-center py-3 text-muted">
    <h4><strong>🚀 登入會員可查看和領取優惠券 🐛</strong></h4>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, computed, watch } from 'vue'
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

// ✅ 遊戲紀錄狀態
const hasGameRecord = ref(false)
const lastGameScore = ref(null)

// 🚀 載入優惠券清單
async function loadCoupons() {
  if (!isLogin.value) {
    couponList.value = []
    return
  }

  try {
    const { data } = await http.get('/mkt/coupon') // 自動帶 JWT
    couponList.value = data
    console.log('💰 優惠券載入成功:', data.map(c => c.couponName))
  } catch (err) {
    console.error('[CouponPage] 載入失敗', err)
  }
}

// ✅ 檢查今日遊戲紀錄
async function checkGameRecord() {
  if (!isLogin.value) {
    hasGameRecord.value = false
    return
  }

  try {
    const userId = user.value?.userNumberId || user.value?.user_number_id
    if (!userId) {
      console.warn('⚠️ 無法取得 userNumberId')
      return
    }

    const { data } = await http.get(`/mkt/MktGameRecord/${userId}`)
    console.log('🎮 遊戲紀錄查詢結果:', data)

    if (data?.played === true && data?.record) {
      hasGameRecord.value = true
      lastGameScore.value = data.record.score
      localStorage.setItem('gameScore', data.record.score)
      console.log('✅ 偵測到今日分數:', data.record.score)
    } else {
      hasGameRecord.value = false
      lastGameScore.value = null
      localStorage.removeItem('gameScore')
      console.log('🚫 尚未玩過遊戲')
    }
  } catch (err) {
    console.error('🔍 檢查遊戲紀錄失敗', err)
    hasGameRecord.value = false
  }
}

// ✅ 掛載時初始化
onMounted(() => {
  if (isLogin.value) {
    loadCoupons()
    checkGameRecord()
  }

  // ✅ 監聽 localStorage「refreshCoupons」事件（遊戲結束後觸發）
  const onStorageChange = e => {
    if (e.key === 'refreshCoupons' && e.newValue === 'true') {
      console.log('🎯 偵測到 refreshCoupons，重新載入優惠券')
      loadCoupons()
      checkGameRecord()
      localStorage.removeItem('refreshCoupons')
    }
  }
  window.addEventListener('storage', onStorageChange)

  // ✅ 卸載監聽
  onUnmounted(() => {
    window.removeEventListener('storage', onStorageChange)
  })
})

// ✅ 登入狀態變化時自動刷新
watch(isLogin, newVal => {
  if (newVal) {
    loadCoupons()
    checkGameRecord()
  } else {
    couponList.value = []
    hasGameRecord.value = false
    lastGameScore.value = null
  }
})

// ✅ 過濾顯示優惠券（依遊戲紀錄決定）
const filteredCoupons = computed(() => {
  if (!isLogin.value) return []

  // ❌ 未玩過遊戲：不顯示 GAME 類券
  if (!hasGameRecord.value) {
    return couponList.value.filter(c => !c.couponCode?.startsWith('GAME'))
  }

  // ✅ 已玩過：顯示對應分數的遊戲券
  const score = lastGameScore.value ?? localStorage.getItem('gameScore')
  if (!score) {
    return couponList.value.filter(c => !c.couponCode?.startsWith('GAME'))
  }

  const normalizedScore = Number(score)
  console.log('🎯 篩選遊戲分數:', normalizedScore)

  return couponList.value.filter(c => {
    if (c.couponCode?.startsWith('GAME')) {
      const name = c.couponName?.replace(/[（）]/g, s => (s === '（' ? '(' : s === '）' ? ')' : s))
      return name?.includes(`翻牌遊戲獎勵(${normalizedScore}分)`)
    }
    return true
  })
})

// 🚀 領取優惠券
async function handleReceive(coupon) {
  if (!isLogin.value) {
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
    await http.post('/mkt/coupon/receive', { couponId: coupon.couponId })
    coupon.isReceived = true

    await Swal.fire({
      title: '領取成功！',
      html: `<strong>${coupon.couponName}</strong> 已加入您的優惠券錢包 🎉`,
      icon: 'success',
      confirmButtonColor: 'rgb(0,112,131)',
      timer: 1800,
      showConfirmButton: false
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
