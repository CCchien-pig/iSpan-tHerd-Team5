<!--
  PromoBanner.vue - 促銷橫幅組件
  功能：展示促銷活動信息，包含左側文字、右側文字和倒計時
  特色：漸層背景、響應式布局、可配置內容
  用途：用於首頁頂部、特殊促銷活動展示
-->
<template>
  <!-- 促銷橫幅容器 -->
  <div class="promo-banner main-color-green py-2">
    <div
      class="container-fluid d-flex justify-content-between align-items-center"
    >
      <div class="align-items-center d-flex gap-2">
        <!-- 左側促銷信息按鈕 -->
        <AppButton
          :text="leftText"
          variant="light"
          size="sm"
          :show-border="false"
          custom-class="main-color-green-text main-color-white border-0 p-2 rounded-pill"
        >
          <span class="hover-underline">{{ leftText }}</span>
          <!-- 倒計時（如果有） -->
          <span class="ms-3 hover-none" v-if="countdown">{{ countdown }}</span>
        </AppButton>

        <!-- 右側促銷信息按鈕 -->
        <AppButton
          :text="rightText"
          variant="light"
          size="sm"
          :show-border="false"
          custom-class="main-color-white-text bg-transparent border-0 p-2 rounded-pill"
        >
          <span class="hover-underline">{{ rightText }}</span>
        </AppButton>
      </div>

      <div class="d-flex gap-2">
        
        <!-- 分享按鈕 -->
        <AppButton
          variant="outline-light"
          size="sm"
          :show-border="true"
          custom-class="bg-transparent py-2 px-3 rounded-pill"
          class="main-color-white"
        >
          <span class="hover-none text-black">
            <i class="bi bi-share main-color-green-text"></i>
          </span>
        </AppButton>
      </div>
    </div>
  </div>
</template>

<script>
// 導入可重用按鈕組件
import AppButton from '@/components/ui/AppButton.vue';

/**
 * PromoBanner.vue 組件配置
 * 功能：可重用的促銷橫幅組件
 * 特色：支持自定義文字、倒計時顯示、響應式布局、統一按鈕樣式
 */
export default {
  name: 'PromoBanner', // 組件名稱

  /**
   * 子組件註冊
   */
  components: {
    AppButton,
  },

  /**
   * Props定義 - 組件的可配置屬性
   */
  props: {
    // 左側促銷文字
    leftText: {
      type: String,
      default: 'tHerd 自主研發品牌 71折',
    },
    // 右側促銷文字
    rightText: {
      type: String,
      default: '29週年慶特惠',
    },
    // 倒計時文字
    countdown: {
      type: String,
      default: '還剩: 10小時 53分 36秒',
    },
  },
};
</script>

<style scoped>
/* .promo-banner {
  background: linear-gradient(135deg, #6f42c1, #8e44ad) !important;
} */
</style>
