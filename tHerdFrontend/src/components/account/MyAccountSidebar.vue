<!-- /src/components/account/MyAccountSidebar.vue -->
<template>
  <section class="myaccount-sidebar">
    <!-- 手機版漢堡 -->
    <div class="myaccount-hamburger-menu sm-only">
      <i class="icon-hamburgermenufat h-menu" />
    </div>

    <!-- 我的帳戶 -->
    <div class="category">
      <!-- <div class="category-title">
        <router-link :to="{ name: 'userme' }" class="b">我的帳戶</router-link>
      </div> -->
      <ul class="category-links">
        <li><router-link class="profile" :to="safeTo('userme')">儀表板</router-link></li>
        <li><router-link class="orders" :to="safeTo('orders')">訂單</router-link></li>
        <!-- <li><router-link class="myaccount-subscription" :to="safeTo('subscriptions')">定期自動送貨享優惠</router-link></li>
        <li>
          <router-link class="messagecenter" :to="safeTo('messages')">訊息</router-link>
          <span v-if="badges.messages>0" class="badge badge-danger badge-small">{{ badges.messages }}</span>
        </li> -->
        <li><router-link class="accountsettings" :to="safeTo('account')">帳號資訊</router-link></li>
        <!-- <li><router-link class="accountsettings" :to="{ name: 'account' }">帳號資訊</router-link></li> -->
        <!-- <li><router-link class="addressbook" :to="safeTo('addressbook')">地址簿</router-link></li>
        <li><router-link class="payment" :to="safeTo('payments')">付款方式</router-link></li>-->
        <li><router-link class="lists" :to="safeTo('user-favorites')">我的最愛</router-link></li> 
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
      <div class="category-title"><span class="b">客服服務</span></div>
      <ul class="category-links">
        <!-- <li><router-link class="publicprofile" :to="safeTo('publicprofile')">我的地盤</router-link></li> -->
        <li><router-link class="publicprofile" :to="safeTo('TicketList')">客服紀錄</router-link></li>
        <!-- <li><router-link class="reviews" :to="safeTo('myreviews')">我的產品評論</router-link></li>
        <li>
          <router-link class="questions" :to="safeTo('myquestions')">我的問題</router-link>
          <span v-if="badges.questions>0" class="badge badge-danger badge-small">{{ badges.questions }}</span>
        </li>
        <li>
          <router-link class="answers" :to="safeTo('myanswers')">我的答案</router-link>
          <span v-if="badges.answers>0" class="badge badge-danger badge-small">{{ badges.answers }}</span>
        </li> -->
      </ul>
    </div>

    <!-- 設置 -->
    <div class="category">
      <div class="category-title"><span class="b">設置</span></div>
      <ul class="category-links">
        <!-- <li><router-link class="commpreferences" :to="safeTo('notifications')">通知設定</router-link></li> -->
        <li>
          <router-link class="two-step" :to="safeTo('twostep')">兩步驗證</router-link>
          <span class="badge-unread" v-if="badges.twostep"></span>
        </li>
        <!-- <li>
          <router-link class="passkey" :to="safeTo('passkeys')">通行碼</router-link>
          <span class="badge-unread" v-if="badges.passkeys"></span>
        </li> -->
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
/* .myaccount-sidebar { width: 100%; max-width: 320px; }
.category { margin-bottom: 20px; }
.category-title { font-weight: 700; margin-bottom: 8px; }
.category-links { list-style: none; padding-left: 0; margin: 0; }
.category-links li { margin: 6px 0; }
.category-links a.router-link-active { font-weight: 600; }
.badge { margin-left: 6px; }
.sm-only { display: none; }
@media (max-width: 768px) {
  .sm-only { display: block; }
} */
 /* === Sidebar：套用與樣板一致的色票與排版 === */
