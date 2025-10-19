<template>
  <header class="main-header bg-success text-white py-2">
    <div class="container-fluid">
      <div class="row align-items-center g-2">
        <!-- ✅ Logo區 -->
        <div class="col-6 col-md-2 col-lg-2 d-flex align-items-center flex-shrink-0">
          <router-link
            to="/"
            class="navbar-brand text-white text-decoration-none d-flex align-items-center"
          >
            <img
              src="/homePageIcon/tHerd-header.png"
              alt="tHerd Logo"
              class="img-fluid ms-2 ms-md-4"
              style="max-height: 50px;"
            />
          </router-link>
        </div>

        <!-- ✅ 搜尋欄 (桌機顯示) -->
        <div class="col search-col d-none d-md-flex align-items-center justify-content-center flex-grow-1 flex-shrink-1 flex-basis-0">
          <div class="search-container d-flex align-items-center position-relative w-100">
            <input
              type="text"
              class="form-control form-control-lg rounded-pill pe-5"
              placeholder="搜尋所有 tHerd 商品"
              style="padding-left: 30px;"
              v-model="searchQuery"
              @keyup.enter="handleSearch"
            />
            <button
              class="btn btn-outline-primary rounded-circle search-btn d-flex align-items-center justify-content-center"
              @click="handleSearch"
            >
              <i class="bi bi-search"></i>
            </button>
          </div>
        </div>

        <!-- ✅ 桌機右側功能 -->
        <div class="col-auto d-none d-md-flex align-items-center justify-content-end gap-3 flex-shrink-0">
          <!-- 用戶登入 -->
          <div class="dropdown">
            <button
              class="btn btn-md dropdown-toggle main-color-green main-color-white-text"
              type="button"
              id="userDropdown"
              data-bs-toggle="dropdown"
              data-bs-auto-close="true"
              aria-expanded="false"
            >
              <i class="bi bi-person me-1"></i>
              <span>登入</span>
            </button>
            <ul class="dropdown-menu dropdown-menu-end" aria-labelledby="userDropdown">
              <li><a class="dropdown-item" href="/login"><i class="bi bi-box-arrow-in-right me-2"></i>登入</a></li>
              <li><a class="dropdown-item" href="/register"><i class="bi bi-person-plus me-2"></i>註冊</a></li>
              <li><hr class="dropdown-divider" /></li>
              <li><a class="dropdown-item" href="/profile"><i class="bi bi-person-circle me-2"></i>我的帳戶</a></li>
            </ul>
          </div>
          
          <!-- 訂單 -->
          <button class="btn btn-md position-relative main-color-green">
            <i class="bi bi-bag main-color-white-text"></i>
            <span class="main-color-white-text ms-1">訂單</span>
          </button>
          
          <!-- 購物車 -->
          <button 
            @click="goToCart" 
            class="btn btn-md position-relative main-color-green"
          >
            <i class="bi bi-cart3 me-1 main-color-white-text"></i>
            <span class="main-color-white-text">購物車</span>
            <span
              v-if="cartCount > 0"
              class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger"
            >
              {{ cartCount }}
            </span>
          </button>
        </div>

        <!-- ✅ 手機版功能（漢堡選單） -->
        <div class="col-6 d-flex justify-content-end d-md-none">
          <button
            class="btn btn-outline-light"
            type="button"
            data-bs-toggle="collapse"
            data-bs-target="#mobileMenu"
            aria-expanded="false"
            aria-controls="mobileMenu"
          >
            <i class="bi bi-list fs-3"></i>
          </button>
        </div>
      </div>

      <!-- ✅ 手機搜尋欄 -->
      <div class="row d-md-none mt-2">
        <div class="col-12">
          <div class="search-container d-flex align-items-center position-relative w-100">
            <input
              type="text"
              class="form-control rounded-pill pe-5"
              placeholder="搜尋所有 tHerd 商品"
              style="padding-left: 20px;"
              v-model="searchQuery"
              @keyup.enter="handleSearch"
            />
            <button
              class="btn btn-outline-primary rounded-circle search-btn d-flex align-items-center justify-content-center"
              @click="handleSearch"
            >
              <i class="bi bi-search"></i>
            </button>
          </div>
        </div>
      </div>

      <!-- ✅ 手機選單 -->
      <div class="collapse mt-2 d-md-none" id="mobileMenu">
        <div class="d-flex flex-column gap-2 align-items-start px-2">
          <div class="dropdown w-100">
            <button 
              class="btn btn-md w-100 main-color-green main-color-white-text text-start dropdown-toggle"
              type="button"
              id="mobileUserDropdown"
              data-bs-toggle="dropdown"
              aria-expanded="false"
            >
              <i class="bi bi-person me-2"></i> 登入 / 註冊
            </button>
            <ul class="dropdown-menu w-100" aria-labelledby="mobileUserDropdown">
              <li><a class="dropdown-item" href="/login">登入</a></li>
              <li><a class="dropdown-item" href="/register">註冊</a></li>
              <li><hr class="dropdown-divider" /></li>
              <li><a class="dropdown-item" href="/profile">我的帳戶</a></li>
            </ul>
          </div>
          <button 
            @click="goToCart"
            class="btn btn-md w-100 main-color-green text-start position-relative"
          >
            <i class="bi bi-cart3 me-2 main-color-white-text"></i>
            <span class="main-color-white-text">購物車</span>
            <span
              v-if="cartCount > 0"
              class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger"
            >
              {{ cartCount }}
            </span>
          </button>
          <button class="btn btn-md w-100 main-color-green text-start">
            <i class="bi bi-bag me-2 main-color-white-text"></i>
            <span class="main-color-white-text">訂單</span>
          </button>
        </div>
      </div>
    </div>
  </header>
