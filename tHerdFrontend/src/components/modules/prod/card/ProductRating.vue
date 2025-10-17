<!--
  ProductRating.vue - 產品評分組件
  功能：顯示產品星級評分和評價數量
  特色：星級顯示、評價數量、可配置最大星數
  用途：用於產品卡片中的評分顯示區域
-->
<template>
  <!-- 產品評分容器 -->
  <div class="product-rating mb-2">
    <!-- 星級評分顯示 -->
    <div class="stars">
      <i
        v-for="n in maxStars"
        :key="n"
        class="bi bi-star-fill text-warning"
        :class="{ 'text-muted': n > rating }"
      ></i>
    </div>
    <!-- 評價數量顯示 -->
    <span class="ms-2 text-muted">({{ reviews }})</span>
  </div>
</template>

<script>
/**
 * ProductRating.vue 組件配置
 * 功能：可重用的產品評分組件
 * 特色：星級顯示、評價數量、可配置參數
 */
export default {
  name: 'ProductRating', // 組件名稱

  /**
   * Props定義 - 組件的可配置屬性
   */
  props: {
    // 評分（0-5星）
    rating: {
      type: Number,
      default: 5,
      validator: (value) => value >= 0 && value <= 5, // 驗證評分範圍
    },
    // 評價數量
    reviews: {
      type: Number,
      default: 0,
    },
    // 最大星數
    maxStars: {
      type: Number,
      default: 5,
    },
  },
}
</script>

<style scoped>
.product-rating {
  display: flex;
  align-items: center;
}

.stars {
  display: flex;
  gap: 0.125rem;
}
</style>
