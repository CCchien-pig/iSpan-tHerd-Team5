import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useLoadingStore = defineStore('loading', () => {
  // 狀態
  const isLoading = ref(false);
  const loadingText = ref('載入中...');

  // 動作
  const showLoading = (text = '載入中...') => {
    loadingText.value = text;
    isLoading.value = true;
  };

  const hideLoading = () => {
    isLoading.value = false;
  };

  const setLoadingText = text => {
    loadingText.value = text;
  };

  return {
    // 狀態
    isLoading,
    loadingText,
    // 動作
    showLoading,
    hideLoading,
    setLoadingText,
  };
});
