<!--
  HeroSection.vue - 英雄區塊組件
  功能：首頁主要展示區域，包含標題、描述、按鈕和圖片
  特色：支持自定義內容、多個按鈕操作、響應式設計
  用途：用於首頁、關於我們頁面等需要突出展示的區域
-->
<template>
  <!-- 英雄區塊容器 -->
  <section class="hero-section">
    <div class="container">
      <div class="row align-items-center min-vh-50">
        <!-- 左側內容區域 -->
        <div class="col-lg-6">
          <!-- 主標題 -->
          <h1 class="display-3 fw-bold text-success mb-4">
            {{ title }}
          </h1>
          <!-- 描述文字 -->
          <p class="lead mb-4">
            {{ description }}
          </p>
          <!-- 按鈕組 -->
          <div class="d-flex flex-wrap gap-3">
            <!-- 主要行動按鈕 -->
            <button
              class="btn btn-success btn-lg px-4 py-3"
              @click="handlePrimaryAction"
            >
              <i class="bi bi-cart3 me-2"></i>
              {{ primaryButtonText }}
            </button>
            <!-- 次要行動按鈕 -->
            <button
              class="btn btn-outline-success btn-lg px-4 py-3"
              @click="handleSecondaryAction"
            >
              <i class="bi bi-play-circle me-2"></i>
              {{ secondaryButtonText }}
            </button>
            <!-- 測試Loading按鈕 -->
            <button
              class="btn btn-warning btn-lg px-4 py-3"
              @click="handleTestLoading"
            >
              <i class="bi bi-gear me-2"></i>
              測試Loading
            </button>
            <!-- 自定義Loading按鈕 -->
            <button
              class="btn btn-info btn-lg px-4 py-3"
              @click="handleCustomLoading"
            >
              <i class="bi bi-clock me-2"></i>
              自定義Loading
            </button>
          </div>
        </div>
        <!-- 右側圖片區域 -->
        <div class="col-lg-6">
          <div class="hero-image">
            <img
              :src="heroImage"
              :alt="heroImageAlt"
              class="img-fluid rounded shadow"
            />
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script>
/**
 * HeroSection.vue 組件配置
 * 功能：可重用的英雄區塊組件，支持自定義內容和事件
 * 特色：完全可配置的props，支持多種按鈕操作
 */
export default {
  name: 'HeroSection', // 組件名稱

  /**
   * Props定義 - 組件的可配置屬性
   * 所有props都有默認值，確保組件可以獨立使用
   */
  props: {
    // 主標題文字
    title: {
      type: String,
      default: '健康生活，從 iHerb 開始',
    },
    // 描述文字
    description: {
      type: String,
      default:
        '超過 30,000 種優質健康產品，包括維生素、礦物質、美容護膚、運動營養等，為您提供全方位的健康解決方案。',
    },
    // 主要按鈕文字
    primaryButtonText: {
      type: String,
      default: '立即購物',
    },
    // 次要按鈕文字
    secondaryButtonText: {
      type: String,
      default: '觀看介紹',
    },
    // 英雄圖片URL
    heroImage: {
      type: String,
      default: 'https://picsum.photos/600/400?random=14',
    },
    // 圖片替代文字
    heroImageAlt: {
      type: String,
      default: '健康產品',
    },
  },

  /**
   * 方法定義 - 處理用戶交互事件
   * 所有方法都通過emit向父組件發送事件
   */
  methods: {
    /**
     * 處理主要按鈕點擊
     * 發送primary-action事件給父組件
     */
    handlePrimaryAction() {
      this.$emit('primary-action');
    },

    /**
     * 處理次要按鈕點擊
     * 發送secondary-action事件給父組件
     */
    handleSecondaryAction() {
      this.$emit('secondary-action');
    },

    /**
     * 處理測試Loading按鈕點擊
     * 發送test-loading事件給父組件
     */
    handleTestLoading() {
      this.$emit('test-loading');
    },

    /**
     * 處理自定義Loading按鈕點擊
     * 發送custom-loading事件給父組件
     */
    handleCustomLoading() {
      this.$emit('custom-loading');
    },
  },
};
</script>

<style scoped>
.hero-section {
  background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
  padding: 4rem 0;
}

.min-vh-50 {
  min-height: 50vh;
}

.hero-image img {
  border-radius: 1rem;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1);
}

/* 響應式設計 */
@media (max-width: 768px) {
  .hero-section {
    padding: 2rem 0;
  }

  .display-3 {
    font-size: 2rem;
  }
}
</style>
