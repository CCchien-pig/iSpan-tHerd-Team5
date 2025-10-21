<!-- src/pages/modules/cnt/NutritionList.vue -->
<template>
  <div ref="pageRef" class="nutrition-page">
    <!-- 🔍 搜尋列（卡片化 + 緊湊 + 主題按鈕） -->
    <el-card class="search-card">
      <div class="search-row">
        <!-- 關鍵字 -->
        <el-input
          v-model="q.keyword"
          placeholder="輸入關鍵字（例如：小麥、牛肉、豆腐）"
          clearable
          class="w-80"
          @keyup.enter.native="applySearch"
          @clear="onKeywordCleared"
        />

        <!-- 分類：下拉（顯示中文；查詢傳 ID） -->
        <el-select
          v-model="q.categoryId"
          class="w-56"
          clearable
          filterable
          placeholder="選擇分類（可輸入關鍵字過濾）"
          @change="applySearch"
          @clear="onCategoryCleared"
        >
          <el-option label="全部分類" :value="null" />
          <el-option
            v-for="c in categories"
            :key="c.id"
            :label="c.name"
            :value="c.id"
          />
        </el-select>

        <!-- 排序（維持你後端的 name/newest/category/popular 等） -->
        <el-select v-model="q.sort" class="w-56" placeholder="排序方式" @change="applySearch">
          <el-option label="名稱字串排序" value="name" />
          <el-option label="最新資料" value="newest" />
          <el-option label="依分類" value="category" />
          <el-option label="熱門（保留）" value="popular" />
          <el-option label="依營養素 (α-維生素E)" value="nutrient:1105" />
        </el-select>

        <!-- 搜尋按鈕：主色 + 白字 + hover glow -->
        <el-button
          :loading="loading"
          class="btn-strong teal-reflect-button btn-wide"
          @click="applySearch"
          title="執行查詢"
        >
          搜尋
        </el-button>

        <div class="ml-auto total-hint">
          共 <b>{{ total }}</b> 筆
        </div>
      </div>
    </el-card>

    <!-- 📋 表格（保留 Element 行為；視覺主題化） -->
    <el-card class="table-card">
      <el-table
        :data="items"
        stripe
        border
        height="60vh"
        class="brand-table"
        @row-click="toDetail"
      >
        <el-table-column prop="sampleId" label="ID" width="90" />
        <el-table-column prop="sampleName" label="食材名稱" min-width="220" />
        <el-table-column prop="categoryName" label="分類" width="180" />
        <el-table-column prop="aliasName" label="別名" min-width="220" />

        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button
              size="small"
              class="btn-strong teal-reflect-button btn-compact"
              @click.stop="toDetail(row)"
              title="查看詳情"
            >
              查看
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 📄 分頁（預設 24；使用者可切換） -->
      <div class="pager-wrap">
        <el-pagination
          background
          class="brand-pagination"
          layout="prev, pager, next, jumper, ->, sizes, total"
          :total="total"
          :current-page="q.page"
          :page-sizes="[12, 24, 48, 100]"
          :page-size="q.pageSize"
          @current-change="(p) => { q.page = p; fetchData() }"
          @size-change="(s) => { q.pageSize = s; q.page = 1; fetchData() }"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getNutritionList, getFoodCategories } from './api/cntService'

const router = useRouter()
const route  = useRoute()

// 分類列表（for 下拉）
const categories = ref([])

// 🔎 查詢參數（預設 24 筆）
const q = ref({
  keyword: '',
  categoryId: null, // 用中文選單，但實際傳 ID 給後端
  sort: 'name',
  page: 1,
  pageSize: 24
})

// 📦 狀態
const items   = ref([])
const total   = ref(0)
const loading = ref(false)

// 🧭 首頁 scroll 支援：?scroll=nutrition | list → 進頁即捲動至此模組
const pageRef = ref(null)
async function ensureScrollIfNeeded() {
  const s = String(route.query.scroll || '').toLowerCase()
  if (s === 'nutrition' || s === 'list') {
    await nextTick()
    pageRef.value?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }
}

// 📡 讀取列表
async function fetchData() {
  loading.value = true
  try {
    const { items: list, total: tt } = await getNutritionList(q.value)
    items.value = Array.isArray(list) ? list : []
    total.value = Number(tt || 0)
  } catch (err) {
    console.warn('[NutritionList] 讀取失敗：', err)
    items.value = []
    total.value = 0
  } finally {
    loading.value = false
  }
}

// 📥 讀取分類（下拉）
async function loadCategories() {
  try {
    const data = await getFoodCategories()
    categories.value = Array.isArray(data) ? data : []
  } catch (e) {
    console.warn('[NutritionList] 讀取分類失敗：', e)
    categories.value = []
  }
}

// 🔎 立即搜尋：重置到第 1 頁
function applySearch() {
  q.value.page = 1
  fetchData()
}

// 清除 keyword/分類時也要重查，避免 UI 看起來沒反應
function onKeywordCleared() {
  applySearch()
}
function onCategoryCleared() {
  q.value.categoryId = null
  applySearch()
}