</template>

<script>
export default {
  name: 'AppHeader',
  data() {
    return {
      searchQuery: '',
      cartCount: 0,
    };
  },
  methods: {
    handleSearch() {
      if (this.searchQuery.trim()) {
        this.$router.push({
          name: 'search',
          query: { q: this.searchQuery },
        });
      }
    },
    goToCart() {
      this.$router.push('/cart');
    },
    async loadCartCount() {
      try {
        const response = await fetch('/ORD/CartTest/GetCartCount');
        if (response.ok) {
          this.cartCount = await response.json();
        }
      } catch (error) {
        console.error('載入購物車數量失敗:', error);
      }
    }
  },
  mounted() {
    this.loadCartCount();
    
    // 確保 Bootstrap Dropdown 初始化
    this.$nextTick(() => {
      // 強制初始化所有 dropdown
      const dropdownElementList = document.querySelectorAll('[data-bs-toggle="dropdown"]');
      if (window.bootstrap && window.bootstrap.Dropdown) {
        dropdownElementList.forEach(dropdownToggleEl => {
          new window.bootstrap.Dropdown(dropdownToggleEl);
        });
      }
    });
  }
};
</script>

<style scoped>
/* ✅ 搜尋按鈕與輸入框整合對齊 */
.search-container .search-btn {
  position: absolute;
  right: 10px;
  top: 50%;
  transform: translateY(-50%);
  width: 40px;
  height: 40px;
}

.main-header img {
  min-width: 120px; /* ✅ 避免被壓扁 */
}

.search-container {
  max-width: 800px;      /* 限制搜尋欄最長不超過 800px */
  width: 100%;           /* 小螢幕時可彈性縮小 */
  margin: 0 auto;        /* 置中 */
  position: relative;
  min-width: 200px;  /* 👉 給它下限 */
  flex: 1 1 auto;
}

@media (max-width: 1250px) {
  .main-header .row {
    display: flex;
    flex-wrap: wrap;         /* ✅ 讓搜尋欄換行 */
  }

  .main-header .search-col {
    order: 3;                /* ✅ 掉到第二行 */
    width: 100%;
    justify-content: center;
    margin-top: 10px;
  }

  .search-container {
    max-width: 600px;        /* ✅ 中螢幕縮短 */
  }
}

@media (max-width: 1100px) {
  .search-container {
    max-width: 500px;
  }
}

@media (max-width: 992px) {
  .search-container {
    max-width: 500px; /* 更小螢幕再縮短 */
  }
}

/* ✅ 小螢幕調整 */
@media (max-width: 768px) {
  .main-header img {
    max-height: 40px;
  }
  .btn {
    font-size: 0.9rem;
  }
  .search-container .search-btn {
    width: 35px;
    height: 35px;
  }
  .search-container input {
    font-size: 0.9rem;
  }
}
</style>