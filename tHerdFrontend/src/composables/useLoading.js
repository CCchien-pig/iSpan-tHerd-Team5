/**
 * useLoading.js - Loading 組合式函數
 * 功能：提供簡化的 loading 管理功能
 */

import { useLoadingStore } from '@/stores/loading'

/**
 * Loading 組合式函數
 * 提供簡化的 loading 狀態管理
 * @returns {Object} loading 相關方法
 */
export function useLoading() {
  const loadingStore = useLoadingStore()

  /**
   * 顯示 loading
   * @param {string} text - loading 文字
   */
  const showLoading = (text = '載入中...') => {
    loadingStore.showLoading(text)
  }

  /**
   * 隱藏 loading
   */
  const hideLoading = () => {
    loadingStore.hideLoading()
  }

  /**
   * 執行異步操作並自動管理 loading
   * @param {Function} asyncFn - 異步函數
   * @param {string} loadingText - loading 文字
   * @returns {Promise} 異步函數的結果
   * @example
   * const result = await withLoading(
   *   () => productApi.getProductDetail(id),
   *   '載入商品資料中...'
   * )
   */
  const withLoading = async (asyncFn, loadingText = '載入中...') => {
    try {
      showLoading(loadingText)
      const result = await asyncFn()
      return result
    } finally {
      hideLoading()
    }
  }

  /**
   * 設置 loading 文字
   * @param {string} text - loading 文字
   */
  const setLoadingText = (text) => {
    loadingStore.setLoadingText(text)
  }

  return {
    showLoading,
    hideLoading,
    withLoading,
    setLoadingText,
    isLoading: loadingStore.isLoading,
    loadingText: loadingStore.loadingText,
  }
}
