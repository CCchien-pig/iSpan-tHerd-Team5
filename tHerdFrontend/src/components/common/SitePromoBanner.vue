<!--
  SitePromoBanner.vue - 網站促銷橫幅組件
  功能：展示網站級別的促銷活動信息，如定期送貨優惠等
  特色：可配置標題和描述、居中顯示、漸層背景
  用途：用於展示網站級別的促銷活動
-->
<template>
  <!-- 網站促銷橫幅容器 -->
  <div class="site-promo-banner bg-info text-dark py-2">
    <div class="container">
      <!-- 促銷信息內容 - 居中顯示 -->
      <div class="text-center">
        <strong class="main-color-white-text">{{ title }}</strong>
        <span class="ms-3 main-color-white-text">{{ description }}</span>
      </div>
    </div>
  </div>
</template>

<script>
/**
 * SitePromoBanner.vue 組件配置
 * 功能：可重用的網站促銷橫幅組件
 * 特色：支持自定義標題和描述、響應式布局
 */
export default {
  name: 'SitePromoBanner', // 組件名稱

  /**
   * Props定義 - 組件的可配置屬性
   */
  props: {
    // 促銷標題
    title: {
      type: String,
      default: '"定期自動送貨享優惠"特價品',
    },
    // 促銷描述
    description: {
      type: String,
      default: '首次訂購可享20%或更多折扣。後續訂單均享10%折扣。',
    },
  },
};
</script>

<style scoped>
.site-promo-banner {
  background: linear-gradient(135deg, #17a2b8, #20c997) !important;
}
</style>
