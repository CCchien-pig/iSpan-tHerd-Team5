<!--
  AppLoading.vue - 全站Loading組件
  功能：顯示全站Loading覆蓋層，包含旋轉動畫和自定義文字
  特色：全屏覆蓋、旋轉動畫、可配置文字、Pinia狀態管理
  用途：用於全站Loading狀態顯示
-->
<template>
  <!-- Loading覆蓋層 - 只在Loading狀態時顯示 -->
  <div v-if="loadingStore.isLoading" class="loading-overlay">
    <div class="loading-container">
      <!-- 旋轉動畫容器 -->
      <div class="loading-spinner">
        <div class="spinner-ring"></div>
        <div class="spinner-ring"></div>
        <div class="spinner-ring"></div>
        <div class="spinner-ring"></div>
      </div>
      <!-- Loading文字顯示 -->
      <div class="loading-text">{{ loadingStore.loadingText }}</div>
    </div>
  </div>
</template>

<script>
// 導入Pinia Loading狀態管理
import { useLoadingStore } from '@/stores/loading'

/**
 * AppLoading.vue 組件配置
 * 功能：全站Loading覆蓋層組件
 * 特色：使用Pinia狀態管理、旋轉動畫、可配置文字
 */
export default {
  name: 'AppLoading', // 組件名稱

  /**
   * Composition API setup函數
   * 用於導入Pinia store並返回給模板使用
   */
  setup() {
    // 獲取Loading store實例
    const loadingStore = useLoadingStore()

    // 返回給模板使用的數據
    return {
      loadingStore,
    }
  },
}
</script>

<style scoped>
.loading-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(255, 255, 255, 0.9);
  backdrop-filter: blur(2px);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 9999;
}

.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.loading-spinner {
  position: relative;
  width: 60px;
  height: 60px;
}

.spinner-ring {
  position: absolute;
  width: 100%;
  height: 100%;
  border: 3px solid transparent;
  border-top: 3px solid #28a745;
  border-radius: 50%;
  animation: spin 1.2s cubic-bezier(0.5, 0, 0.5, 1) infinite;
}

.spinner-ring:nth-child(1) {
  animation-delay: -0.45s;
}

.spinner-ring:nth-child(2) {
  animation-delay: -0.3s;
  border-top-color: #20c997;
}

.spinner-ring:nth-child(3) {
  animation-delay: -0.15s;
  border-top-color: #17a2b8;
}

.spinner-ring:nth-child(4) {
  border-top-color: #6f42c1;
}

.loading-text {
  font-size: 1rem;
  font-weight: 500;
  color: #495057;
  text-align: center;
}

@keyframes spin {
  0% {
    transform: rotate(0deg);
  }
  100% {
    transform: rotate(360deg);
  }
}

/* 響應式設計 */
@media (max-width: 768px) {
  .loading-spinner {
    width: 50px;
    height: 50px;
  }

  .loading-text {
    font-size: 0.9rem;
  }
}

/* 深色主題支持 */
@media (prefers-color-scheme: dark) {
  .loading-overlay {
    background-color: rgba(0, 0, 0, 0.8);
  }

  .loading-text {
    color: #ffffff;
  }
}
</style>
