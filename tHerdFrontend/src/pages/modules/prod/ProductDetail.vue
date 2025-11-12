<!--
  ProductDetail.vue - 商品詳情頁面
  功能：顯示商品完整資訊、規格選擇、加入購物車、收藏、按讚與最近瀏覽
-->
<template>
  <div class="product-detail-page">
    <!-- 🧭 麵包屑導航 -->
    <Breadcrumb :breadcrumbs="breadcrumbs" />

    <!-- 🚫 錯誤訊息 -->
    <div v-if="error" class="alert alert-danger container mt-4" role="alert">
      {{ error }}
    </div>

    <!-- ✅ 商品內容 -->
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
                :favorite-count="product.favoriteCount"
                :toggling-favorite="togglingFavorite"
                :is-liked="isLiked"
                :like-count="product.likeCount"
                :toggling-like="togglingLike"
                v-model:quantity="quantity"
                @add-to-cart="handleAddToCart"
                @toggle-favorite="handleToggleFavorite"
                @toggle-like="handleToggleLike"
              />
            </div>
          </div>
        </div>
      </div>

      <!-- 📜 商品詳細描述 -->
      <div class="row mt-5">
        <div class="col-12">
          <ProductTabs
            :product="product"
            :reviews="product.reviews"
            :questions="product.questions"
            @refresh="loadProduct"
          />
        </div>
      </div>

      <!-- 🕒 最近瀏覽商品 -->
      <RecentlyViewedHero class="mt-5"
        @add-to-cart="handleAddToCart"
        @toggle-wishlist="handleToggleWishlist"
        @quick-view="handleQuickView"
       />
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useLoading } from '@/composables/useLoading'
import ProductsApi from '@/api/modules/prod/ProductsApi'
import { error as showError, toast } from '@/utils/sweetalert'
import { calculateDiscount } from '@/utils/productUtils'
import Breadcrumb from '@/components/ui/Breadcrumb.vue'
import ProductImageGallery from '@/components/modules/prod/detail/ProductImageGallery.vue'
import ProductInfo from '@/components/modules/prod/detail/ProductInfo.vue'
import ProductPurchaseCard from '@/components/modules/prod/detail/ProductPurchaseCard.vue'
import ProductTabs from '@/components/modules/prod/detail/ProductTabs.vue'
import RecentlyViewedHero from '@/components/modules/prod/list/RecentlyViewedHero.vue'
import { http } from '@/api/http'
import { useAuthStore } from '@/stores/auth'

// 加入購物車
import { useAddToCart } from '@/composables/modules/prod/useAddToCart'
const { addToCart } = useAddToCart()
const route = useRoute()
const router = useRouter()
const { showLoading, hideLoading } = useLoading()
const auth = useAuthStore()

const error = ref(null)
const product = ref(null)
const selectedSpec = ref(null)
const quantity = ref(1)

/* ❤️ 收藏狀態 */
const favoriteIds = ref([])
const togglingFavorite = ref(false)
const isFavorited = computed(() =>
  !!product.value && favoriteIds.value.includes(product.value.productId)
)

/* 👍 按讚狀態（✅ 新增） */
const likedIds = ref([])
const togglingLike = ref(false)
const isLiked = computed(() =>
  !!product.value && likedIds.value.includes(product.value.productId)
)

/* 麵包屑 */
const breadcrumbs = computed(() => {
  if (!product.value) return []
  const slug = product.value.brandName
    ? product.value.brandName.replace(/\s+/g, '-')
    : 'brand'
  return [
    { name: '首頁', path: '/' },
    { name: '品牌 A-Z', path: '/brands' },
    { name: product.value.brandName, path: `/brands/${slug}-${product.value.brandId}` },
    { name: product.value.productName, path: null },
  ]
})

/* 💰 價格邏輯 */
const currentPrice = computed(() => selectedSpec.value?.salePrice || selectedSpec.value?.unitPrice || product.value?.salePrice || 0)
const originalPrice = computed(() => selectedSpec.value?.unitPrice || product.value?.listPrice || 0)
const hasDiscount = computed(() => currentPrice.value < originalPrice.value)
const discountPercent = computed(() => calculateDiscount(currentPrice.value, originalPrice.value))

