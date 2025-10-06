<!--
  FeatureSection.vue - 服務特色展示組件
  功能：展示服務特色和優勢，建立競爭優勢
  特色：圖標展示、簡潔布局、響應式設計
  用途：用於首頁、關於我們頁面等需要展示服務優勢的區域
-->
<template>
  <!-- 服務特色區塊容器 -->
  <section class="features-section py-5">
    <div class="container">
      <!-- 區塊標題 -->
      <h2 class="text-center mb-5">{{ title }}</h2>
      <!-- 特色項目網格 -->
      <div class="row g-4">
        <!-- 遍歷特色數據，生成特色項目 -->
        <div
          v-for="feature in features"
          :key="feature.id"
          class="col-lg-3 col-md-6"
        >
          <!-- 特色項目 -->
          <div class="feature-item text-center">
            <!-- 特色圖標 -->
            <div class="feature-icon">
              <i :class="feature.icon"></i>
            </div>
            <!-- 特色標題 -->
            <h5>{{ feature.title }}</h5>
            <!-- 特色描述 -->
            <p>{{ feature.description }}</p>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script>
/**
 * FeatureSection.vue 組件配置
 * 功能：可重用的服務特色展示組件
 * 特色：支持動態特色數據、圖標展示、簡潔布局
 */
export default {
  name: 'FeatureSection', // 組件名稱

  /**
   * Props定義 - 組件的可配置屬性
   */
  props: {
    // 區塊標題
    title: {
      type: String,
      default: '為什麼選擇 iHerb',
    },
    // 特色數據數組
    features: {
      type: Array,
      required: true, // 必須提供特色數據
      default: () => [], // 默認為空數組
    },
  },
};
</script>

<style scoped>
.feature-item {
  padding: 2rem 1rem;
}

.feature-icon {
  font-size: 3rem;
  color: #28a745;
  margin-bottom: 1rem;
}
</style>