.omyaccount-sidebar {
  /* 固定在視窗內（可依你的 header 高度微調 top） */
  position: sticky;
  top: 92px;

  /* 以整個 sidebar 高度為 1 份，等分給各 category */
  display: flex;
  flex-direction: column;

  /* 讓 sidebar 吃滿可視高度；扣掉 header/footer 的空間可自行調整 */
  height: calc(100vh - 160px);
  min-height: 520px;
  max-width: 160px;

  /* 視覺：邊線與背景呼應樣板 panel */
  border: 1px solid rgb(77, 180, 193);
  border-radius: 6px;
  background: #fff;
  padding: 12px 14px;
}

.myaccount-sidebar {
  /* 仍保留貼齊視窗上緣的 sticky 行為 */
  position: sticky;
  top: 92px;

  /* 改：高度跟著父層（UserMe 的 aside）走，與主內容等高 */
  height: 100%;
  min-height: unset;

  /* 既有排版 */
  display: flex;
  flex-direction: column;
  max-width: 160px;

  border: 1px solid rgb(77, 180, 193);
  border-radius: 6px;
  background: #fff;
  padding: 12px 14px;
}

/* 每個分類平均分配整體高度（關鍵） */
.category {
  flex: 1 1 0;
  display: flex;
  flex-direction: column;
  /* 分隔各區塊的間距，與樣板 panel 類似，但較為輕量 */
  margin: 6px 0;
  padding: 6px 0;
  border-bottom: 1px dashed rgba(77, 180, 193, 0.5);
}
.category:last-child {
  border-bottom: none;
}

/* 分類標題（延用樣板色票） */
.category-title {
  font-weight: 700;
  margin-bottom: 6px;
  color: #2c3e50;
}

/* 連結清單本身也吃滿該分類，讓連結能垂直等距分布（關鍵） */
.category-links {
  list-style: none;
  padding-left: 0;
  margin: 0;

  /* 讓 link 在分類內平均分配間距 */
  display: flex;
  flex-direction: column;
  justify-content: space-evenly;

  /* 撐滿這個分類剩餘空間 */
  flex: 1;
  /* 避免太少連結時擠在一起，可加最小間距 */
  gap: 6px; /* gap 會與 space-evenly 一起作用，維持舒適距離 */
}

/* 單一連結項目 */
.category-links li {
  /* 取消舊有外距，交由 flex 分配 */
  margin: 0;
}

/* 連結樣式：字色與樣板 hint-list li 一致 (#4a5568) */
.category-links a {
  display: block;
  padding: 8px 10px;
  border-radius: 4px;

  font-size: 15px;
  line-height: 1.4;
  color: #4a5568;                /* ← 與樣板 <li> 同色 */
  text-decoration: none;

  /* 微淡的分隔邊，呼應樣板表格/面板的線條調性 */
  border: 1px solid transparent;
  transition: background-color .2s ease, color .2s ease, border-color .2s ease, padding-left .2s ease;
}

/* hover：沿用樣板色票（深綠 → 淺青） */
.category-links a:hover {
  background-color: rgba(77, 180, 193, 0.08);
  border-color: rgba(77, 180, 193, 0.35);
  color: #2c3e50;
  padding-left: 12px;
}

/* 目前路由 active 狀態（左邊高亮，與樣板用色一致） */
.category-links a.router-link-active {
  font-weight: 600;
  color: #2c3e50;
  background-color: rgba(0, 112, 131, 0.08);
  border-color: rgb(77, 180, 193);
  box-shadow: inset 3px 0 0 0 rgb(0, 112, 131);
}

/* 小徽章位置微調 */
.badge,
.badge-danger,
.badge-small {
  margin-left: 6px;
}

/* 手機行為：高度不做 sticky，避免壓縮內容；改為自然流佈局 */
.sm-only { display: none; }

@media (max-width: 992px) {
  .myaccount-sidebar {
    position: static;
    height: auto;
    min-height: 0;
    padding: 10px 12px;
  }
  .category {
    /* 行動裝置不強求平均高度，避免過度留白 */
    flex: initial;
    margin: 10px 0;
    padding: 8px 0;
  }
  .category-links {
    /* 行動裝置改為自然縱向排列 */
    justify-content: flex-start;
    gap: 8px;
  }
  .sm-only { display: block; }
}
 
</style>