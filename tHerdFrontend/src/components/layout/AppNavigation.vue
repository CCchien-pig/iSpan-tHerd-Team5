<!--
  AppNavigation.vue - 主導航組件
  功能：展示主要導航菜單，包含產品分類和功能頁面
  特色：響應式設計、動態高亮、Mega Menu
  用途：作為所有頁面的主要導航區域
-->
<template>
  <nav class="main-navigation bg-white border-bottom">
    <div class="container-fluid">
      <div class="row">
        <div class="col-12">
          <ul class="nav nav-pills justify-content-center flex-wrap py-2">
            <!-- 🔸 一般導航項目 -->
            <li
              v-for="item in navigationItemsWithIcon"
              :key="item.name"
              class="nav-item position-relative"
            >
              <router-link
              :to="item.path"
              class="nav-link fw-medium rounded-pill d-flex align-items-center gap-2"
              :class="{ active: $route.path.startsWith(item.path) }"
            >
              <div class="nav-icon-wrapper">
                <img v-if="item.icon" :src="item.icon" alt="" class="nav-icon" />
              </div>
              <span>{{ item.name }}</span>
            </router-link>

            </li>
            <!-- 🏷 品牌 A-Z Mega Menu -->
            <li
              class="nav-item position-relative "
              @mouseenter="showBrands = true"
              @mouseleave="showBrands = false"
            >
              <button
                type="button"
                class="nav-link fw-medium rounded-pill border-0 bg-transparent d-flex align-items-center gap-2"
                :class="{ active: showBrands }"
                @click="toggleBrands"
              >
                <div class="nav-icon-wrapper"></div>
                <span>品牌 A-Z</span>
              </button>

              <transition name="fade">
                <div v-if="showBrands" class="mega-menu shadow-lg bg-white p-4">
                  <div
                    class="container"
                    @mouseenter="showBrands = true"
                    @mouseleave="showBrands = false"
                  >
                    <div class="row">
                      <div
                        class="col-6 col-md-3"
                        v-for="(group, gIdx) in brandGroups"
                        :key="gIdx"
                      >
                        <ul class="list-unstyled">
                          <li
                            v-for="brand in group"
                            :key="brand"
                            class="mb-2"
                          >
                            <router-link
                              :to="`/brands/${brand.toLowerCase()}`"
                              class="text-dark text-decoration-none"
                            >
                              {{ brand }}
                            </router-link>
                          </li>
                        </ul>
                      </div>
                      <div class="col-12 col-md-3 border-start">
                        <h6 class="fw-bold text-success">推薦品牌</h6>
                        <div
                          v-for="rec in recommendedBrands"
                          :key="rec.name"
                          class="mb-3"
                        >
                          <img
                            :src="rec.logo"
                            alt=""
                            class="img-fluid mb-1"
                            style="max-height: 40px"
                          />
                          <div class="small">{{ rec.name }}</div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </transition>
            </li>
          </ul>
        </div>
      </div>
    </div>
  </nav>
</template>

<script>
export default {
  name: 'AppNavigation',
  data() {
    return {
      // 🔸 一般導航項目（不含品牌 A-Z）
      navigationItemsWithIcon: [
        { name: '補充劑', path: '/supplements', icon: '/homePageIcon/supplement.png' },
        { name: '運動營養', path: '/sports-nutrition', icon: '/homePageIcon/sport.png' },
        { name: '沐浴', path: '/bath', icon: '/homePageIcon/bath.png' },
        { name: '美容美妝', path: '/beauty', icon: '/homePageIcon/makeup.png' },
        { name: '食品百貨', path: '/grocery', icon: '/homePageIcon/food.png' },
        { name: '健康家居', path: '/healthy-home', icon: '/homePageIcon/health.png' },
        { name: '嬰童用品', path: '/baby-kids', icon: '/homePageIcon/baby.png' },
        { name: '寵物用品', path: '/pet-supplies', icon: '/homePageIcon/pet.png' },
        { name: '健康主題', path: '/health-topics' },
        { name: '特惠', path: '/specials' },
        { name: '暢銷', path: '/bestsellers' },
        { name: '試用', path: '/trials' },
        { name: '新產品', path: '/new-products' },
        { name: '健康中心', path: '/health-hub' },
      ],
      
      // 🏷 Mega Menu 狀態
      showBrands: false,

      // 📦 品牌清單分組
      brandGroups: [
        ['21st Century', 'ACURE', 'ALLMAX', 'Beauty of Joseon'],
        ["Doctor's Best", 'Eucerin', 'Fairhaven Health', 'Garden of Life'],
        ['Life Extension', 'MegaFood', 'NOW Foods', "Nature's Bounty"],
        ['Solgar', 'Thorne', 'Vital Proteins', 'The Vitamin Shoppe'],
      ],

      // 🏆 推薦品牌
      recommendedBrands: [
        { name: "Nature's Bounty", logo: 'https://via.placeholder.com/80x40' },
        { name: '21st Century', logo: 'https://via.placeholder.com/80x40' },
        { name: 'Fairhaven', logo: 'https://via.placeholder.com/80x40' },
      ],
    };
  },
  methods: {
    toggleBrands() {
      // 手機點擊時用來開/關 mega menu
      this.showBrands = !this.showBrands;
    },
  },
};
</script>

<style scoped>
.mega-menu {
  top: 100%;
  left: 0;
  z-index: 1050;
  width: 100%;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

.main-navigation {
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.nav-link {
  color: rgb(0, 112, 131) !important;
  transition: all 0.3s ease;
  font-size: 1.2rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding-top: 0.25rem;
  padding-bottom: 0.25rem;
}

.nav-link:hover {
  background-color: #e8f5e8;
  color: rgb(0, 112, 131) !important;
}

.nav-link.active {
  background-color: rgb(77, 180, 193);
  color: white !important;
}

.nav-icon-wrapper {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0; /* 避免被壓縮 */
}

.nav-icon {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.nav-link span {
  line-height: 1;  /* 🔸 確保文字不撐高 */
  display: inline-block;
}

/* 📱 RWD */
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
    font-size: 1.7rem;
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
  .nav-link{
    font-size: 1.5rem;
  }
}
</style>
