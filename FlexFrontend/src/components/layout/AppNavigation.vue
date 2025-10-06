<!--
  AppNavigation.vue - 主導航組件
  功能：展示主要導航菜單，包含產品分類和功能頁面
  特色：響應式設計、動態高亮、路由導航
  用途：作為所有頁面的主要導航區域
-->
<template>
  <!-- 主導航容器 -->
  <nav class="main-navigation bg-white border-bottom">
    <div class="container-fluid">
      <div class="row">
        <div class="col-12">
          <!-- 導航菜單列表 -->
          <ul class="nav nav-pills justify-content-center flex-wrap py-2">
            <!-- 遍歷導航項目，生成導航鏈接 -->
            <li
              class="nav-item"
              v-for="item in navigationItems"
              :key="item.name"
            >
              <router-link
                :to="item.path"
                class="nav-link text-success fw-medium rounded-pill"
                :class="{ active: $route.path.startsWith(item.path) }"
              >
                {{ item.name }}
              </router-link>
            </li>
          </ul>
        </div>
      </div>
    </div>
  </nav>
</template>

<script>
/**
 * AppNavigation.vue 組件配置
 * 功能：主導航組件，展示產品分類和功能頁面
 * 特色：動態導航項目、路由高亮、響應式布局
 */
export default {
  name: 'AppNavigation', // 組件名稱

  /**
   * 組件數據 - 導航項目配置
   */
  data() {
    return {
      // 導航項目列表 - 包含產品分類和功能頁面
      // TODO: 更新為API數據或是寫成正確的導航項目
      navigationItems: [
        { name: '補充劑', path: '/supplements' },
        { name: '運動營養', path: '/sports-nutrition' },
        { name: '沐浴', path: '/bath' },
        { name: '美容美妝', path: '/beauty' },
        { name: '食品百貨', path: '/grocery' },
        { name: '健康家居', path: '/healthy-home' },
        { name: '嬰童用品', path: '/baby-kids' },
        { name: '寵物用品', path: '/pet-supplies' },
        { name: '品牌 A-Z', path: '/brands' },
        { name: '健康主題', path: '/health-topics' },
        { name: '特惠', path: '/specials' },
        { name: '暢銷', path: '/bestsellers' },
        { name: '試用', path: '/trials' },
        { name: '新產品', path: '/new-products' },
        { name: '健康中心', path: '/health-hub' },
      ],
    };
  },
};
</script>

<style scoped>
.main-navigation {
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.nav-link {
  color: #28a745 !important;
  transition: all 0.3s ease;
}

.nav-link:hover {
  background-color: #e8f5e8;
  color: #1e7e34 !important;
}

.nav-link.active {
  background-color: #28a745;
  color: white !important;
}

@media (max-width: 768px) {
  .nav {
    flex-direction: column;
    align-items: center;
  }

  .nav-item {
    width: 100%;
    text-align: center;
    margin-bottom: 0.25rem;
  }

  .nav-link {
    width: 100%;
    text-align: center;
  }
}

@media (max-width: 576px) {
  .nav {
    flex-wrap: wrap;
    justify-content: space-around;
  }

  .nav-item {
    flex: 0 0 auto;
    margin-bottom: 0.5rem;
  }
}
</style>
