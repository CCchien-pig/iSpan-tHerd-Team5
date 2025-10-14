<!--
  ProductBreadcrumb.vue - 產品麵包屑組件
  功能：顯示產品頁面的導航路徑，包含首頁、分類、品牌、產品詳情
  特色：動態路徑生成、可配置麵包屑項目
  用途：用於產品詳情頁面的導航路徑顯示
-->
<template>
  <!-- 使用通用麵包屑組件，傳入產品相關的導航路徑 -->
  <BreadcrumbNav :breadcrumbs="productBreadcrumbs" />
</template>

<script>
// 導入通用麵包屑導航組件
import BreadcrumbNav from '@/components/ui/BreadcrumbNav.vue';

/**
 * ProductBreadcrumb.vue 組件配置
 * 功能：產品專用的麵包屑導航組件
 * 特色：預設產品導航路徑、可擴展配置
 */
export default {
  name: 'ProductBreadcrumb', // 組件名稱

  /**
   * 子組件註冊
   */
  components: {
    BreadcrumbNav,
  },

  /**
   * 計算屬性 - 生成產品麵包屑導航路徑
   */
  computed: {
    /**
     * 產品麵包屑導航路徑
     * 包含從首頁到產品詳情的完整路徑
     * TODO: 可根據實際路由動態生成
     */
    productBreadcrumbs() {
      return [
        { name: '首頁', path: '/' },
        { name: '品牌 A-Z', path: '/brands' },
        { name: 'Life Extension', path: '/brands/life-extension' },
        { name: '產品詳情', path: null }, // 當前頁面，無需鏈接
      ];
    },
  },
};
</script>
