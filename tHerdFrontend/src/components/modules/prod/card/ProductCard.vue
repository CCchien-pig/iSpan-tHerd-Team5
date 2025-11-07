<template>
  <div class="product-card d-flex flex-column">
  <!-- 讓整張卡片可點擊進入商品詳細 -->
    <router-link
      :to="`/prod/products/${product.productId}`"
      class="stretched-link"
    ></router-link>
      
      <!-- 商品徽章 -->
      <ProductBadge
        v-if="product.badgeName"
        :badge="product.badgeName"
      />

        <!-- 商品圖片 -->
        <div class="product-image position-relative">
          <img :src="productImage" :alt="productName" class="img-fluid" />

          <!-- 加入購物車按鈕 -->
          <button
            class="btn btn-warning add-to-cart-btn fw-bold"
            @click.stop="handleAddToCart"
          >
            加入購物車
          </button>
        </div>

        <!-- 商品資訊 -->
        <div class="product-info flex-grow-1 d-flex flex-column justify-content-between p-2">
          <!-- 品牌名稱 -->
          <p class="brand-name text-muted mb-1">{{ product.brandName }}</p>

          <!-- 商品名稱 -->
          <p class="product-name mb-1">{{ productName }}</p>

          <!-- 評分 + 評價數 -->
          <div class="rating d-flex align-items-center mb-1">
            <span v-for="i in 5" :key="i" class="star">
              <i
                class="bi"
                :class="
                  i <= Math.round(avgRating) ? 'bi-star-fill text-warning' : 'bi-star text-warning'
                "
              >
              </i>
            </span>
            <span class="reviews text-primary ms-1">{{ reviewCount }}</span>
          </div>

          <!-- 價格 -->
          <div class="price">
            <span class="current-price">NT${{ currentPrice }}</span>
            <span v-if="hasDiscount" class="original-price">NT${{ originalPrice }}</span>
          </div>
        </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import ProductBadge from '@/components/modules/prod/card/ProductBadge.vue'

// 加入購物車
import { useAddToCart } from '@/composables/modules/prod/useAddToCart'
const { addToCart } = useAddToCart()

const props = defineProps({
  product: {
    type: Object,
    required: true,
  },
})

// 商品名稱 (兼容新舊格式)
const productName = computed(() => {
  return props.product.productName || props.product.name || ''
})

// 商品圖片 (兼容新舊格式)
const productImage = computed(() => {
  return props.product.imageUrl || props.product.image || 'https://via.placeholder.com/200'
})

// 評分
const avgRating = computed(() => {
  return props.product.avgRating || props.product.rating || 0
})

// 評價數
const reviewCount = computed(() => {
  return props.product.reviewCount || props.product.reviews || 0
})

// 當前價格
const currentPrice = computed(() => {
  return props.product.billingPrice || 0
})

// 原價
const originalPrice = computed(() => {
  return props.product.listPrice || 0
})

// 是否有折扣
const hasDiscount = computed(() => {
  return originalPrice.value > 0 && currentPrice.value < originalPrice.value
})

/**
 * 加入購物車事件
 * 因為列表商品多半只有一個 SKU（或暫時未開放選規格），
 * 所以直接取商品本身的價格/資訊
 */
async function handleAddToCart() {
  // 假設無規格，直接以商品價格作為 SKU
  const selectedSku = {
    skuId: props.product.mainSkuId || props.product.productId, // 沒有 skuId 時用 productId 代替
    billingPrice: props.product.billingPrice,
    optionName: props.product.productName,
  }

  // 調用共用 composable
  await addToCart(props.product, selectedSku, 1)
}
</script>

<style scoped>
.product-card {
  width: 100%;
  max-width: 300px;
  height: 420px; /* 固定卡片總高度 */
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  background: #fff;
  border: 1px solid #eee;
  border-radius: 0.5rem;
  overflow: hidden;
  transition:
    box-shadow 0.3s ease,
    transform 0.2s;
  cursor: pointer;
  position: relative;
}

.product-card:hover {
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.15);
  transform: translateY(-3px);
}

/* router-link 作為全區可點擊區域 */
.stretched-link {
  position: absolute;
  inset: 0;
  z-index: 1; /* router-link 底層 */
}

/* 商品徽章 */
.product-badge {
  position: absolute;
  top: 10px;
  right: 10px;
  z-index: 10;
}

.product-badge .badge {
  font-size: 0.75rem;
  padding: 0.35rem 0.65rem;
}

.product-image {
  position: relative;
  aspect-ratio: 1 / 1; /* 讓它維持正方形 */
  height: auto;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #fff;
}

.product-image img {
  width: 85%;              /* ✅ 從 100% 改成 85%，圖片會縮小 */
  height: 85%;             /* ✅ 保持正方形比例 */
  object-fit: contain;     /* ✅ 改成 contain，確保不被裁切 */
  transition: transform 0.3s ease;
}

.product-card:hover .product-image img {
  transform: scale(1.05);  /* ✅ 滑鼠移入時略微放大，增加動態感 */
}

.add-to-cart-btn {
  position: absolute;
  z-index: 2; /* 提升層級到 router-link 之上 */
  bottom: 10px; /* 固定在底部 */
  left: 20%; /* 距離左邊 10px，不再用 translate */
  right: 20%; /* 距離右邊 10px，讓它自動置中 */
  background-color: #f68b1e;
  color: white;
  border: none;
  padding: 0.4rem 0; /* 自動撐滿左右 */
  font-size: 0.85rem;
  border-radius: 4px;
  text-align: center;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.3s ease;
  width: 60%;
}

.product-card:hover .add-to-cart-btn {
  opacity: 1;
  pointer-events: auto;
}

.product-info {
  padding: 0.5rem 0.75rem;
  text-align: center;
}

.brand-name {
  font-size: 0.75rem;
  color: #666;
  margin-bottom: 0.25rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.product-name {
  font-size: 0.85rem;
  line-height: 1.2rem;
  color: #333;
  min-height: 2.4rem; /* 兩行固定高度 */
  overflow: hidden;
}

.rating {
  display: flex;
  align-items: center;
  justify-content: center; /* 讓整塊置中 */
  line-height: 1; /* 避免 baseline 不一樣 */
}

.rating .star i {
  font-size: 0.9rem;
  vertical-align: middle; /* 強制與數字同一高度 */
}

.reviews {
  font-size: 0.8rem;
  margin-left: 4px; /* 取代 ms-1 更精準 */
  vertical-align: middle;
}

.rating .star {
  font-size: 0.9rem;
}
.price {
  margin-top: 0.2rem;
}

.current-price {
  font-size: 1rem;
  font-weight: bold;
  color: #d32f2f;
  margin-right: 0.4rem;
}

.original-price {
  font-size: 0.85rem;
  color: #999;
  text-decoration: line-through;
}
</style>
