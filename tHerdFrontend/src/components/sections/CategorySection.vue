<!--
  CategorySection.vue - 產品分類展示組件
  功能：展示產品分類列表，幫助用戶快速導航到感興趣的產品類別
  特色：卡片式布局、懸停動畫效果、響應式設計
  用途：用於首頁、產品頁面等需要分類導航的區域
-->
<template>
  <!-- 分類區塊容器 -->
  <section class="categories-section py-5">
    <div class="container">
      <!-- 區塊標題 -->
      <h2 class="text-center mb-5">{{ title }}</h2>
      <!-- 分類卡片網格 -->
      <div class="row g-4">
        <!-- 遍歷分類數據，生成分類卡片 -->
        <div
          v-for="category in categories"
          :key="category.id"
          class="col-lg-3 col-md-6"
        >
          <!-- 分類卡片 -->
          <div class="category-card h-100">
            <!-- 分類圖標 -->
            <div class="category-icon">
              <i :class="category.icon"></i>
            </div>
            <!-- 分類名稱 -->
            <h5>{{ category.name }}</h5>
            <!-- 分類描述 -->
            <p>{{ category.description }}</p>
            <!-- 瀏覽按鈕 -->
            <a
              href="#"
              class="btn btn-outline-success"
              @click="handleCategoryClick(category)"
            >
              瀏覽產品
            </a>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script>
/**
 * CategorySection.vue 組件配置
 * 功能：可重用的產品分類展示組件
 * 特色：支持動態分類數據、卡片式布局、交互事件
 */
export default {
  name: 'CategorySection', // 組件名稱

  /**
   * Props定義 - 組件的可配置屬性
   */
  props: {
    // 區塊標題
    title: {
      type: String,
      default: '熱門分類',
    },
    // 分類數據數組
    categories: {
      type: Array,
      required: true, // 必須提供分類數據
      default: () => [], // 默認為空數組
    },
  },

  /**
   * 方法定義 - 處理用戶交互事件
   */
  methods: {
    /**
     * 處理分類點擊事件
     * @param {Object} category - 被點擊的分類對象
     * 發送category-click事件給父組件，傳遞分類信息
     */
    handleCategoryClick(category) {
      this.$emit('category-click', category);
    },
  },
};
</script>

<style scoped>
.category-card {
  background: white;
  border-radius: 1rem;
  padding: 2rem;
  text-align: center;
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.08);
  transition:
    transform 0.3s ease,
    box-shadow 0.3s ease;
}

.category-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.15);
}

.category-icon {
  font-size: 3rem;
  color: #28a745;
  margin-bottom: 1rem;
}
</style>
