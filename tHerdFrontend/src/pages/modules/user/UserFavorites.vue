<template>
  <div class="container py-4 user-favorites-page">
    <!-- 麵包屑 -->
    <div class="breadcrumb">
      <router-link :to="{ name: 'userme' }">我的帳戶</router-link>
      <span>我的最愛</span>
    </div>

    <div v-if="!isLoggedIn" class="alert alert-warning">請先登入後查看您的願望清單</div>

    <div v-else class="layout">
      <!-- 左側側欄 -->
      <aside class="sidebar">
        <MyAccountSidebar />
      </aside>

      <!-- 右側主內容 -->
      <main class="content">
        <div class="header">
          <h2 class="title">已收藏的商品</h2>
          <div class="tools">
            <el-input
              v-model="q"
              placeholder="輸入商品名稱搜尋…"
              clearable
              class="w-64"
              @clear="handleSearch"
              @keyup.enter="handleSearch"
            >
              <template #append>
                <el-button :loading="loading" @click="handleSearch">
                  <i class="bi bi-search"></i>
                </el-button>
              </template>
            </el-input>

            <el-select v-model="pageSize" class="ms-2 w-28" @change="handlePageSize">
              <el-option v-for="s in pageSizeOptions" :key="s" :label="s + ' /頁'" :value="s" />
            </el-select>

            <el-button class="ms-2" :loading="loading" @click="refresh">
              <i class="bi bi-arrow-clockwise me-1"></i> 重新整理
            </el-button>
          </div>
        </div>

        <el-card shadow="never" class="card">
          <!-- 載入狀態 -->
          <div v-if="loading" class="mt-2">
            <el-skeleton animated :rows="5" />
          </div>

          <!-- 錯誤 -->
          <el-alert
            v-else-if="errorMsg"
            :title="errorMsg"
            type="error"
            show-icon
            :closable="false"
          />

          <!-- 空清單 -->
          <div v-else-if="filteredItems.length === 0" class="empty-list">
            <i class="bi bi-emoji-neutral"></i>
            <div class="hint">沒有符合條件的收藏商品</div>
          </div>

          <!-- 表格 -->
          <el-table
            v-else
            :data="pagedItems"
            stripe
            highlight-current-row
            class="fav-table"
          >
            <el-table-column label="" width="10">
              <template #default="{ row }">
                <span class="dot" :class="{ off: !row.isPublished }"></span>
              </template>
            </el-table-column>

            <el-table-column prop="productName" label="商品名稱" min-width="240">
              <template #default="{ row }">
                <a class="link" @click.prevent="goProduct(row.productId)">{{ row.productName }}</a>
                <el-tag v-if="row.badge" class="ms-2" type="success" size="small" effect="light">{{ row.badge }}</el-tag>
              </template>
            </el-table-column>

            <el-table-column prop="isPublished" label="上架" width="90" align="center">
              <template #default="{ row }">
                <el-tag size="small" :type="row.isPublished ? 'success' : 'info'">
                  {{ row.isPublished ? '已上架' : '未上架' }}
                </el-tag>
              </template>
            </el-table-column>

            <el-table-column prop="createdDate" label="收藏日期" width="160" align="center">
              <template #default="{ row }">
                {{ formatDate(row.createdDate) }}
              </template>
            </el-table-column>

            <el-table-column label="動作" width="160" align="center">
              <template #default="{ row }">
                <el-button size="small" type="primary" plain @click="goProduct(row.productId)">
                  前往商品
                </el-button>
                <el-button size="small" type="danger" plain :loading="removingId === row.productId" @click="removeOne(row.productId)">
                  移除
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <!-- 分頁 -->
          <div v-if="filteredItems.length > 0" class="pager">
            <el-pagination
              background
              layout="prev, pager, next"
              :current-page="page"
              :page-size="pageSize"
              :total="filteredTotal"
              @current-change="handlePage"
            />
          </div>
        </el-card>
      </main>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { http } from '@/api/http'
import { useAuthStore } from '@/stores/auth'
import MyAccountSidebar from '@/components/account/MyAccountSidebar.vue'

const router = useRouter()
const auth = useAuthStore()

const isLoggedIn = computed(() => !!auth?.user)

// UI 狀態
const loading = ref(false)
const removingId = ref(0)
const errorMsg = ref('')

// 分頁/查詢
const items = ref([])      // 伺服器回來的當頁資料
const total = ref(0)       // 伺服器記錄的總數（未過濾）
const page = ref(1)
const pageSize = ref(10)
const pageSizeOptions = [10, 20, 50]
const q = ref('')          // 查詢字串（目前前端過濾；若未來 API 提供 ?q= 可直接串上去）

