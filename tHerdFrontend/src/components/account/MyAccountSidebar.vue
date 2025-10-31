<template>
  <section class="myaccount-sidebar">
    <!-- 手機版漢堡：可依需要補上開關 -->
    <div class="myaccount-hamburger-menu sm-only">
      <i class="icon-hamburgermenufat h-menu" />
    </div>

    <!-- 類別區塊：我的帳戶 -->
    <div class="category">
      <div class="category-title">
        <router-link :to="{ name: 'userme' }" class="b">我的帳戶</router-link>
      </div>
      <ul class="category-links">
        <li><router-link class="profile" :to="{ name: 'userme' }">儀表板</router-link></li>
        <li><router-link class="orders" :to="{ name: 'orders' }">訂單</router-link></li>
        <li><router-link class="myaccount-subscription" :to="{ name: 'subscriptions' }">定期自動送貨享優惠</router-link></li>
        <li>
          <router-link class="messagecenter" :to="{ name: 'messages' }">訊息</router-link>
          <span v-if="badges.messages>0" class="badge badge-danger badge-small">{{ badges.messages }}</span>
        </li>
        <li><router-link class="accountsettings" :to="{ name: 'account' }">帳號資訊</router-link></li>
        <li><router-link class="addressbook" :to="{ name: 'addressbook' }">地址簿</router-link></li>
        <li><router-link class="payment" :to="{ name: 'payments' }">付款方式</router-link></li>
        <li><router-link class="lists" :to="{ name: 'wishlist' }">我的清單</router-link></li>
      </ul>
    </div>

    <!-- 類別區塊：獎勵和節省 -->
    <div class="category">
      <div class="category-title"><span class="b">獎勵和節省</span></div>
      <ul class="category-links">
        <li><router-link class="overview" :to="{ name: 'rewards' }">我的獎勵金</router-link></li>
        <li><router-link class="storecredits" :to="{ name: 'credits' }">帳戶餘額</router-link></li>
        <li><router-link class="salesoffers" :to="{ name: 'promotions' }">優惠促銷</router-link></li>
      </ul>
    </div>

    <!-- 類別區塊：我的活動 -->
    <div class="category">
      <div class="category-title"><span class="b">我的活動</span></div>
      <ul class="category-links">
        <li><router-link class="publicprofile" :to="{ name: 'publicprofile' }">我的地盤</router-link></li>
        <li><router-link class="reviews" :to="{ name: 'myreviews' }">我的產品評論</router-link></li>
        <li>
          <router-link class="questions" :to="{ name: 'myquestions' }">我的問題</router-link>
          <span v-if="badges.questions>0" class="badge badge-danger badge-small">{{ badges.questions }}</span>
        </li>
        <li>
          <router-link class="answers" :to="{ name: 'myanswers' }">我的答案</router-link>
          <span v-if="badges.answers>0" class="badge badge-danger badge-small">{{ badges.answers }}</span>
        </li>
      </ul>
    </div>

    <!-- 類別區塊：設置 -->
    <div class="category">
      <div class="category-title"><span class="b">設置</span></div>
      <ul class="category-links">
        <li><router-link class="commpreferences" :to="{ name: 'notifications' }">通知設定</router-link></li>
        <li>
          <router-link class="two-step" :to="{ name: 'twostep' }">兩步驗證</router-link>
          <span class="badge-unread" v-if="badges.twostep"></span>
        </li>
        <li>
          <router-link class="passkey" :to="{ name: 'passkeys' }">通行碼</router-link>
          <span class="badge-unread" v-if="badges.passkeys"></span>
        </li>
      </ul>
    </div>
  </section>
</template>

<script setup lang="ts">
import { reactive } from 'vue'

const badges = reactive({
  messages: 0,
  questions: 0,
  answers: 0,
  twostep: false,
  passkeys: false,
})
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
