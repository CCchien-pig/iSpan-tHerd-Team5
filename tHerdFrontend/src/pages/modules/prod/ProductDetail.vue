<!--
  ProductDetail.vue - 商品詳情頁面
  功能：顯示商品完整資訊、規格選擇、加入購物車等
-->
<template>
  <div class="product-detail-page">
    <!-- 麵包屑導航 -->
    <Breadcrumb :breadcrumbs="breadcrumbs" />

    <!-- 錯誤訊息 -->
    <div v-if="error" class="alert alert-danger container mt-4" role="alert">
      {{ error }}
    </div>

    <!-- 商品內容 -->
    <div v-else-if="product" class="container py-4">
      <div class="row">
        <!-- 左側：商品圖片 -->
        <div class="col-lg-5 col-md-12 mb-4">
          <ProductImageGallery :images="product.images" :product-name="product.productName" />
        </div>

        <!-- 右側：商品資訊 + 購買卡片 -->
        <div class="col-lg-7 col-md-12">
          <div class="row">
            <!-- 商品資訊 -->
            <div class="col-lg-8 col-md-12 mb-4">
              <ProductInfo
                :product="product"
                :selected-spec="selectedSpec"
                @spec-selected="handleSpecSelected"
              />
            </div>

            <!-- 購買卡片 -->
            <div class="col-lg-4 col-md-12">
              <ProductPurchaseCard
                :current-price="currentPrice"
                :original-price="originalPrice"
                :has-discount="hasDiscount"
                :discount-percent="discountPercent"
                :unit-text="selectedSpec?.UnitText || '件'"
                :selected-sku="selectedSpec"
                :product-id="product.productId"
                :is-favorited="isFavorited"
                :toggling-favorite="togglingFavorite"
                v-model:quantity="quantity"
                @add-to-cart="handleAddToCart"
                @toggle-favorite="handleToggleFavorite"
              />
            </div>
          </div>
        </div>
      </div>

      <!-- 商品詳細描述 -->
      <div class="row mt-5">
        <div class="col-12">
          <ProductTabs :product="product" />
        </div>
      </div>

      <!-- 相關商品推薦 -->
      <div class="row mt-5" v-if="relatedProducts.length > 0">
        <div class="col-12">
          <h3 class="mb-4">組合推薦</h3>
          <div class="row">
            <div
              v-for="relatedProduct in relatedProducts"
              :key="relatedProduct.productId"
              class="col-lg-3 col-md-4 col-sm-6 mb-4"
            >
              <ProductCard
                :product="relatedProduct"
                @click="goToProduct(relatedProduct.productId)"
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useLoading } from '@/composables/useLoading'
import ProductsApi from '@/api/modules/prod/ProductsApi'
import { warning, error as showError, toast } from '@/utils/sweetalert'
import { calculateDiscount } from '@/utils/productUtils'
import Breadcrumb from '@/components/ui/Breadcrumb.vue'
import ProductImageGallery from '@/components/modules/prod/detail/ProductImageGallery.vue'
import ProductInfo from '@/components/modules/prod/detail/ProductInfo.vue'
import ProductPurchaseCard from '@/components/modules/prod/detail/ProductPurchaseCard.vue'
import ProductTabs from '@/components/modules/prod/detail/ProductTabs.vue'
import ProductCard from '@/components/modules/prod/card/ProductCard.vue'
import { http } from '@/api/http'
import { useAuthStore } from '@/stores/auth'

// 加入購物車
import { useAddToCart } from '@/composables/modules/prod//useAddToCart'
const { addToCart } = useAddToCart()

const route = useRoute()
const router = useRouter()
const { showLoading, hideLoading } = useLoading()
const auth = useAuthStore()

// 狀態管理
const error = ref(null)
const product = ref(null)
const selectedSpec = ref(null)
const relatedProducts = ref([])
const quantity = ref(1)

// NEW: 收藏相關本地狀態
const favoriteIds = ref([])
const togglingFavorite = ref(false)
const isFavorited = computed(() =>
  !!product.value && favoriteIds.value.includes(product.value.productId)
)

// 麵包屑導航
const breadcrumbs = computed(() => {
  if (!product.value) return []

  // 將品牌名稱轉 slug（例如空白換成連字號）
  const slug = product.value.brandName
    ? product.value.brandName.replace(/\s+/g, '-')
    : 'brand'

  return [
    { name: '首頁', path: '/' },
    { name: '品牌 A-Z', path: '/brands' },
    {
      name: product.value.brandName || 'California Gold Nutrition',
      // 導向品牌詳細頁
      path: `/brands/${slug}-${product.value.brandId}`,
    },
    { name: product.value.productName, path: null },
  ]
})

// 當前價格
const currentPrice = computed(() => {
  if (selectedSpec.value) {
    return selectedSpec.value.salePrice || selectedSpec.value.unitPrice
  }
  return product.value?.salePrice || 0
})

// 原價
const originalPrice = computed(() => {
  if (selectedSpec.value) {
    return selectedSpec.value.unitPrice
  }
  return product.value?.listPrice || 0
})

// 是否有折扣
const hasDiscount = computed(() => {
  return currentPrice.value < originalPrice.value
})

// 折扣百分比
const discountPercent = computed(() => {
  return calculateDiscount(currentPrice.value, originalPrice.value)
})

/**
 * 載入商品資料
 */