// 輔助
function formatDate(v) {
  try {
    const d = new Date(v)
    const y = d.getFullYear()
    const m = String(d.getMonth() + 1).padStart(2, '0')
    const day = String(d.getDate()).padStart(2, '0')
    return `${y}/${m}/${day}`
  } catch {
    return ''
  }
}

// 讀列表（從後端）
async function fetchFavorites() {
  loading.value = true
  errorMsg.value = ''
  try {
    // const { data } = await http.get(`/user/favorites?page=${page.value}&pageSize=${pageSize.value}`)
    const { data } = await http.get(`/user/favorites`, {
   params: { page: page.value, pageSize: pageSize.value, q: (q.value || '').trim() || undefined }})
    // 後端回 { items, total, page, pageSize }
    items.value = Array.isArray(data?.items) ? data.items : []
    total.value = Number(data?.total ?? 0)
  } catch (err) {
    if (err?.response?.status === 401) {
      errorMsg.value = '請先登入'
    } else {
      errorMsg.value = err?.response?.data?.error || '載入失敗，請稍後再試'
    }
    items.value = []
    total.value = 0
  } finally {
    loading.value = false
  }
}

// 重新整理
async function refresh() {
  await fetchFavorites()
}

// 搜尋（前端過濾）
function handleSearch() {
  // 搜尋時回到第一頁（在前端分頁中處理）
  page.value = 1
}

// 調整每頁筆數
function handlePageSize() {
  page.value = 1
  fetchFavorites()
}

// 換頁
function handlePage(p) {
  page.value = p
  // 注意：因為我們還有前端過濾，如果希望過濾在伺服器端，建議後端支援 ?q=
  // 目前維持後端分頁 + 前端搜尋的混合：仍抓該頁，再在前端過濾顯示
  fetchFavorites()
}

// 前往商品詳情
function goProduct(productId) {
  if (router.hasRoute('product-detail')) {
    router.push({ name: 'product-detail', params: { id: productId } })
  } else {
    ElMessage.info('尚未設定商品詳情路由')
  }
}

// 移除一筆最愛
async function removeOne(productId) {
  try {
    await ElMessageBox.confirm('確定要從願望清單中移除此商品嗎？', '移除最愛', {
      confirmButtonText: '移除',
      cancelButtonText: '取消',
      type: 'warning',
      autofocus: false,
      confirmButtonClass: 'el-button--danger',
    })
  } catch {
    return
  }

  removingId.value = productId
  try {
    await http.delete(`/user/favorites/${productId}`)
    // 從當前資料移除，或重新載入
    const idx = items.value.findIndex(x => x.productId === productId)
    if (idx >= 0) items.value.splice(idx, 1)

    ElMessage.success('已移除')
    // 通知側欄刷新徽章
    window.dispatchEvent(new CustomEvent('favorite-changed'))

    // 若刪到當頁無資料且頁碼 > 1，自動往前一頁
    await nextTick()
    if (items.value.length === 0 && page.value > 1) {
      page.value = page.value - 1
      await fetchFavorites()
    }
  } catch (err) {
    const msg = err?.response?.data?.error || '移除失敗'
    ElMessage.error(msg)
  } finally {
    removingId.value = 0
  }
}

/** 前端過濾結果（目前只對 productName 做包含式搜尋） */
const filteredItems = computed(() => {
  const s = (q.value || '').trim().toLowerCase()
  if (!s) return items.value
  return items.value.filter(x => (x.productName || '').toLowerCase().includes(s))
})

/**
 * 因為我們同時有「後端分頁」和「前端搜尋」，為避免複雜度，
 * 這裡讓搜尋僅影響「當頁」上的資料（filteredItems），分頁仍用後端。
 * 若你要改成「完全由後端搜尋 + 分頁」，建議讓 API 支援 ?q= 並在 fetchFavorites() 帶上。
 */
const filteredTotal = computed(() => filteredItems.value.length)
const pagedItems = computed(() => filteredItems.value) // 目前 filtered 就是當頁資料

// 首次載入 & 監聽全域事件（商品頁加/移最愛後）
function onFavChanged() {
  fetchFavorites()
}

onMounted(() => {
  if (isLoggedIn.value) {
    fetchFavorites()
  }
  window.addEventListener('favorite-changed', onFavChanged)
})

onBeforeUnmount(() => {
  window.removeEventListener('favorite-changed', onFavChanged)
})
</script>

