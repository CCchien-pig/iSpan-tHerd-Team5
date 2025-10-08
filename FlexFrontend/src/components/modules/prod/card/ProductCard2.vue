<!--
  ProductCard.vue - 產品卡片組件
  功能：展示單個產品的完整信息，包括圖片、名稱、評分、價格和操作按鈕
  特色：組合多個子組件、懸停動畫效果、響應式設計
  用途：用於產品列表、推薦產品等需要展示產品信息的區域
-->
<template>
  <!-- 產品卡片容器 -->
  <div class="product-card h-100">
    <!-- 產品圖片區域 -->
    <div class="product-image">
      <img :src="product.image" :alt="product.name" />
      <!-- 產品標籤（如果有） -->
      <ProductBadge v-if="product.badge" :badge="product.badge" />
    </div>
    <!-- 產品信息區域 -->
    <div class="product-info">
      <!-- 產品名稱 -->
      <h6 class="product-name">{{ product.name }}</h6>
      <!-- 產品評分 -->
      <ProductRating :rating="5" :reviews="product.reviews" />
      <!-- 產品價格 -->
      <ProductPrice
        :current-price="product.price"
        :original-price="product.originalPrice"
      />
      <!-- 產品操作按鈕 -->
      <ProductActions :product="product" />
    </div>
  </div>
</template>

<script>
// 導入子組件
import ProductBadge from './ProductBadge.vue'; // 產品標籤組件
import ProductRating from './ProductRating.vue'; // 產品評分組件
import ProductPrice from './ProductPrice.vue'; // 產品價格組件
import ProductActions from './ProductActions.vue'; // 產品操作組件

/**
 * ProductCard.vue 組件配置
 * 功能：可重用的產品卡片組件
 * 特色：組合多個子組件、完整的產品信息展示
 */
export default {
  name: 'ProductCard', // 組件名稱

  /**
   * 子組件註冊
   */
  components: {
    ProductBadge,
    ProductRating,
    ProductPrice,
    ProductActions,
  },

  /**
   * Props定義 - 組件的可配置屬性
   */
  props: {
    // 產品數據對象
    product: {
      type: Object,
      required: true, // 必須提供產品數據
      // 驗證產品數據的完整性
      validator: product => {
        return product.id && product.name && product.image && product.price;
      },
    },
  },
};
</script>

<style scoped>
.product-card {
  background: white;
  border-radius: 1rem;
  overflow: hidden;
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.08);
  transition: transform 0.3s ease;
}

.product-card:hover {
  transform: translateY(-5px);
}

.product-image {
  position: relative;
  height: 200px;
  overflow: hidden;
}

.product-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.product-info {
  padding: 1.5rem;
}

.product-name {
  font-weight: 600;
  margin-bottom: 0.5rem;
  color: #333;
}
</style>