const loadProduct = async () => {
  try {
    showLoading('載入商品資料中...')
    error.value = null

    const productId = route.params.id
    const response = await ProductsApi.getProductDetail(productId)

    // console.log(response);
    product.value = response
    
    if (response.success) {
      product.value = response.data
      // 預設選擇第一個規格
      if (product.value.Specs && product.value.Specs.length > 0) {
        selectedSpec.value = product.value.Specs[0]
      }
    } else {
      error.value = response.message || '載入商品失敗'
    }
  } catch (err) {
    console.error('載入商品錯誤:', err)
    error.value = '載入商品時發生錯誤，請稍後再試'
  } finally {
    hideLoading()
  }
}

/**
 * 載入相關商品（組合推薦）
 */
const loadRelatedProducts = async () => {
  try {
    const response = await ProductsApi.getProductList({
      page: 1,
      pageSize: 4,
    })

    if (response.success && response.data.items) {
      relatedProducts.value = response.data.items
    }
  } catch (err) {
    console.error('載入相關商品錯誤:', err)
  }
}

watch(
  () => route.params.id,
  async (newId) => {
    // ⚠️ 清空舊商品資料與狀態
    product.value = null
    selectedSpec.value = null
    quantity.value = 1

    // 🔄 重新載入新商品資料
    await loadProduct()
    await loadRelatedProducts()
    await loadFavoriteIds()

    window.scrollTo({ top: 0, behavior: 'smooth' })
  }
)

// NEW: 讀取目前使用者的收藏 ProductId 清單
async function loadFavoriteIds() {
  try {
    const { data } = await http.get('/user/favorites/ids') // baseURL=/api
    favoriteIds.value = Array.isArray(data) ? data : []
  } catch (err) {
    // 未登入就忽略，不擋頁
    if (err?.response?.status !== 401) {
      console.warn('[favorite ids] load failed', err)
    }
  }
}

/**
 * 處理規格選擇
 */
const handleSpecSelected = (spec) => {
  selectedSpec.value = spec
}

/**
 * 處理加入購物車
 */
async function handleAddToCart(selectedSku, qty) {
  await addToCart(product.value, selectedSku, qty)
}

/**
 * 處理收藏
 */
const handleToggleFavorite = async () => {
  if (!product.value) return
  // 未登入 → 導去登入並帶回跳轉
  if (!auth?.user) {
    return router.push({ name: 'userlogin', query: { returnUrl: route.fullPath } })
  }

  if (togglingFavorite.value) return
  togglingFavorite.value = true

  // 樂觀更新
  const pid = product.value.productId
  const originallyFavorited = favoriteIds.value.includes(pid)
  if (originallyFavorited) {
    favoriteIds.value = favoriteIds.value.filter(id => id !== pid)
  } else {
    favoriteIds.value.push(pid)
  }

  try {
    const { data } = await http.post('/user/favorites/toggle', { productId: pid })
    const serverIsFav = !!data?.isFavorited
    const clientHas = favoriteIds.value.includes(pid)
    if (serverIsFav && !clientHas) favoriteIds.value.push(pid)
    if (!serverIsFav && clientHas) favoriteIds.value = favoriteIds.value.filter(id => id !== pid)

    toast(serverIsFav ? '已加入我的最愛' : '已取消收藏', serverIsFav ? 'success' : 'info')
    // 通知其他頁（例如側欄徽章、我的最愛頁）刷新
    window.dispatchEvent(new CustomEvent('favorite-changed'))
  } catch (err) {
    // 還原
    if (originallyFavorited) {
      if (!favoriteIds.value.includes(pid)) favoriteIds.value.push(pid)
    } else {
      favoriteIds.value = favoriteIds.value.filter(id => id !== pid)
    }

    if (err?.response?.status === 401) {
      router.push({ name: 'userlogin', query: { returnUrl: route.fullPath } })
    } else {
      const msg = err?.response?.data?.error || '操作失敗，請稍後再試'
      showError(msg)
    }
  } finally {
    togglingFavorite.value = false
  }
}

/**
 * 處理按讚
 */

 async function refreshFavoriteIds () {
  try {
    const { data } = await http.get('/user/favorites/ids')
    favoriteIds.value = Array.isArray(data) ? data : []
  } catch (e) {
    // 不影響主流程，失敗就當沒收藏
    favoriteIds.value = []
  }
}

const handleToggleLike = async () => {
  try {
    // TODO: 實作按讚狀態管理
    const isLiked = false // 假設目前未按讚

    if (isLiked) {
      await ProductsApi.unlikeProduct(product.value.productId)
      toast('已取消按讚', 'info')
    } else {
      await ProductsApi.likeProduct({ productId: product.value.productId })
      toast('已按讚', 'success')
    }
  } catch (err) {
    console.error('按讚操作錯誤:', err)
    showError('操作失敗，請稍後再試')
  }
}



/**
 * 前往其他商品頁面
 */
const goToProduct = (productId) => {
  router.push({ name: 'product-detail', params: { id: productId } })
  // 重新載入商品資料
  loadProduct()
  loadRelatedProducts()
  loadFavoriteIds() // NEW：切頁後也更新收藏狀態
  // 滾動到頂部
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

// 生命週期
onMounted(() => {
  loadProduct()
  loadRelatedProducts()
  loadFavoriteIds() // NEW：首次載入時抓使用者收藏清單
  refreshFavoriteIds()
})
</script>

<style scoped>
.product-detail-page {
  min-height: 100vh;
}

.container {
  max-width: 1200px;
}
</style>
