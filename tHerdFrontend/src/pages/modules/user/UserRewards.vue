<!-- /src/pages/modules/user/UserRewards.vue -->
<template>
  <div class="container py-4 rewards-page">
    <!-- 麵包屑 -->
    <div class="breadcrumb">
      <router-link :to="{ name: 'userme' }">我的帳戶</router-link>
      <span>我的優惠券</span>
    </div>

    <div class="layout">
      <!-- 側欄 -->
      <aside class="sidebar">
        <MyAccountSidebar />
      </aside>

      <!-- 主要內容 -->
      <main class="content">
        <div class="header">
          <h2 class="title">我的優惠券</h2>

          <!-- 摘要 chips -->
          <div class="chips">
            <span class="chip">全部：{{ summary.total }}</span>
            <!-- <span class="chip">可用：{{ summary.usable }}</span> -->
            <span
              v-for="(count, k) in summary.byStatus"
              :key="k"
              class="chip"
            >
              {{ statusLabel(k) }}：{{ count }}
            </span>
          </div>
        </div>

        <!-- 篩選列 -->
        <el-card shadow="never" class="toolbar">
          <div class="toolbar-row">
            <el-select v-model="filters.status" placeholder="全部狀態" clearable @change="onFilterChange" style="width: 180px">
              <el-option label="全部" value="" />
              <el-option label="未使用" value="unuse" />
              <el-option label="已使用" value="used" />
              <el-option label="已過期" value="Expired" />
            </el-select>

            <el-switch
              v-model="filters.onlyUsable"
              :disabled="filters.status === 'Used' || filters.status === 'Expired'"
              @change="onFilterChange"
              active-text="只看可用"
            />

            <el-button :loading="loading" @click="refresh" class="ml-1">重新整理</el-button>
          </div>
        </el-card>

        <!-- 列表 -->
        <el-skeleton :loading="loading" animated :count="3">
          <template #template>
            <el-card class="mb-2" />
          </template>
          <template #default>
            <div v-if="rows.length === 0" class="empty">
              <i class="bi bi-ticket-perforated"></i>
              <div class="mt-1">目前沒有符合條件的優惠券</div>
              <router-link class="link" :to="{ name: 'userme' }">回到我的帳戶</router-link>
            </div>

            <div class="list">
              <TicketCoupon
                v-for="row in rows"
                :key="row.couponWalletId"
                :coupon="row.coupon"
              >
                <!-- 右側自訂動作（可選） -->
                <template #default>
                  <el-button
                    size="small"
                    type="primary"
                    :disabled="!row.isUsable"
                    @click="goUse(row)"
                  >
                    {{ row.isUsable ? '可使用' : '不可用' }}
                  </el-button>
                </template>
              </TicketCoupon>
            </div>

            <!-- 分頁 -->
            <div class="pager" v-if="total > 0">
              <el-pagination
                background
                layout="prev, pager, next, jumper"
                :page-size="pageSize"
                :current-page="page"
                :total="total"
                @current-change="onPageChange"
              />
            </div>
          </template>
        </el-skeleton>
      </main>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { http } from '@/api/http'
import MyAccountSidebar from '@/components/account/MyAccountSidebar.vue'
import TicketCoupon from '@/components/coupon/TicketCoupon.vue' // ← 路徑依你的實際存放調整

// 狀態→顯示文字
function statusLabel(s) {
  if (s === 'Active') return '有效'
  if (s === 'used') return '已使用'
  if (s === 'unuse') return '未使用'
  if (s === 'Expired') return '已過期'
  return s || '全部'
}

// 狀態/可用 篩選 + 分頁
const filters = reactive({
  status: '',        // '', 'Active', 'Used', 'Expired'
  onlyUsable: false, // 僅可用
})
const page = ref(1)
const pageSize = ref(10)
const total = ref(0)

const loading = ref(false)
const rows = ref([])      // 來自 /user/coupons/wallet 的 items
const summary = reactive({ // 來自 /user/coupons/wallet/summary
  total: 0,
  usable: 0,
  byStatus: {} // { Active: n, Used: n, ... }
})

async function loadSummary() {
  try {
    const { data } = await http.get('/user/coupons/wallet/summary')
    summary.total = data.total ?? 0
    summary.usable = data.usable ?? 0
    summary.byStatus = data.byStatus ?? {}
  } catch (err) {
    // 摘要失敗不擋流程
    console.warn('[UserRewards] summary fail', err)
  }
}

async function loadWallet() {
  loading.value = true
  try {
    const params = new URLSearchParams()
    if (filters.status) params.set('status', filters.status)
    if (filters.onlyUsable) params.set('onlyUsable', 'true')
    params.set('page', String(page.value))
    params.set('pageSize', String(pageSize.value))

    const { data } = await http.get(`/user/coupons/wallet?${params.toString()}`)

    // 預期回傳：{ items: [], total: number, page, pageSize }
    rows.value = data.items ?? []
    total.value = data.total ?? 0
  } catch (err) {
    ElMessage.error(err?.response?.data?.error || '載入優惠券失敗')
    console.error('[UserRewards] load', err)
  } finally {
    loading.value = false
  }
}

function onFilterChange() {
  // 切換篩選時回到第 1 頁
  page.value = 1
  loadWallet()
  loadSummary() // 同步更新摘要
}

function onPageChange(p) {
  page.value = p
  loadWallet()
}

function refresh() {
  loadWallet()
  loadSummary()
}

function goUse(row) {
  // 這裡視你的導購策略而定：
  // ex: 導向商品清單或結帳頁，並把 couponCode 帶入 query
  const code = row?.coupon?.couponCode
  if (!row.isUsable) return
  if (code) {
    ElMessage.success(`已套用優惠碼：${code}（示意）`)
    // router.push({ name: 'shop', query: { coupon: code } })
  }
}

onMounted(async () => {
  await Promise.all([loadSummary(), loadWallet()])
})
</script>

<style scoped>
.rewards-page { max-width: 1200px; }
.breadcrumb { display:flex; gap:8px; color:#666; font-size:14px; margin-bottom:12px; }
.breadcrumb a { color:#4183c4; }

.layout { display:grid; grid-template-columns: 300px 1fr; gap:20px; }
.sidebar { min-width:0; }
.content { min-width:0; }

.title { font-size:22px; font-weight:700; }

.header { display:flex; align-items:center; justify-content:space-between; margin-bottom:12px; gap:12px; flex-wrap:wrap; }
.chips { display:flex; flex-wrap:wrap; gap:8px; }
.chip { background:#f4f6f8; color:#333; border-radius:999px; padding:4px 10px; font-size:12px; }

.toolbar { margin-bottom:12px; }
.toolbar-row { display:flex; align-items:center; gap:12px; flex-wrap:wrap; }
.ml-1 { margin-left: 6px; }

.list { display:flex; flex-direction:column; gap:12px; }
.mb-2 { margin-bottom: 12px; }

.pager { display:flex; justify-content:center; margin-top:16px; }

.empty { padding:40px 16px; text-align:center; color:#666; }
.empty .link { display:inline-block; margin-top:6px; color:#4183c4; }

@media (max-width: 992px) {
  .layout { grid-template-columns: 1fr; }
}
</style>
