<!--
  ProductPrice.vue - 產品價格組件
  功能：顯示產品價格信息，包含現價、原價和折扣百分比
  特色：價格格式化、折扣計算、響應式布局
  用途：用於產品卡片中的價格顯示區域
-->
<template>
  <!-- 產品價格容器 -->
  <div class="product-price">
    <!-- 現價 - 主要價格顯示 -->
    <span class="current-price">NT$ {{ formatPrice(currentPrice) }}</span>

    <!-- 原價 - 如果有原價則顯示刪除線 -->
    <span v-if="originalPrice" class="original-price"> NT$ {{ formatPrice(originalPrice) }} </span>

    <!-- 折扣百分比 - 如果有折扣則顯示百分比 -->
    <span v-if="discountPercentage" class="discount-percentage"> -{{ discountPercentage }}% </span>
  </div>
</template>

<script>
/**
 * ProductPrice.vue 組件配置
 * 功能：可重用的產品價格組件
 * 特色：價格格式化、折扣計算、多種價格顯示
 */
export default {
  name: 'ProductPrice', // 組件名稱

  /**
   * Props定義 - 組件的可配置屬性
   */
  props: {
    // 現價
    currentPrice: {
      type: Number,
      required: true, // 必須提供現價
    },
    // 原價（可選）
    originalPrice: {
      type: Number,
      default: null, // 可選，用於顯示折扣
    },
  },

  /**
   * 計算屬性 - 折扣百分比計算
   */
  computed: {
    /**
     * 計算折扣百分比
     * 如果有原價且原價大於現價，則計算折扣百分比
     */
    discountPercentage() {
      if (!this.originalPrice || this.originalPrice <= this.currentPrice) {
        return null
      }
      return Math.round(((this.originalPrice - this.currentPrice) / this.originalPrice) * 100)
    },
  },

  /**
   * 方法定義 - 價格格式化
   */
  methods: {
    /**
     * 格式化價格顯示
     * 使用台灣地區的數字格式
     * @param {number} price - 要格式化的價格
     * @returns {string} 格式化後的價格字符串
     */
    formatPrice(price) {
      return new Intl.NumberFormat('zh-TW').format(price)
    },
  },
}
</script>

<style scoped>
.product-price {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.current-price {
  font-size: 1.25rem;
  font-weight: bold;
  color: #28a745;
}

.original-price {
  font-size: 1rem;
  color: #6c757d;
  text-decoration: line-through;
}

.discount-percentage {
  font-size: 0.875rem;
  font-weight: bold;
  color: #dc3545;
  background: #f8d7da;
  padding: 0.125rem 0.375rem;
  border-radius: 0.25rem;
}
</style>
