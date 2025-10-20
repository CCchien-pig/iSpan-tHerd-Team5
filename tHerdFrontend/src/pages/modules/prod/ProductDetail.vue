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
                v-model:quantity="quantity"
                @add-to-cart="handleAddToCart"
                @toggle-favorite="handleToggleFavorite"
                @toggle-like="handleToggleLike"
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
import { ref, onMounted, computed } from 'vue'
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

const route = useRoute()
const router = useRouter()
const { showLoading, hideLoading } = useLoading()

// 狀態管理
const error = ref(null)
const product = ref(null)
const selectedSpec = ref(null)
const relatedProducts = ref([])
const quantity = ref(1)

// 麵包屑導航
const breadcrumbs = computed(() => {
  if (!product.value) return []

  return [
    { name: '首頁', path: '/' },
    { name: '品牌 A-Z', path: '/brands' },
    { name: product.value.brandName || 'California Gold Nutrition', path: '#' },
    { name: product.value.productName, path: null },
  ]
})

// 當前價格
const currentPrice = computed(() => {
  if (selectedSpec.value) {
    return selectedSpec.value.SalePrice || selectedSpec.value.UnitPrice
  }
  return product.value?.salePrice || 0
})

// 原價
const originalPrice = computed(() => {
  if (selectedSpec.value) {
    return selectedSpec.value.UnitPrice
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

/**
 * 處理規格選擇
 */
const handleSpecSelected = (spec) => {
  selectedSpec.value = spec
}

/**
 * 處理加入購物車
 */
const handleAddToCart = (data) => {
  if (!selectedSpec.value) {
    warning('請選擇商品規格')
    return
  }

  console.log('加入購物車:', {
    product: product.value,
    spec: selectedSpec.value,
    quantity: data.quantity,
    autoDelivery: data.autoDelivery,
  })

  // TODO: 實作購物車功能
  toast(
    `已加入購物車：${product.value.productName} - ${selectedSpec.value.OptionName} x ${data.quantity}`,
    'success',
    2000,
  )
}

/**
 * 處理收藏
 */
const handleToggleFavorite = async () => {
  try {
    // TODO: 實作收藏狀態管理
    const isFavorited = false // 假設目前未收藏

    if (isFavorited) {
      await ProductsApi.removeFavorite(product.value.productId)
      toast('已取消收藏', 'info')
    } else {
      await ProductsApi.addFavorite({ productId: product.value.productId })
      toast('已加入我的最愛', 'success')
    }
  } catch (err) {
    console.error('收藏操作錯誤:', err)
    showError('操作失敗，請稍後再試')
  }
}

/**
 * 處理按讚
 */
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
  // 滾動到頂部
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

// 生命週期
onMounted(() => {
  loadProduct()
  loadRelatedProducts()
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
