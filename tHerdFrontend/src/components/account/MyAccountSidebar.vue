<script setup lang="ts">
function noop(e: Event) {
  // 只是呈現成超連結樣式，不導航
  e.preventDefault()
}
</script>

<template>
  <section class="myaccount-sidebar">
    <!-- 我的帳戶 -->
    <div class="category">
      <div class="category-title">
        <!-- 這個有頁面就保留 router-link -->
        <router-link :to="{ name: 'userme' }" class="b">我的帳戶</router-link>
      </div>

      <ul class="category-links">
        <li>
          <router-link class="profile" :to="{ name: 'userme' }">儀表板</router-link>
        </li>

        <!-- 以下為「尚未有頁面」→ 以超連結樣式顯示，但不導頁 -->
        <li><a href="#" class="fake-link orders" @click="noop">訂單</a></li>
        <li><a href="#" class="fake-link myaccount-subscription" @click="noop">定期自動送貨享優惠</a></li>
        <li><a href="#" class="fake-link messagecenter" @click="noop">訊息</a></li>
      </ul>
    </div>

    <!-- 獎勳與節省 -->
    <div class="category">
      <div class="category-title">獎勳和節省</div>
      <ul class="category-links">
        <li><a href="#" class="fake-link rewards" @click="noop">獎勳金</a></li>
        <li><a href="#" class="fake-link deals" @click="noop">促銷與優惠</a></li>
      </ul>
    </div>

    <!-- 我的活動 -->
    <div class="category">
      <div class="category-title">我的活動</div>
      <ul class="category-links">
        <li><a href="#" class="fake-link lists" @click="noop">我的清單</a></li>
        <li><a href="#" class="fake-link affiliate" @click="noop">聯盟計畫</a></li>
      </ul>
    </div>

    <!-- 設置 -->
    <div class="category">
      <div class="category-title">設置</div>
      <ul class="category-links">
        <li><a href="#" class="fake-link addresses" @click="noop">地址簿</a></li>
        <li><a href="#" class="fake-link security" @click="noop">安全性與登入</a></li>
      </ul>
    </div>
  </section>
</template>

<style scoped>
.category { margin-bottom: 1rem; }
.category-title { font-weight: 700; margin-bottom: .25rem; }
.category-links { list-style: none; padding-left: 0; margin: 0; }
.category-links li { margin: .25rem 0; }

/* 超連結外觀但不導航 */
.fake-link {
  color: var(--bs-link-color, #0d6efd);
  text-decoration: underline;
  cursor: pointer;
}
.fake-link:hover { opacity: .9; }
</style>
