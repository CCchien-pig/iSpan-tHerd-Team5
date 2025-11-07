<!--
  ProducMainSearch.vue - 產品列表查詢
  功能：展示產品列表，包含標題、查看全部按鈕和產品卡片網格
  特色：響應式網格布局、事件傳遞、可配置標題
  用途：用於首頁、產品頁面等需要展示多個產品的區域
-->
<template>
  <div class="container py-5">
    <!-- 結果統計列 -->
    <div class="d-flex justify-content-between align-items-center mb-3 flex-wrap">
      <div class="text-muted small">
        共 {{ totalCount }} 項結果中的第 {{ startIndex }}–{{ endIndex }} 項
        <template v-if="keyword">：「<strong>{{ keyword }}</strong>」</template>
        <!-- <template v-else-if="productTypeName">：分類：<strong>{{ productTypeName }}</strong></template> -->
        <template v-else></template>
      </div>

      <div class="d-flex align-items-center mt-2 mt-md-0">
        <label class="me-2 text-muted small">排序方式</label>
        <select
          v-model="sortBy"
          class="form-select form-select-sm"
          style="width: auto"
          @change="reloadProducts"
        >
          <option value="relevance">相關性</option>
          <option value="price-asc">價格：低 → 高</option>
          <option value="price-desc">價格：高 → 低</option>
          <option value="newest">最新上架</option>
        </select>
      </div>
    </div>

      <!-- 🧩 商品列表 : 查詢結果 -->
    <ProductList
      :key="pageIndex + '_' + (keyword || productTypeId || 'all')"
      :title="'搜尋結果'"
      :products="products"
      :total-count="totalCount"
      :page-size="pageSize"
      :page-index="pageIndex"
      @page-change="page => searchProducts(page)"
      @add-to-cart="addToCart"
    />
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useLoading } from "@/composables/useLoading"
import ProductsApi from "@/api/modules/prod/ProductsApi"
import ProductList from "@/components/modules/prod/list/ProductList.vue"

const route = useRoute()
const router = useRouter()
const { showLoading, hideLoading } = useLoading()

// ===== 狀態 =====
const keyword = ref("")
const productTypeId = ref(null)
const productTypeCode = ref("")
const productTypeName = ref("")
const products = ref([])
const totalCount = ref(0)
const pageIndex = ref(1)
const pageSize = ref(40)
const sortBy = ref("relevance")
const isLoading = ref(false)
const errorMessage = ref("")

// ===== 顯示範圍 =====
const startIndex = computed(() =>
  totalCount.value === 0 ? 0 : (pageIndex.value - 1) * pageSize.value + 1
)
const endIndex = computed(() =>
  Math.min(pageIndex.value * pageSize.value, totalCount.value)
)

// ===== 解析 slug =====
function parseSlug() {
  const slug = route.params.slug || ""
  const parts = slug.split("-")
  productTypeId.value = Number(parts.pop()) || null
  productTypeCode.value = parts.join("-") || ""
}

// ===== 查詢商品 =====
async function searchProducts(page = 1) {
  console.log('🟡 route.query:', route.query)
  try {
    isLoading.value = true
    errorMessage.value = ""
    showLoading("載入商品中...")

    parseSlug()
    keyword.value = (route.query.q ?? "").toString().trim()

    // 清空舊資料，避免殘影
    products.value = []
    totalCount.value = 0

    const badgeQuery = (route.query.badge ?? "").toString().trim()
    const otherQuery = (route.query.other ?? "").toString().trim()

    const query = {
      pageIndex: page,
      pageSize: pageSize.value,
      sortBy: sortBy.value,
      isPublished: true,
      isFrontEnd: true,
      badge: badgeQuery,
      other: otherQuery,
    }

    if (keyword.value) query.keyword = keyword.value
    if (productTypeId.value) query.productTypeId = productTypeId.value

    const res = await ProductsApi.getProductList(query)
    const data = res.data || {}
    products.value = Array.isArray(data.items) ? data.items : []
    totalCount.value = data.totalCount || 0
    pageIndex.value = data.pageIndex || 1
    productTypeName.value =
      data.productTypeName ||
      productTypeCode.value?.toUpperCase() ||
      ""

    // UX：滾動到頂部
    window.scrollTo({ top: 0, behavior: "smooth" })
  } catch (err) {
    console.error("❌ 搜尋商品錯誤：", err)
    errorMessage.value = "無法載入商品資料，請稍後再試。"
  } finally {
    isLoading.value = false
    hideLoading()
  }
}

// ===== 排序變更 =====
function reloadProducts() {
  searchProducts(pageIndex.value)
}

// ===== 監聽路由變化 =====
watch(
  () => route.query,
  () => searchProducts(1),
  { deep: true, immediate: true }
)
</script>