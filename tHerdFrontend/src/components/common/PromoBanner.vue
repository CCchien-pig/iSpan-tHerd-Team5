<template>
  <!-- 促銷橫幅容器 -->
  <div class="promo-banner main-color-green py-2">
    <div class="container-fluid d-flex justify-content-between align-items-center">
      <div class="align-items-center d-flex gap-2">
        <!-- 左側促銷信息按鈕 -->
        <AppButton
          :text="leftText"
          variant="light"
          size="sm"
          :show-border="false"
          custom-class="main-color-green-text main-color-white border-0 p-2 rounded-pill"
          @click="goToGame"
        >
          <span class="hover-underline">{{ leftText }}</span>
        </AppButton>

        <!-- 右側促銷信息 -->
        <span class="hover-underline">
          <strong class="main-color-lightgreen-text">|會員福利|</strong>
          {{ rightText }}
        </span>
      </div>

      <div class="d-flex gap-2">
        <!-- 分享按鈕 -->
        <AppButton
          variant="outline-light"
          size="sm"
          :show-border="true"
          custom-class="bg-transparent py-2 px-3 rounded-pill"
          class="main-color-white"
        >
          <span class="hover-none text-black">
            <i class="bi bi-share main-color-green-text"></i>
          </span>
        </AppButton>
      </div>
    </div>
  </div>
</template>

<script>
import AppButton from '@/components/ui/AppButton.vue'
import { useRouter } from 'vue-router'
import Swal from 'sweetalert2'

export default {
  name: 'PromoBanner',
  components: { AppButton },
  props: {
    leftText: { type: String, default: '前往遊戲頁面' },
    rightText: { type: String, default: '挑戰翻牌遊戲，贏取專屬獎勵！( 全站之商品資料部分參考自 iHerb，僅供學術研究使用，無任何商業行為。 )' }
  },
  setup() {
    const router = useRouter()

    async function goToGame() {
      const token = localStorage.getItem('accessToken')
      if (!token) {
        await Swal.fire({
          icon: 'warning',
          title: '請先登入會員',
          text: '登入後才能參加遊戲活動並獲取專屬獎勵！',
          confirmButtonText: '前往登入',
          confirmButtonColor: 'rgb(0, 112, 131)',
          showCancelButton: true,
          cancelButtonText: '稍後再說',
          backdrop: `
            rgba(0,0,0,0.4)
            left top
            no-repeat
          `,
          customClass: {
            popup: 'rounded-4 shadow-lg',
            confirmButton: 'px-4 py-2 fw-bold',
            cancelButton: 'px-4 py-2'
          }
        }).then((result) => {
          if (result.isConfirmed) {
            // ✅ 導向 UserLogin 頁面（可用 name 或 path）
            router.push({ name: 'userlogin' })
          }
        })
        return
      }

      // ✅ 已登入 → 前往遊戲頁面
      router.push('/mkt/game')
    }

    return { goToGame }
  }
}
</script>

<style scoped>
.promo-banner .container-fluid {
  max-width: 1200px;
  margin: 0 auto;
  transition: all 0.3s ease;
}

@media (max-width: 900px) {
  .promo-banner .container-fluid {
    max-width: 100%;
    padding-left: 15px;
    padding-right: 15px;
  }
}
</style>
