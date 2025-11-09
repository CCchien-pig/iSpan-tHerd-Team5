<template>
  <nav>
    <button :disabled="activeTab === 'info'" class="btn link-btn" @click="setTab('info')">
      配送資訊
    </button>
    <button :disabled="activeTab === 'fee'" class="btn link-btn" @click="setTab('fee')">
      運費計算
    </button>
    <button :disabled="activeTab === 'map'" class="btn link-btn" @click="setTab('map')">
      門市地圖查詢
    </button>
  </nav>

  <!-- <StoreMap /> -->
  <!-- <LogisticsListTable /> -->

  <!-- 統一標題區 -->
  <header class="page-header">
    <h1>{{ titleMap[activeTab].title }}</h1>
    <p class="subtitle" v-if="titleMap[activeTab].desc">{{ titleMap[activeTab].desc }}</p>
  </header>

  <section v-if="activeTab === 'info'">
    <div class="info-wrap">
      <div class="panel">
        <h3>配送說明</h3>
        <ul class="hint-list">
          <li>本館提供多種運送方式，實際可選項目依商品/地區/訂單條件而定。</li>
          <li>工作日不含例假日與國定假日；特殊活動或天候因素可能影響配達時效。</li>
        </ul>
      </div>

      <div class="panel">
        <!-- 運送方式一覽（延用 LogisticsListTable 現有 API 資料） -->
        <LogisticsListTable />
      </div>
      <!-- 參考頁常見段落：到貨時間、限制、注意事項 -->
      <div class="panel">
        <h3>到貨時間</h3>
        <ul class="flat">
          <li>一般宅配約 1~3 個工作日，超商取貨約 2~4 個工作日；偏遠地區與特殊節日將順延。</li>
        </ul>
      </div>
      <div class="panel">
        <h3>配送限制</h3>
        <ul class="flat">
          <li>部分大型或溫控商品不支援超商取貨。</li>
          <li>離島或偏遠地區部分物流商不配送或額外加價。</li>
        </ul>
      </div>
      <div class="panel">
        <h3>注意事項</h3>
        <ul class="flat">
          <li>訂單成立後無法變更配送方式；如需變更請取消後重下單。</li>
          <li>收件資訊請務必正確，以免影響時效或退件。</li>
        </ul>
      </div>
    </div>
  </section>

  <!-- 不換路由，直接載入對應元件 -->
  <section v-show="activeTab === 'fee'">
    <LogisticsFee />
  </section>

  <section v-show="activeTab === 'map'">
    <StoreMap />
  </section>
</template>

<script setup>
import { ref, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import LogisticsListTable from '@/components/modules/sup/logistics/LogisticsList.vue'
import LogisticsFee from '@/components/modules/sup/logistics/LogisticsFee.vue' // 將原本頁面內容抽成元件
import StoreMap from '@/components/modules/sup/logistics/StoreMap.vue'

const route = useRoute()
const router = useRouter()

const activeTab = ref('info')
const titleMap = {
  info: { title: '配送資訊', desc: '各物流方式、到貨時間與限制說明' },
  fee: { title: '運費計算', desc: '依條件估算運費與區間費率' },
  map: { title: '門市地圖查詢', desc: '搜尋鄰近門市據點與取貨服務' },
}

// 依 URL 查詢參數同步頁籤（進場與路由變更時）
const syncFromQuery = () => {
  const raw = (route.query.tab || '').toString().toLowerCase()
  activeTab.value = ['info', 'fee', 'map'].includes(raw) ? raw : 'info'
}
onMounted(syncFromQuery)
watch(() => route.query.tab, syncFromQuery)

// 內部切換頁籤時也更新 URL，利於可分享與回上一頁
const setTab = (tab) => {
  if (tab === activeTab.value) return
  activeTab.value = tab
  router.replace({ path: '/sup/logistics-info', query: { tab } })
}
</script>

<style scoped>
nav {
  padding: 1rem;
  text-align: center;
}
nav a {
  margin-right: 1rem;
  text-decoration: none;
  color: #2c3e50;
}
nav a:hover {
  color: #42b983;
}

.btn.link-btn {
  display: inline-block;
  padding: 8px 22px;
  margin: 0 8px 12px 0;
  border: none;
  border-radius: 4px;
  font-size: 16px;
  font-weight: 600;
  cursor: pointer;
  background-color: rgb(0, 112, 131);
  color: rgb(248, 249, 250);
  transition: background-color 0.3s ease;
}
.btn.link-btn:hover {
  background-color: rgb(77, 180, 193);
  color: rgb(248, 249, 250);
}
/* 新增 disabled 樣式 */
.btn.link-btn:disabled {
  opacity: 0.6; /* 降低透明度 */
  cursor: default; /* 滑鼠游標改為預設箭頭 */
  background-color: rgba(
    38,
    119,
    133,
    0.588
  ); /* 可以改成一個比較淺的顏色，或者維持原色但降低透明度 */

  color: rgb(81, 90, 99);
  /* 也可以考慮加個底線或邊框來強調它是當前選中的項目 */
  /* border-bottom: 2px solid #2c3e50; */
}

/* 確保 disabled 狀態下 hover 不會改變顏色 */
.btn.link-btn:disabled:hover {
  background-color: rgb(77, 180, 193); /* 維持 disabled 的背景色 */
  color: rgb(248, 249, 250);
}

.toolbar {
  display: flex;
  gap: 8px;
  padding: 0 16px;
  margin-top: 8px;
}

.info-wrap {
  height: auto;
  min-height: 60vh;
  margin: 24px auto 60px;
  width: min(1100px, 94%);
}
.info-wrap h2 {
  margin: 12px 0 8px;
  padding-left: 4px;
  color: #2c3e50;
}
.hint-list {
  margin: 8px 0 16px;
  padding-left: 18px;
  color: #4a5568;
}
.panel {
  border: 1px solid rgb(77, 180, 193);
  border-radius: 6px;
  padding: 12px 14px;
  margin: 18px 0;
  background: #fff;
}
.panel h3 {
  margin: 0 0 6px;
  color: #2c3e50;
}
.panel .flat {
  margin: 0;
  padding-left: 18px;
  color: #4a5568;
}

/* 套用 LogisticsList 的表格配色 */
:deep(table) {
  border-collapse: collapse;
  width: 100%;
  margin-top: 12px;
}
:deep(th),
:deep(td) {
  border: 1px solid rgb(77, 180, 193);
  padding: 0.5rem;
  text-align: center;
}
:deep(thead) {
  background: rgb(0, 112, 131);
  color: rgb(248, 249, 250);
}

/* 標題統一樣式，延續 logisticsList 色票 */
.page-header {
  width: min(1100px, 94%);
  margin: 12px auto 8px;
  padding: 10px 12px 8px;
  border-bottom: 2px solid rgb(77, 180, 193);
  display: flex;
  flex-direction: column;
  align-items: center; /* 整體置中 */
}
.page-header h1 {
  margin: 0;
  font-size: 22px;
  color: #2c3e50;
}
.subtitle {
  margin: 4px 0 0;
  color: #4a5568;
}
</style>