// 🔁 詳細頁導向
function toDetail(row) {
  const id = row?.sampleId ?? row
  router.push({ name: 'cnt-nutrition-detail', params: { id } })
}

// 🚀 初次載入
onMounted(async () => {
  await loadCategories()
  await fetchData()
  await ensureScrollIfNeeded()
})
</script>

<style scoped>
/* ======================
   版面 & 卡片化
   ====================== */
.nutrition-page {
  max-width: 1080px;
  margin: 0 auto;
  padding: 1rem;
}

.search-card,
.table-card {
  border-radius: 14px;
  border: 1px solid #e9eef1;
  box-shadow: 0 10px 22px rgba(0,0,0,.06), 0 2px 8px rgba(0,0,0,.04);
  overflow: hidden;
}
.search-card {
  background: #f8f9fa;
  margin-bottom: 1rem;
}
.table-card {
  background: #fff;
}

/* ======================
   搜尋列（緊湊 + 主題按鈕）
   ====================== */
.search-row {
  display: flex;
  flex-wrap: wrap;
  align-items: end;
  gap: .75rem;
}
.total-hint {
  color: #6b7280;
  font-size: .95rem;
}
.w-80 { width: 20rem; min-width: 16rem; }
.w-56 { width: 14rem; min-width: 12rem; }
.ml-auto { margin-left: auto; }

/* 主題按鈕（白字 + 亮度 + hover glow）
   修正：height 與 line-height 要一致，避免不同瀏覽器造成垂直位移 */
.btn-strong {
  color: #fff !important;
  font-weight: 600;
  border: none;
  padding: 0 16px;
  height: 40px;               /* ✅ 修正：40 / 40 一致 */
  line-height: 40px;          /* ✅ */
  background: linear-gradient(180deg, rgba(0,112,131,1) 0%, rgba(0,112,131,.88) 100%);
  box-shadow: 0 8px 18px rgba(0,112,131,.28), inset 0 0 0 1px rgba(255,255,255,.12);
  transition: transform .08s ease, box-shadow .18s ease, filter .18s ease;
}
.btn-strong:hover {
  filter: saturate(1.05) brightness(1.02);
  box-shadow: 0 10px 22px rgba(0,112,131,.34), 0 2px 8px rgba(0,0,0,.06);
  transform: translateY(-1px);
}
.btn-wide { padding: 0 30px; font-size: 16px; }
.btn-compact { padding: 0 12px; height: 32px; line-height: 32px; font-size: 14px; }

/* ======================
   表格（品牌主題）
   ====================== */
:deep(.brand-table .el-table__header th) {
  background: rgb(0,112,131);  /* 主色：深藍綠（PPT主題） */
  color: #fff;
  font-weight: 700;
  letter-spacing: .3px;
}
:deep(.brand-table .el-table__row) {
  transition: background-color .18s ease, box-shadow .18s ease;
}
:deep(.brand-table .el-table__row:hover) {
  background-color: #f5fbfb;
}
:deep(.brand-table) {
  border-radius: 12px;
  overflow: hidden;
}

/* ======================
   分頁（深綠膠囊主題）
   ====================== */
.pager-wrap {
  display: flex;
  justify-content: end;
  padding-top: .75rem;
}

:deep(.brand-pagination .el-pagination.is-background .btn-prev),
:deep(.brand-pagination .el-pagination.is-background .btn-next),
:deep(.brand-pagination .el-pagination.is-background .el-pager li) {
  border-radius: 999px;
  margin: 0 2px;
  min-width: 36px;
  height: 36px;
  line-height: 36px;
}

/* hover / active → 主色 + 白字 + glow */
:deep(.brand-pagination .el-pagination.is-background .el-pager li:hover),
:deep(.brand-pagination .el-pagination.is-background .el-pager li.is-active) {
  background: linear-gradient(180deg, rgba(0,112,131,.96) 0%, rgba(0,112,131,.86) 100%);
  color: #fff;
  box-shadow: 0 6px 14px rgba(0,112,131,.25);
}

/* sizes / jumper：白底 + 綠描邊 + hover 輕亮 */
:deep(.brand-pagination .el-pagination__jump),
:deep(.brand-pagination .el-pagination__sizes .el-input .el-input__wrapper) {
  border-radius: 999px;
}
:deep(.brand-pagination .el-pagination__sizes .el-input .el-input__wrapper) {
  border: 1px solid rgba(0,112,131,.28);
  box-shadow: none;
}
:deep(.brand-pagination .el-pagination__sizes .el-input.is-focus .el-input__wrapper),
:deep(.brand-pagination .el-pagination__sizes .el-input .el-input__wrapper:hover) {
  border-color: rgba(0,112,131,.6);
}

/* ======================
   RWD
   ====================== */
@media (max-width: 1024px) {
  .w-80 { width: 100%; }
  .w-56 { width: 100%; }
  .ml-auto { margin-left: 0; }
  .total-hint { width: 100%; text-align: right; }
}
</style>
