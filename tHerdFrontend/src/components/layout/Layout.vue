<!--
  Layout.vue - 主布局組件
  功能：定義整個應用的基本布局結構，包含Header、導航、內容區域和Footer
  特色：響應式布局、組件化設計、Loading覆蓋層
  用途：作為所有頁面的基礎布局容器
-->
<template>
  <!-- 主布局容器 - 使用Flexbox垂直布局 -->
  <div class="d-flex flex-column min-vh-100">
    <!-- 頂部促銷橫幅 -->
    <PromoBanner />

    <!-- 主要Header - 包含Logo、搜索、用戶操作 -->
     <header :class="{ 'header-scrolled': scroll }">
    <AppHeader />
     </header>

    <!-- 導航欄 - 主要導航菜單 -->
    <AppNavigation />

    <ScrollToTop />

    <!-- 促銷橫幅 - 特殊促銷活動展示 -->
    <SitePromoBanner />

    <!-- 麵包屑導航 - 頁面路徑導航 -->
    <!-- <BreadcrumbNav /> -->

    <!-- 主要內容區域 - 頁面內容插槽 -->
    <main class="flex-fill py-4">
      <slot />
    </main>

    <!-- Footer - 頁腳信息和鏈接 -->
    <AppFooter />

    <!-- Loading 覆蓋層 - 全站Loading狀態 -->
    <AppLoading />
  </div>
</template>

<script>
// 導入布局相關組件
import AppHeader from './AppHeader.vue'; // 主Header組件
import AppNavigation from './AppNavigation.vue'; // 導航組件
import AppFooter from './AppFooter.vue'; // Footer組件

// 導入通用組件
import PromoBanner from '@/components/common/PromoBanner.vue'; // 促銷橫幅
import SitePromoBanner from '@/components/common/SitePromoBanner.vue'; // 網站促銷橫幅

// 導入UI組件
import BreadcrumbNav from '@/components/ui/BreadcrumbNav.vue'; // 麵包屑導航
import AppLoading from '@/components/ui/AppLoading.vue'; // Loading組件

import ScrollToTop from '@/components/common/ScrollToTop.vue'
/**
 * Layout.vue 組件配置
 * 功能：應用主布局組件，定義整體頁面結構
 * 特色：組件化布局、響應式設計、插槽內容
 */
export default {
  name: 'AppLayout', // 組件名稱

  /**
   * 子組件註冊 - 布局相關的所有組件
   */
  components: {
    AppHeader,
    AppNavigation,
    AppFooter,
    PromoBanner,
    SitePromoBanner,
    BreadcrumbNav,
    AppLoading,
    ScrollToTop,
  },
  data(){
    return{
      scroll:false,
    }
  },
  methods:{
    handleHeader:function(){
      this.scroll=window.scrollY>50
    }
  },
  mounted(){
    window.addEventListener('scroll',this.handleHeader);
  },
  beforeUnmount(){
    window.removeEventListener('scroll',this.handleHeader);
  }
};
</script>

<style scoped>
/* 使用Bootstrap類，無需自定義CSS */
header {
  position: sticky;
  top: 0;
  z-index: 1050;
  transition: all 0.4s ease; /* 平滑過渡效果 */
}

header.header-scrolled {
  transform: translateY(0);
  opacity: 1;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1); /* 捲動後加陰影 */
}

</style>
