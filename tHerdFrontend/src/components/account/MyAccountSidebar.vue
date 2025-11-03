<!-- /src/components/account/MyAccountSidebar.vue -->
<template>
  <section class="myaccount-sidebar">
    <!-- 手機版漢堡 -->
    <div class="myaccount-hamburger-menu sm-only">
      <i class="icon-hamburgermenufat h-menu" />
    </div>

    <!-- 我的帳戶 -->
    <div class="category">
      <div class="category-title">
        <router-link :to="{ name: 'userme' }" class="b">我的帳戶</router-link>
      </div>
      <ul class="category-links">
        <li><router-link class="profile" :to="safeTo('userme')">儀表板</router-link></li>
        <li><router-link class="orders" :to="safeTo('orders')">訂單</router-link></li>
        <li><router-link class="myaccount-subscription" :to="safeTo('subscriptions')">定期自動送貨享優惠</router-link></li>
        <li>
          <router-link class="messagecenter" :to="safeTo('messages')">訊息</router-link>
          <span v-if="badges.messages>0" class="badge badge-danger badge-small">{{ badges.messages }}</span>
        </li>
        <li><router-link class="accountsettings" :to="safeTo('account')">帳號資訊</router-link></li>
        <li><router-link class="addressbook" :to="safeTo('addressbook')">地址簿</router-link></li>
        <li><router-link class="payment" :to="safeTo('payments')">付款方式</router-link></li>
        <li><router-link class="lists" :to="safeTo('wishlist')">我的清單</router-link></li>
      </ul>
    </div>

    <!-- 獎勵和節省 -->
    <div class="category">
      <div class="category-title"><span class="b">獎勵</span></div>
      <ul class="category-links">
        <li><router-link class="overview" :to="safeTo('rewards')">我的優惠券</router-link></li>
        <!-- <li><router-link class="storecredits" :to="safeTo('credits')">帳戶餘額</router-link></li> -->
        <!-- <li><router-link class="salesoffers" :to="safeTo('promotions')">優惠促銷</router-link></li> -->
      </ul>
    </div>

    <!-- 我的活動 -->
    <div class="category">
      <div class="category-title"><span class="b">我的活動</span></div>
      <ul class="category-links">
        <li><router-link class="publicprofile" :to="safeTo('publicprofile')">我的地盤</router-link></li>
        <li><router-link class="reviews" :to="safeTo('myreviews')">我的產品評論</router-link></li>
        <li>
          <router-link class="questions" :to="safeTo('myquestions')">我的問題</router-link>
          <span v-if="badges.questions>0" class="badge badge-danger badge-small">{{ badges.questions }}</span>
        </li>
        <li>
          <router-link class="answers" :to="safeTo('myanswers')">我的答案</router-link>
          <span v-if="badges.answers>0" class="badge badge-danger badge-small">{{ badges.answers }}</span>
        </li>
      </ul>
    </div>

    <!-- 設置 -->
    <div class="category">
      <div class="category-title"><span class="b">設置</span></div>
      <ul class="category-links">
        <li><router-link class="commpreferences" :to="safeTo('notifications')">通知設定</router-link></li>
        <li>
          <router-link class="two-step" :to="safeTo('twostep')">兩步驗證</router-link>
          <span class="badge-unread" v-if="badges.twostep"></span>
        </li>
        <li>
          <router-link class="passkey" :to="safeTo('passkeys')">通行碼</router-link>
          <span class="badge-unread" v-if="badges.passkeys"></span>
        </li>
      </ul>
    </div>
  </section>
</template>

<script setup>
import { reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { http } from '@/api/http'     // ★ 新增：呼叫 API

const router = useRouter()

const badges = reactive({
  messages: 0,
  questions: 0,
  answers: 0,
  twostep: false,
  passkeys: false,
})

onMounted(async () => {
  try {
    const { data } = await http.get('/user/me/detail') // 需回傳 { twoFactorEnabled: bool } 或 TwoFactorEnabled 欄位
    badges.twostep = !!(data.twoFactorEnabled ?? data.TwoFactorEnabled)
  } catch {
    badges.twostep = false
  }
})

function routeExists(name) {
  try {
    return router.hasRoute(name)
  } catch {
    return false
  }
}
function safeTo(name) {
  return routeExists(name) ? { name } : { name: 'userme' }
}
</script>

<style scoped>
.myaccount-sidebar { width: 100%; max-width: 320px; }
.category { margin-bottom: 20px; }
.category-title { font-weight: 700; margin-bottom: 8px; }
.category-links { list-style: none; padding-left: 0; margin: 0; }
.category-links li { margin: 6px 0; }
.category-links a.router-link-active { font-weight: 600; }
.badge { margin-left: 6px; }
.sm-only { display: none; }
@media (max-width: 768px) {
  .sm-only { display: block; }
}
</style>
