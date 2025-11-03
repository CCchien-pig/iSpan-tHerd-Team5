<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import Swal from 'sweetalert2'
import CouponCard from '@/components/modules/mkt/CouponCard.vue'
import { http } from '@/api/http'
import { useRouter } from 'vue-router'

const auth = useAuthStore()
const router = useRouter()
const couponList = ref([])

// ✅ 使用 computed 確保反應式登入狀態
const isLogin = computed(() => auth.isAuthenticated)

onMounted(async () => {
  await auth.init()

  try {
    // ⚠️ 若未登入，也能先撈公開券（後端若加 [Authorize] 則會擋掉）
    const { data } = await http.get('/api/mkt/coupon')
    couponList.value = data
  } catch (err) {
    console.warn('尚未登入或 API 被保護')
    couponList.value = []
  }
})

// ✅ 點擊領取
async function handleReceive(coupon) {
  // 1️⃣ 若尚未登入 → 提示並導向登入頁
  if (!isLogin.value) {
    await Swal.fire({
      title: '請先登入會員',
      text: '登入後即可領取優惠券',
      icon: 'info',
      confirmButtonText: '前往登入',
      confirmButtonColor: '#007083'
    })
    router.push({ name: 'userlogin' }) // ✅ 跳轉到登入頁
    return
  }

  // 2️⃣ 已登入 → 呼叫 API 領取
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
      需登入後才可以領取優惠券
    </div>
  </template>
