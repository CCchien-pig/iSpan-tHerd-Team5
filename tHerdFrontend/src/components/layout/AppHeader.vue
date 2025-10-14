<!--
  AppHeader.vue - 主Header組件
  功能：包含Logo、搜索欄、用戶操作按鈕和購物車
  特色：響應式設計、搜索功能、用戶交互
  用途：作為所有頁面的頂部導航區域
-->
<template>
  <!-- 主Header容器 -->
  <header class="main-header bg-success text-white py-3">
    <div class="container-fluid">
      <div class="row align-items-center">
        <!-- Logo區域 -->
        <div class="col-md-2">
          <router-link
            to="/"
            class="navbar-brand text-white text-decoration-none"
          >
            <h3 class="mb-0 fw-bold"><img src="../../../public/homePageIcon/tHerd-header.png" alt="" class="img-fluid w-100 h-100"></h3>
          </router-link>
        </div>

        <!-- 搜索欄 -->
        <div class="col-md-6">
          <div class="search-container position-relative ">
            <input
              type="text"
              class="form-control form-control-lg rounded-pill pe-5"
              placeholder="搜尋所有 tHerd 商品"
              v-model="searchQuery"
              @keyup.enter="handleSearch"
            />
            <button
              class="btn btn-outline-primary position-absolute top-50 end-0 translate-middle-y me-2 rounded-circle d-flex align-items-center justify-content-center"
              style="width: 40px; height: 40px"
              @click="handleSearch"
            >
              <i class="bi bi-search"></i>
            </button>
          </div>
        </div>

        <!-- 右側功能區 -->
        <div class="col-md-4">
          <div class="d-flex justify-content-end align-items-center gap-3">
            <!-- 用戶登入 -->
            <div class="dropdown">
              <button
                class="btn btn-lg dropdown-toggle main-color-green"
                type="button"
                data-bs-toggle="dropdown"
              >
                <i class="bi bi-person me-1 main-color-white-text"></i>
                <span class="main-color-white-text">登入</span>
              </button>
              <ul class="dropdown-menu">
                <li><a class="dropdown-item" href="#">登入</a></li>
                <li><a class="dropdown-item" href="#">註冊</a></li>
                <li><hr class="dropdown-divider" /></li>
                <li><a class="dropdown-item" href="#">我的帳戶</a></li>
              </ul>
            </div>

            <!-- 購物車 -->
            <button class="btn btn-lg position-relative main-color-green">
              <i class="bi bi-cart3 me-1 main-color-white-text"></i>
              <span class="main-color-white-text">購物車</span>
              <span
                class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger"
              >
                {{ cartCount }}
              </span>
            </button>
          </div>
        </div>
      </div>
    </div>
  </header>
</template>

<script>
/**
 * AppHeader.vue 組件配置
 * 功能：主Header組件，包含Logo、搜索、用戶操作
 * 特色：響應式布局、搜索功能、購物車狀態
 */
export default {
  name: 'AppHeader', // 組件名稱

  /**
   * 組件數據 - 搜索和購物車狀態
   */
  data() {
    return {
      searchQuery: '', // 搜索關鍵字
      cartCount: 0, // 購物車商品數量
    };
  },

  /**
   * 方法定義 - 處理用戶交互
   */
  methods: {
    /**
     * 處理搜索功能
     * 當用戶輸入搜索關鍵字並按Enter或點擊搜索按鈕時觸發
     * TODO: 實現搜索功能
     */
    handleSearch() {
      if (this.searchQuery.trim()) {
        // 處理搜索邏輯
        // TODO: 實現搜索功能
        // 這裡可以導航到搜索結果頁面
        this.$router.push({
          name: 'search',
          query: { q: this.searchQuery },
        });
      }
    },
  },
};
</script>

<style scoped>
@media (max-width: 768px) {
  .main-header .col-md-2,
  .main-header .col-md-6,
  .main-header .col-md-4 {
    margin-bottom: 1rem;
  }

  .search-container {
    margin-bottom: 1rem;
  }
}
</style>
