/**
 * mocks/index.js - Mock 系統入口檔案
 * 功能：初始化和管理所有 Mock API
 * 用途：統一設定所有模組的 Mock
 */

import { mockConfig, showMockConfig } from './mockConfig'
import { setupProductMock } from './productMock'

/**
 * 初始化所有 Mock API
 * 這個函數會在應用啟動時被呼叫
 */
export function initializeMocks() {
  if (!mockConfig.enabled) {
    return
  }

  // 設定商品模組 Mock
  setupProductMock()

  // 在開發環境顯示 Mock 配置
  if (import.meta.env.DEV) {
    showMockConfig()
  }
}

/**
 * 匯出 Mock 配置管理函數
 */
export { mockConfig, showMockConfig, isMockEnabled } from './mockConfig'

export default {
  initializeMocks,
}
