<template>
  <div class="p-4 space-y-4">
    <!-- 🔍 搜尋與條件 -->
    <el-card>
      <div class="flex flex-wrap items-end gap-3">
        <el-input
          v-model="q.keyword"
          placeholder="關鍵字（例如：小麥、牛肉、豆腐）"
          clearable
          class="w-80"
        />

        <el-input
          v-model.number="q.categoryId"
          placeholder="分類ID（可留空）"
          clearable
          class="w-40"
        />

        <el-select v-model="q.sort" class="w-56" placeholder="排序方式">
          <el-option label="名稱 A → Z" value="name" />
          <el-option label="最新資料" value="newest" />
          <el-option label="依分類" value="category" />
          <el-option label="熱門（保留）" value="popular" />
          <!-- Example: Special nutrient sorting -->
          <el-option label="依營養成分 (α-維生素E)" value="nutrient:1105" />
        </el-select>

        <el-button type="primary" :loading="loading" @click="fetchData">
          搜尋
        </el-button>

        <div class="ml-auto text-sm text-gray-500">
          共 {{ total }} 筆
        </div>
      </div>
    </el-card>

    <!-- 📋 資料表格 -->
    <el-card>
      <el-table
        :data="items"
        border
        stripe
        height="60vh"
        @row-click="toDetail"
      >
        <el-table-column prop="sampleId" label="ID" width="90" />
        <el-table-column prop="sampleName" label="食材名稱" min-width="200" />
        <el-table-column prop="categoryName" label="分類" width="180" />
        <el-table-column prop="aliasName" label="別名" min-width="200" />

        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row }">
            <el-button
              size="small"
              type="primary"
              @click.stop="toDetail(row)"
            >
              查看
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 📄 分頁 -->
      <div class="flex justify-end mt-4">
        <el-pagination
          background
          layout="prev, pager, next, jumper, ->, sizes, total"
          :total="total"
          :current-page="q.page"
          :page-sizes="[10, 12, 20, 30, 50]"
          :page-size="q.pageSize"
          @current-change="(p) => { q.page = p; fetchData() }"
          @size-change="(s) => { q.pageSize = s; q.page = 1; fetchData() }"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { getNutritionList } from './api/cntService'

const router = useRouter()

// 🔍 查詢參數
const q = ref({
  keyword: '',
  categoryId: null,
  sort: 'name',
  page: 1,
  pageSize: 12
})

// 📦 資料與狀態
const items = ref([])
const total = ref(0)
const loading = ref(false)

// 📡 拉資料
async function fetchData() {
  loading.value = true
  try {
    const { items: list, total: tt } = await getNutritionList(q.value)
    items.value = list ?? []
    total.value = tt ?? 0
  } finally {
    loading.value = false
  }
}

// 🔁 跳轉詳細頁
function toDetail(row) {
  const id = row?.sampleId ?? row
  router.push({ name: 'cnt-nutrition-detail', params: { id } })
}

// 🚀 初次載入
onMounted(fetchData)
</script>

<style scoped>
.p-4 { padding: 1rem; }
.w-80 { width: 20rem; }
.w-56 { width: 14rem; }
.w-40 { width: 10rem; }
.space-y-4 > * + * { margin-top: 1rem; }
</style>