/* 📦 載入商品資料 */
const loadProduct = async () => {
  try {
    showLoading('載入商品資料中...')
    error.value = null
    const productId = route.params.id
    const response = await ProductsApi.getProductDetail(productId)

    if (response.success) {
      product.value = response.data
      if (product.value.Specs?.length > 0) selectedSpec.value = product.value.Specs[0]

       // 儲存近期瀏覽到快取
      saveRecentlyViewed(product.value)

      await refreshProductStats(); // 顯示按讚狀態
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

/* 🕒 記錄最近瀏覽商品 */
function saveRecentlyViewed(p) {
  if (!p) return
  const key = 'recently_viewed_products'
  let list = JSON.parse(localStorage.getItem(key)) || []

  // 移除重複
  list = list.filter(x => x.productId !== p.productId)

  // 儲存完整資料
  list.unshift({
    productId: p.productId,
    mainSkuId: p.mainSkuId || p.productId, // ✅ 主 SKU
    productName: p.productName,
    brandName: p.brandName,
    imageUrl: p.imageUrl || '',
    avgRating: p.avgRating || 0,
    reviewCount: p.reviewCount || 0,
    billingPrice: p.billingPrice || p.salePrice || 0, // 售價
    listPrice: p.listPrice || p.unitPrice || 0,       // 原價
    salePrice: p.salePrice || 0,                      // 折扣價
    badgeName: p.badgeName || null,                   // 顯示標章

    // 新增：即時同步收藏 / 按讚數
    favoriteCount: p.favoriteCount || 0,
    likeCount: p.likeCount || 0
  })

  // 最多只留 10 筆
  if (list.length > 10) list = list.slice(0, 10)

  localStorage.setItem(key, JSON.stringify(list))
  window.dispatchEvent(new CustomEvent('recently-viewed-updated'))
}

/* ❤️ 讀取收藏清單 */
async function loadFavoriteIds() {
  try {
    const { data } = await http.get('/user/favorites/ids')
    favoriteIds.value = Array.isArray(data) ? data : []
  } catch (err) {
    if (err?.response?.status !== 401) console.warn('[favorite ids] load failed', err)
  }
}

/* 👍 檢查是否已按讚 */
async function checkLikeStatus() {
  try {
    if (!product.value?.productId) return

    const { data } = await http.get(`/prod/products/check/${product.value.productId}`)
    // const { data } = await http.get('/prod/products/check', { params: { productId: product.value.productId } })

    const isLiked = data?.data?.isLiked ?? data?.isLiked

    likedIds.value = isLiked ? [product.value.productId] : []
  } catch (err) {
    if (err?.response?.status !== 401) console.warn('[like status] load failed', err)
  }
}

function updateRecentlyViewedStats(updatedProduct) {
  const key = 'recently_viewed_products'
  let list = JSON.parse(localStorage.getItem(key)) || []
  const idx = list.findIndex(x => x.productId === updatedProduct.productId)

  if (idx !== -1) {
    list[idx].favoriteCount = updatedProduct.favoriteCount || 0
    list[idx].likeCount = updatedProduct.likeCount || 0
    localStorage.setItem(key, JSON.stringify(list))
    window.dispatchEvent(new CustomEvent('recently-viewed-updated'))
  }
}

/**
 * 加入購物車事件
 * - 支援：目前商品（selectedSpec）或最近瀏覽商品（product 物件）
 */
async function handleAddToCart(fromSkuOrProduct = null, qty = quantity.value) {
  try {
    let productToAdd = product.value
    let skuToAdd = null

    // Case 1：從最近瀏覽傳來的整個商品物件
    // e.g. RecentlyViewedHero @add-to-cart="handleAddToCart(product)"
    if (fromSkuOrProduct?.productId && !fromSkuOrProduct?.skuId) {
      productToAdd = fromSkuOrProduct
      skuToAdd = {
        skuId: fromSkuOrProduct.mainSkuId || fromSkuOrProduct.productId,
        optionName: fromSkuOrProduct.productName,
        billingPrice: fromSkuOrProduct.billingPrice || fromSkuOrProduct.salePrice || 0,
        unitPrice: fromSkuOrProduct.listPrice || 0,
      }
    }

    // Case 2：從購買卡片（有 selectedSku）
    else if (fromSkuOrProduct?.skuId) {
      skuToAdd = fromSkuOrProduct
    }

    // Case 3：沒傳任何東西，使用目前頁面選中的規格
    else {
      skuToAdd = selectedSpec.value || {
        skuId: productToAdd.mainSkuId || productToAdd.productId,
        optionName: productToAdd.productName,
        billingPrice: productToAdd.billingPrice || productToAdd.salePrice || 0,
        unitPrice: productToAdd.listPrice || 0,
      }
    }

    // 防呆
    if (!productToAdd || !skuToAdd?.skuId) {
      showError('請選擇有效的商品或規格')
      return
    }

    // 調用共用 composable
    await addToCart(productToAdd, skuToAdd, qty)
  } catch (err) {
    console.error('❌ 加入購物車錯誤:', err)
    showError('加入購物車失敗，請稍後再試')
  }
}

/* ❤️ 收藏切換 */
const handleToggleFavorite = async () => {
  if (!product.value) return
  if (!auth?.user) return router.push({ name: 'userlogin', query: { returnUrl: route.fullPath } })
  if (togglingFavorite.value) return

  togglingFavorite.value = true
  const pid = product.value.productId
  const originallyFavorited = favoriteIds.value.includes(pid)

  favoriteIds.value = originallyFavorited
    ? favoriteIds.value.filter(id => id !== pid)
    : [...favoriteIds.value, pid]

  try {
    const { data } = await http.post('/user/favorites/toggle', { productId: pid })
    toast(data?.isFavorited ? '已加入我的最愛' : '已取消收藏', data?.isFavorited ? 'success' : 'info')
    window.dispatchEvent(new CustomEvent('favorite-changed'))
    await refreshProductStats()
  } catch {
    showError('操作失敗，請稍後再試')
  } finally {
    togglingFavorite.value = false
  }
}

/* 👍 按讚切換 */
const handleToggleLike = async () => {
  if (!product.value) return
  if (!auth?.user) {
    return router.push({ name: 'userlogin', query: { returnUrl: route.fullPath } })
  }
  if (togglingLike.value) return
  togglingLike.value = true

  const pid = product.value.productId
  const originallyLiked = likedIds.value.includes(pid)

  // 立即反映 UI 狀態
  likedIds.value = originallyLiked
    ? likedIds.value.filter(id => id !== pid)
    : [...likedIds.value, pid]

  try {
    const res = await http.post('/prod/products/toggle', { productId: pid })
    const data = res?.data?.data // ⚠️ ApiResponse.data 裡才是真正資料
    if (!data) throw new Error('Invalid response')

    if (data.isLiked) {
      toast('已按讚 👍', 'success')
    } else {
      toast('已取消讚 👎', 'info')
    }

    // 觸發全域狀態變化（可有可無）
    window.dispatchEvent(new CustomEvent('like-changed'))

    await refreshProductStats()
  } catch (err) {
    // 復原 UI 狀態
    likedIds.value = originallyLiked
      ? [...likedIds.value, pid]
      : likedIds.value.filter(id => id !== pid)

    showError('操作失敗，請稍後再試')
  } finally {
    togglingLike.value = false
  }
}

// 更新總讚、收藏數
async function refreshProductStats() {
  if (!product.value?.productId) return
  const stats = await ProductsApi.getProductStats(product.value.productId)

  if (stats) {
    product.value.favoriteCount = stats.favoriteCount
    product.value.likeCount = stats.likeCount
  }
  
  updateRecentlyViewedStats(product.value)
}

/* 🧩 規格選擇 */
const handleSpecSelected = (spec) => (selectedSpec.value = spec)

watch(() => route.params.id, async () => {
  product.value = null
  selectedSpec.value = null
  quantity.value = 1
  await loadProduct()
  await loadFavoriteIds()
  await checkLikeStatus()
  window.scrollTo({ top: 0, behavior: 'smooth' })
})

onMounted(async () => {
  await loadProduct()
  await loadFavoriteIds()
  await checkLikeStatus()
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