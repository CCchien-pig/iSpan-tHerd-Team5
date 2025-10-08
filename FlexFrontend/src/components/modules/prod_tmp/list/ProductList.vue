<!--
  ProductList.vue - 產品列表組件
  功能：展示產品列表，包含標題、查看全部按鈕和產品卡片網格
  特色：響應式網格布局、事件傳遞、可配置標題
  用途：用於首頁、產品頁面等需要展示多個產品的區域
-->
<template>
  <!-- 產品列表區塊容器 -->
  <section class="products-section py-5 bg-light">
    <div class="container">
      <!-- 標題和查看全部按鈕 -->
      <div class="d-flex justify-content-between align-items-center mb-5">
        <h2>{{ title }}</h2>
        <a href="#" class="btn btn-outline-primary">{{ viewAllText }}</a>
      </div>
      <!-- 產品卡片網格 -->
      <div class="row g-4">
        <!-- 遍歷產品數據，生成產品卡片 -->
        <div
          v-for="product in products"
          :key="product.id"
          class="col-lg-3 col-md-6"
        >
          <!-- 產品卡片組件 -->
          <ProductCard
            :product="product"
            @add-to-cart="handleAddToCart"
            @toggle-wishlist="handleToggleWishlist"
            @quick-view="handleQuickView"
          />
        </div>
      </div>
    </div>
  </section>
</template>

<script>
// 導入產品卡片組件
import ProductCard from '@/components/modules/prod/card/ProductCard.vue';

/**
 * ProductList.vue 組件配置
 * 功能：可重用的產品列表組件
 * 特色：支持動態產品數據、事件傳遞、響應式布局
 */
export default {
  name: 'ProductList', // 組件名稱

  /**
   * 子組件註冊
   */
  components: {
    ProductCard,
  },

  /**
   * Props定義 - 組件的可配置屬性
   */
  props: {
    // 列表標題
    title: {
      type: String,
      default: '熱銷產品',
    },
    // 產品數據數組
    products: {
      type: Array,
      required: true, // 必須提供產品數據
      default: () => [], // 默認為空數組
    },
    // 查看全部按鈕文字
    viewAllText: {
      type: String,
      default: '查看全部',
    },
  },

  /**
   * 方法定義 - 處理產品相關事件
   * 所有方法都通過emit向父組件發送事件
   */
  methods: {
    /**
     * 處理加入購物車事件
     * @param {Object} product - 產品對象
     * 發送add-to-cart事件給父組件
     */
    handleAddToCart(product) {
      this.$emit('add-to-cart', product);
    },

    /**
     * 處理收藏切換事件
     * @param {Object} product - 產品對象
     * @param {boolean} isInWishlist - 是否在收藏清單中
     * 發送toggle-wishlist事件給父組件
     */
    handleToggleWishlist(product, isInWishlist) {
      this.$emit('toggle-wishlist', product, isInWishlist);
    },

    /**
     * 處理快速查看事件
     * @param {Object} product - 產品對象
     * 發送quick-view事件給父組件
     */
    handleQuickView(product) {
      this.$emit('quick-view', product);
    },
  },
};
</script>

<style scoped>
/* 使用Bootstrap類，無需自定義CSS */
</style>
