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
        共 {{ totalCount }} 項結果中的第 {{ startIndex }}–{{ endIndex }} 項：
        <template v-if="keyword">「<strong>{{ keyword }}</strong>」</template>
        <template v-else-if="productTypeName">分類：<strong>{{ productTypeName }}</strong></template>
        <template v-else>全部商品</template>
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
import { ref, computed, onMounted, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useLoading } from "@/composables/useLoading";
import ProductsApi from "@/api/modules/prod/ProductsApi";
import ProductList from '@/components/modules/prod/list/ProductList.vue';

const route = useRoute()
const router = useRouter()

// ---- 查詢參數（關鍵字 / 分類）----
const { showLoading, hideLoading } = useLoading()
const error = ref(null)

// 若是從 SEO 友善路徑進來（例如 /products/beauty/xzq-1415）
const slug = computed(() => route.params.slug || '')   // 例如 "beauty-1410"
const productTypeCode = ref('')
const productTypeId = ref(null)
const keyword = ref(route.query.q || '')

function parseSlug() {
  if (!slug.value) return
  const parts = slug.value.split('-')
  productTypeId.value = parts.pop()              // 最後一段是 ID
  productTypeCode.value = parts.join('-') || ''  // 前面的是英文代碼
}

const products = ref([])
const totalCount = ref(0)
const pageIndex = ref(1)
const pageSize = ref(40) // 每頁40筆
const sortBy = ref('relevance')

const startIndex = computed(() => {
  return totalCount.value === 0 ? 0 : (pageIndex.value - 1) * pageSize.value + 1
})

const endIndex = computed(() => {
  const end = pageIndex.value * pageSize.value
  return end > totalCount.value ? totalCount.value : end
})

// 篩選邏輯
const filteredProducts = computed(() => {
  if (!Array.isArray(products.value)) return [];
  return products.value.filter((p) =>
    !keyword.value || p.name.includes(keyword.value)
  );
})

const isLoading = ref(false)

// 搜尋動作（之後可接後端 API）
async function searchProducts(page = 1) {
  if (isLoading.value) return
  isLoading.value = true
  showLoading('載入商品中...')

  try {
    parseSlug() // 解析 slug 再查詢

    const query = {
      pageIndex: page,
      pageSize: pageSize.value,
      sortBy: sortBy.value,
      isPublished: true,
      isFrontEnd: true,
    }

    if (productTypeId.value) query.productTypeId = productTypeId.value
    if (keyword.value) query.keyword = keyword.value

    const res = await ProductsApi.getProductList(query)
    const data = res.data

    products.value = Array.isArray(data?.items) ? data.items : []
    totalCount.value = data?.totalCount || 0
    pageIndex.value = data?.pageIndex || 1
  } catch (err) {
    console.error('搜尋商品錯誤：', err)
    products.value = []
    totalCount.value = 0
  } finally {
    isLoading.value = false
    hideLoading()
  }
}

// 監聽網址 query 變化時，自動重新搜尋
watch(
  () => [route.params.slug, route.query.q],
  () => searchProducts(1),
  { immediate: true }
)

// 點擊商品跳轉
const goToProduct = (productId) => {
  router.push({ name: "product-detail", params: { id: productId } });
  window.scrollTo({ top: 0, behavior: "smooth" });
}

// 🔸 加入購物車（範例）
const addToCart = (product) => {
  console.log('加入購物車：', product.productName)
}

// 生命週期
// 初始載入商品列表
onMounted(() => {
  searchProducts(1);
})
</script>
