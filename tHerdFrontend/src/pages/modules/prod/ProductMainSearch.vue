<!--
  ProductMainSearch.vue - 商品主查詢頁
  功能：展示商品列表、分頁、排序與搜尋關鍵字結果
-->
<template>
  <div class="container py-5">
    <!-- 🧮 統計與排序列 -->
    <div class="d-flex justify-content-between align-items-center mb-3 flex-wrap">
      <div class="text-muted small">
        共 {{ totalCount }} 項結果中的第 {{ startIndex }}–{{ endIndex }} 項：
        <template v-if="keyword">「<strong>{{ keyword }}</strong>」</template>
        <template v-else-if="productTypeName">分類：<strong>{{ productTypeName }}</strong></template>
        <template v-else>全部商品</template>
      </div>

      <!-- 🔽 排序下拉 -->
      <div class="d-flex align-items-center mt-2 mt-md-0">
        <label class="me-2 text-muted small">排序方式</label>
        <select
          v-model="sortOption"
          class="form-select form-select-sm"
          style="width: auto"
          @change="handleSortChange"
        >
          <option value="relevance-asc">相關性</option>
          <option value="price-asc">價格：低 → 高</option>
          <option value="price-desc">價格：高 → 低</option>
          <option value="newest-desc">最新上架</option>
        </select>
      </div>
    </div>

    <!-- 🧩 商品列表 -->
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

    <!-- ⚠️ 錯誤提示 -->
    <div v-if="errorMessage" class="alert alert-danger text-center mt-4">
      {{ errorMessage }}
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from "vue"
import { useRoute } from "vue-router"
import { useLoading } from "@/composables/useLoading"
import ProductsApi from "@/api/modules/prod/ProductsApi"
import ProductList from "@/components/modules/prod/list/ProductList.vue"

const route = useRoute()
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
const sortOption = ref("relevance-asc") // 🔹 變更為單一組合欄位
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
  try {
    isLoading.value = true
    errorMessage.value = ""
    showLoading("載入商品中...")

    parseSlug()
    keyword.value = (route.query.q ?? "").toString().trim()

    // 清空舊資料，避免殘影
    products.value = []
    totalCount.value = 0

    // 🔹 拆解排序字串
    const [sortByValue, sortDirection] = sortOption.value.split("-")
    const sortDesc = sortDirection === "desc"

    const query = {
      pageIndex: page,
      pageSize: pageSize.value,
      keyword: keyword.value || "",
      productTypeId: productTypeId.value || 0,
      isPublished: true,
      isFrontEnd: true,
      sortBy: sortByValue,  // ✅ e.g. "price"
      sortDesc: sortDesc,   // ✅ true / false
    }

    const res = await ProductsApi.getProductList(query)
    const data = res.data || {}

    products.value = Array.isArray(data.items) ? data.items : []
    totalCount.value = data.totalCount || 0
    pageIndex.value = data.pageIndex || 1
    productTypeName.value =
      data.productTypeName || productTypeCode.value?.toUpperCase() || "未分類"

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

// ===== 排序變更（自動回第 1 頁）=====
function handleSortChange() {
  pageIndex.value = 1
  searchProducts(1)
}

// ===== 監聽路由變化 =====
watch(
  () => route.fullPath,
  () => searchProducts(1),
  { immediate: true }
)

// ===== 範例：加入購物車 (可依需求接後端 API) =====
function addToCart(product) {
  console.log("🛒 加入購物車：", product)
}
</script>

<style scoped>
select.form-select-sm {
  min-width: 160px;
}
</style>
