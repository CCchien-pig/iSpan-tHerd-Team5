<!--
  BrandSection.vue - 品牌展示組件
  功能：展示合作品牌Logo，建立品牌信任度
  特色：灰階效果、懸停變彩色、響應式網格布局
  用途：用於首頁、關於我們頁面等需要展示合作夥伴的區域
-->
<template>
  <!-- 品牌展示區塊容器 -->
  <section class="brands-section py-5">
    <div class="container">
      <!-- 區塊標題 -->
      <h2 class="text-center mb-5">{{ title }}</h2>
      <!-- 品牌Logo網格 -->
      <div class="row g-4 align-items-center">
        <!-- 遍歷品牌數據，生成品牌Logo -->
        <div
          v-for="brand in brands"
          :key="brand.name"
          class="col-lg-2 col-md-4 col-6"
        >
          <!-- 品牌Logo容器 -->
          <div class="brand-item text-center">
            <img
              :src="brand.logo"
              :alt="brand.name"
              class="img-fluid"
              @click="handleBrandClick(brand)"
            />
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script>
/**
 * BrandSection.vue 組件配置
 * 功能：可重用的品牌展示組件
 * 特色：支持動態品牌數據、Logo展示、交互事件
 */
export default {
  name: 'BrandSection', // 組件名稱

  /**
   * Props定義 - 組件的可配置屬性
   */
  props: {
    // 區塊標題
    title: {
      type: String,
      default: '熱門品牌',
    },
    // 品牌數據數組
    brands: {
      type: Array,
      required: true, // 必須提供品牌數據
      default: () => [], // 默認為空數組
    },
  },

  /**
   * 方法定義 - 處理用戶交互事件
   */
  methods: {
    /**
     * 處理品牌點擊事件
     * @param {Object} brand - 被點擊的品牌對象
     * 發送brand-click事件給父組件，傳遞品牌信息
     */
    handleBrandClick(brand) {
      this.$emit('brand-click', brand);
    },
  },
};
</script>

<style scoped>
.brand-item img {
  max-height: 60px;
  filter: grayscale(100%);
  transition: filter 0.3s ease;
  cursor: pointer;
}

.brand-item:hover img {
  filter: grayscale(0%);
}
</style>