<style scoped>
/* .user-favorites-page .layout {
  display: grid;
  grid-template-columns: 260px 1fr;
  gap: 16px;
}
@media (max-width: 992px) {
  .user-favorites-page .layout {
    grid-template-columns: 1fr;
  }
}
.sidebar {
  min-width: 0;
}
.content {
  min-width: 0;
}
.header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
  flex-wrap: wrap;
  gap: 8px;
}
.title {
  margin: 0;
  font-size: 18px;
  font-weight: 700;
}
.tools {
  display: flex;
  align-items: center;
  gap: 8px;
}
.w-64 { width: 16rem; }
.w-28 { width: 7rem; }
.empty-list {
  padding: 28px 0;
  color: #909399;
  display: flex;
  align-items: center;
  gap: 8px;
  justify-content: center;
}
.fav-table .dot {
  display: inline-block;
  width: 8px;
  height: 8px;
  border-radius: 999px;
  background: #f56c6c;
}
.fav-table .dot.off { background: #c0c4cc; }
.link { cursor: pointer; color: #409eff; }
.link:hover { text-decoration: underline; }
.pager {
  display: flex;
  justify-content: center;
  margin-top: 14px;
}
.breadcrumb {
  display: flex;
  align-items: center;
  gap: 6px;
  color: #909399;
  margin-bottom: 8px;
}
.breadcrumb a { color: inherit; }
.breadcrumb span::before { content: '>'; margin: 0 6px; color: #c0c4cc; } */
/* ===== 外層容器與麵包屑：對齊 UserMe（寬 1200、breadcrumb 位移） ===== */
.user-favorites-page { max-width: 1200px; }
.breadcrumb {
  display:flex; gap:8px; color:#666; font-size:14px; margin-bottom:12px;
  transform: translateX(100px);
}
.breadcrumb a { color:#4183c4; }

/* ===== 版面：與 UserMe 相同（300px + 1fr，gap 20）＋ 固定高度提供區域滾動 ===== */
.layout {
  display: grid;
  grid-template-columns: 300px 1fr;
  gap: 20px;
  height: calc(100vh - 160px);  /* 固定高度容器，內容才滾得起來 */
}

/* ===== Sidebar：與 UserMe 相同（右移 2/3 gap）、等高 ===== */
.sidebar {
  min-width: 0;
  min-height: 0;               /* 讓 Grid 子項可縮，避免擠掉滾動 */
  display: flex;               /* 讓內部 .myaccount-sidebar 吃滿高度 */
  transform: translateX(100px);
}
.sidebar :deep(.myaccount-sidebar) {
  height: 100%;
}

/* ===== 右側內容：區域滾動（只滾主內容，不滾整頁） ===== */
.content {
  min-width: 0;
  min-height: 0;               /* Grid 子項滾動關鍵 */
  height: 100%;
  overflow: auto;
  padding-right: 4px;          /* 捲軸避免壓文字 */
  max-height: 600px; 
}

/* ===== 標題與工具列：字級與色票一致 ===== */
.title {
  margin: 0 0 12px 0;
  font-size: 22px;
  font-weight: 700;
  color: #2c3e50;
}
.header {
  display:flex; align-items:center; justify-content:space-between;
  margin-bottom: 12px; flex-wrap: wrap; gap: 8px;
}
.tools { display:flex; align-items:center; gap:8px; }
.w-64 { width: 16rem; }
.w-28 { width: 7rem; }

/* ===== 卡片：沿用青綠邊線樣式（和 UserMe 同色） ===== */
.card {
  border: 1px solid rgb(77, 180, 193);
  border-radius: 6px;
  background: #fff;
}

/* ===== 表格內元素顏色／連結樣式 ===== */
.fav-table .dot {
  display:inline-block; width:8px; height:8px; border-radius:999px; background:#f56c6c;
}
.fav-table .dot.off { background:#c0c4cc; }

.link { cursor:pointer; color:#4183c4; }
.link:hover { text-decoration: underline; }

.empty-list {
  padding: 28px 0;
  color: #4a5568;
  display:flex; align-items:center; gap:8px; justify-content:center;
}

/* 分頁區塊 */
.pager { display:flex; justify-content:center; margin-top:14px; }

/* ===== RWD：手機改單欄，移除位移與區域滾動（與 UserMe 一致） ===== */
@media (max-width: 992px) {
  .layout { grid-template-columns: 1fr; gap: 16px; height: auto; }
  .sidebar { transform: none; }
  .content { height: auto; overflow: visible; }
}
</style>
