<!--
  BreadcrumbNav.vue - 麵包屑導航組件
  功能：顯示頁面導航路徑，幫助用戶了解當前位置
  特色：動態路徑生成、路由鏈接、無障礙支持
  用途：用於所有頁面的導航路徑顯示
-->
<template>
  <!-- 麵包屑導航容器 -->
  <div class="breadcrumb-section bg-light py-2 border-bottom">
    <div class="container">
      <!-- 無障礙導航標籤 -->
      <nav aria-label="breadcrumb">
        <!-- 麵包屑列表 -->
        <ol class="breadcrumb mb-0">
          <!-- 遍歷麵包屑項目 -->
          <li
            v-for="(item, index) in breadcrumbs"
            :key="index"
            class="breadcrumb-item"
            :class="{ active: index === breadcrumbs.length - 1 }"
            :aria-current="index === breadcrumbs.length - 1 ? 'page' : null"
          >
            <!-- 可點擊的鏈接（非最後一項） -->
            <router-link
              v-if="item.path && index !== breadcrumbs.length - 1"
              :to="item.path"
              class="text-decoration-none"
            >
              {{ item.name }}
            </router-link>
            <!-- 當前頁面（最後一項） -->
            <span v-else>{{ item.name }}</span>
          </li>
        </ol>
      </nav>
    </div>
  </div>
</template>

<script>
/**
 * BreadcrumbNav.vue 組件配置
 * 功能：可重用的麵包屑導航組件
 * 特色：支持動態路徑、路由鏈接、無障礙支持
 */
export default {
  name: 'BreadcrumbNav', // 組件名稱

  /**
   * Props定義 - 組件的可配置屬性
   */
  props: {
    // 麵包屑項目數組
    breadcrumbs: {
      type: Array,
      default: () => [
        { name: '首頁', path: '/' },
        { name: '品牌 A-Z', path: '/brands' },
        { name: 'Life Extension', path: '/brands/life-extension' },
        { name: '產品詳情', path: null }, // 當前頁面，無需鏈接
      ],
    },
  },
};
</script>

<style scoped>
/* 使用Bootstrap類，無需自定義CSS */
</style>
