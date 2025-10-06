<!--
  ProductActions.vue - 產品操作組件
  功能：提供產品相關的操作按鈕，如加入購物車、收藏、快速查看
  特色：動態狀態管理、事件發送、響應式按鈕
  用途：用於產品卡片中的操作按鈕區域
-->
<template>
  <!-- 產品操作按鈕容器 -->
  <div class="product-actions">
    <!-- 主要操作：加入購物車 -->
    <button class="btn btn-success w-100 mt-3" @click="handleAddToCart">
      <i class="bi bi-cart-plus me-2"></i>
      加入購物車
    </button>

    <!-- 次要操作：收藏和快速查看 -->
    <div class="additional-actions mt-2">
      <!-- 收藏按鈕 - 根據收藏狀態顯示不同樣式 -->
      <button
        class="btn btn-outline-secondary btn-sm me-2"
        @click="handleAddToWishlist"
        :class="{ 'btn-danger': isInWishlist }"
      >
        <i class="bi" :class="isInWishlist ? 'bi-heart-fill' : 'bi-heart'"></i>
      </button>

      <!-- 快速查看按鈕 -->
      <button class="btn btn-outline-secondary btn-sm" @click="handleQuickView">
        <i class="bi bi-eye"></i>
      </button>
    </div>
  </div>
</template>

<script>
/**
 * ProductActions.vue 組件配置
 * 功能：可重用的產品操作組件
 * 特色：支持多種操作、狀態管理、事件發送
 */
export default {
  name: 'ProductActions', // 組件名稱

  /**
   * Props定義 - 組件的可配置屬性
   */
  props: {
    // 產品數據對象
    product: {
      type: Object,
      required: true, // 必須提供產品數據
    },
  },

  /**
   * 組件數據 - 操作狀態
   */
  data() {
    return {
      isInWishlist: false, // 是否在收藏清單中
    };
  },

  /**
   * 方法定義 - 處理產品操作
   */
  methods: {
    /**
     * 處理加入購物車操作
     * 發送add-to-cart事件給父組件
     */
    handleAddToCart() {
      this.$emit('add-to-cart', this.product);
      // 這裡可以添加加入購物車的邏輯
      console.log('加入購物車:', this.product.name);
    },

    /**
     * 處理收藏切換操作
     * 切換收藏狀態並發送toggle-wishlist事件
     */
    handleAddToWishlist() {
      this.isInWishlist = !this.isInWishlist;
      this.$emit('toggle-wishlist', this.product, this.isInWishlist);
      console.log('收藏狀態:', this.isInWishlist ? '已收藏' : '取消收藏');
    },

    /**
     * 處理快速查看操作
     * 發送quick-view事件給父組件
     */
    handleQuickView() {
      this.$emit('quick-view', this.product);
      console.log('快速查看:', this.product.name);
    },
  },
};
</script>

<style scoped>
.product-actions {
  margin-top: auto;
}

.additional-actions {
  display: flex;
  justify-content: center;
  gap: 0.5rem;
}

.btn-sm {
  padding: 0.375rem 0.75rem;
  font-size: 0.875rem;
}
</style>
