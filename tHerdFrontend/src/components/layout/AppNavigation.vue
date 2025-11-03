<!--
  AppNavigation.vue - 響應式導航組件
  特色：桌面版橫向導航 + 手機版漢堡選單
-->
<template>
  <nav class="main-navigation bg-white border-bottom">
    <div class="container-fluid">
      <div class="row align-items-center">
        <div class="col-12">
          <!-- 🍔 手機版漢堡按鈕 -->
          <button 
            class="hamburger-btn d-lg-none"
            @click="toggleMobileMenu"
            :class="{ active: showMobileMenu }"
          >
            <span></span>
            <span></span>
            <span></span>
          </button>

          <!-- 🖥️ 桌面版導航 -->
          <ul class="nav nav-pills justify-content-center flex-wrap py-2 d-none d-lg-flex">
            <li
              v-for="item in productMenus"
              :key="item.id"
              class="nav-item position-relative"
              @mouseenter="openMegaMenu(item)"
              @mouseleave="closeMegaMenu"
            >
              <router-link
                :to="item.path"
                class="nav-link fw-medium rounded-pill d-flex align-items-center"
                :class="{ 
                  active: $route.path.startsWith(item.path),
                  'has-icon': item.icon,
                  'text-only': !item.icon
                }"
              >
                <div v-if="item.icon" class="nav-icon-wrapper">
                  <img :src="item.icon" alt="" class="nav-icon" />
                </div>
                <span>{{ item.name }}</span>
              </router-link>
            </li>

            <!-- 品牌 A-Z -->
            <li
              class="nav-item position-relative mega-menu-container"
              @mouseenter="showBrands = true"
              @mouseleave="showBrands = false"
            >
              <button
                type="button"
                class="nav-link fw-medium rounded-pill border-0 bg-transparent d-flex align-items-center text-only"
                :class="{ active: showBrands }"
                @click="toggleBrands"
              >
                <span>品牌 A-Z</span>
              </button>

              <transition name="fade">
                <div 
                  v-if="showBrands" 
                  class="mega-menu shadow-lg bg-white"
                  @mouseenter="showBrands = true"
                  @mouseleave="showBrands = false"
                >
                  <div class="container-fluid py-4 px-4">
                    <div class="row g-4">
                      <div
                        class="col-6 col-md-2"
                        v-for="(group, gIdx) in brandGroups"
                        :key="gIdx"
                      >
                        <ul class="list-unstyled mb-0">
                          <li v-for="brand in group" :key="brand" class="mb-2">
                            <router-link
                              :to="`/brands/${brand.toLowerCase().replace(/\s+/g, '-')}`"
                              class="text-dark text-decoration-none brand-link"
                              @click="showBrands = false"
                            >
                              {{ brand }}
                            </router-link>
                          </li>
                        </ul>
                      </div>

                      <div class="col-12 col-md-4 border-start ps-4">
                        <h6 class="fw-bold text-success mb-3">
                          <i class="bi bi-star-fill me-2"></i>推薦品牌
                        </h6>
                        <div class="recommended-brands">
                          <router-link
                            v-for="rec in recommendedBrands"
                            :key="rec.name"
                            :to="rec.url"
                            class="recommended-brand-item"
                            @click="showBrands = false"
                          >
                            <i class="bi bi-award-fill text-warning me-2"></i>
                            <span class="fw-medium">{{ rec.name }}</span>
                            <i class="bi bi-chevron-right ms-auto"></i>
                          </router-link>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </transition>
            </li>
          </ul>

          <!-- ✅ 把 MegaMenu 放在 ul 外 -->
          <transition name="fade">
            <div 
              v-if="activeMenuId"
              class="mega-menu shadow-lg bg-white"
              @mouseenter="clearCloseTimer"
              @mouseleave="closeMegaMenu"
            >
              <div v-if="isLoadingMenu" class="p-4 text-center text-muted">載入中...</div>
              <div v-else-if="megaMenuData" class="container-fluid py-4 px-4">
                <div class="row g-4">
                  <div
                    v-for="col in megaMenuData.columns"
                    :key="col.title"
                    class="col-6 col-md-2"
                  >
                    <h6 class="fw-bold text-success mb-3">{{ col.title }}</h6>
                    <ul class="list-unstyled mb-0">
                      <li v-for="sub in col.items" :key="sub.id" class="mb-2">
                        <router-link
                          :to="sub.url"
                          class="text-dark text-decoration-none brand-link"
                          @click="closeMegaMenu"
                        >
                          {{ sub.name }}
                        </router-link>
                      </li>
                    </ul>
                  </div>
                </div>
              </div>
            </div>
          </transition>

          <!-- 📱 手機版側邊選單 -->
          <transition name="slide">
            <div v-if="showMobileMenu" class="mobile-menu">
              <div class="mobile-menu-header">
                <h5 class="mb-0 fw-bold text-white">選單</h5>
                <button class="close-btn" @click="closeMobileMenu">
                  <i class="bi bi-x-lg"></i>
                </button>
              </div>

              <div class="mobile-menu-body">
                <!-- 有圖標的分類 -->
                <div class="menu-section">
                  <h6 class="menu-section-title">產品分類</h6>
                  <router-link
                    v-for="item in navigationItemsWithIcon.filter(i => i.icon)"
                    :key="item.name"
                    :to="item.path"
                    class="mobile-menu-item"
                    :class="{ active: $route.path.startsWith(item.path) }"
                    @click="closeMobileMenu"
                  >
                    <div class="mobile-icon-wrapper">
                      <img :src="item.icon" alt="" />
                    </div>
                    <span>{{ item.name }}</span>
                    <i class="bi bi-chevron-right ms-auto"></i>
                  </router-link>
                </div>

                <!-- 無圖標的分類 -->
                <div class="menu-section">
                  <h6 class="menu-section-title">快速連結</h6>
                  <router-link
                    v-for="item in navigationItemsWithIcon.filter(i => !i.icon)"
                    :key="item.name"
                    :to="item.path"
                    class="mobile-menu-item text-only-item"
                    :class="{ active: $route.path.startsWith(item.path) }"
                    @click="closeMobileMenu"
                  >
                    <i class="bi bi-dot"></i>
                    <span>{{ item.name }}</span>
                    <i class="bi bi-chevron-right ms-auto"></i>
                  </router-link>
                </div>

                <!-- ✅ 品牌選單（修正版） -->
                <div class="menu-section">
                  <h6 
                    class="menu-section-title clickable" 
                    @click="toggleBrandsInMobile"
                  >
                    <span>品牌 A-Z</span>
                    <i 
                      class="bi" 
                      :class="showBrandsInMobile ? 'bi-chevron-up' : 'bi-chevron-down'"
                    ></i>
                  </h6>
                  
                  <transition name="expand">
                    <div v-if="showBrandsInMobile" class="brands-list">
                      <!-- ✅ 正確的雙層 v-for 結構 -->
                      <template v-for="(group, gIdx) in brandGroups" :key="`group-${gIdx}`">
                        <router-link
                          v-for="brand in group"
                          :key="brand"
                          :to="`/brands/${brand.toLowerCase().replace(/\s+/g, '-')}`"
                          class="brand-item"
                          @click="closeMobileMenu"
                        >
                          {{ brand }}
                        </router-link>
                      </template>
                    </div>
                  </transition>
                </div>
              </div>
            </div>
          </transition>

          <!-- 🎭 背景遮罩 -->
          <transition name="fade-mask">
            <div 
              v-if="showMobileMenu" 
              class="mobile-menu-overlay"
              @click="closeMobileMenu"
            ></div>
          </transition>
        </div>
      </div>
    </div>
  </nav>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount  } from 'vue'

// ==================== 狀態變數 ====================
const showMobileMenu = ref(false)
const showBrands = ref(false)
const showBrandsInMobile = ref(false)
const productMenus = ref([])
const activeMenuId = ref(null)
const megaMenuData = ref(null)
const isLoadingMenu = ref(false)
const loadedMenus = ref({}) // ✅ 預載快取資料

const navigationItemsWithIcon = [
        { name: '補充劑', type: 'pr', path: '/supplements', icon: '/homePageIcon/supplement.png' },
        { name: '運動營養', type: 'pr', path: '/sports-nutrition', icon: '/homePageIcon/sport.png' },
        { name: '沐浴', type: 'pr', path: '/bath', icon: '/homePageIcon/bath.png' },
        { name: '美容美妝', type: 'pr', path: '/beauty', icon: '/homePageIcon/makeup.png' },
        { name: '食品百貨', type: 'pr', path: '/grocery', icon: '/homePageIcon/food.png' },
        { name: '健康家居', type: 'pr', path: '/healthy-home', icon: '/homePageIcon/health.png' },
        { name: '嬰童用品', type: 'pr', path: '/baby-kids', icon: '/homePageIcon/baby.png' },
        { name: '寵物用品', type: 'pr', path: '/pet-supplies', icon: '/homePageIcon/pet.png' },
        { name: '健康主題', path: '/health-topics' },
        { name: '特惠', path: '/specials' },
        { name: '暢銷', path: '/bestsellers' },
        { name: '試用', path: '/trials' },
        { name: '新產品', path: '/new-products' },
        { name: '健康中心', path: '/cnt' },
      ]

// === 初始化 ===
onMounted(() => {
  productMenus.value = navigationItemsWithIcon
    .filter(i => i.type === 'pr')
    .map((item, index) => ({ ...item, id: `menu-${index + 1}` }))

  preloadMegaMenus() // 一次預載所有資料
})

// === 預先載入所有分類資料 ===
function preloadMegaMenus() {
  const sampleData = {
    columns: [
      {
        title: '熱門分類',
        items: [
          { id: 1, name: '維生素', url: '/category/vitamins' },
          { id: 2, name: '魚油', url: '/category/fishoil' },
          { id: 3, name: '益生菌', url: '/category/probiotics' },
        ],
      },
      {
        title: '品牌推薦',
        items: [
          { id: 4, name: "Nature’s Bounty", url: '/brands/natures-bounty' },
          { id: 5, name: 'NOW Foods', url: '/brands/now-foods' },
          { id: 6, name: 'Solgar', url: '/brands/solgar' },
        ],
      },
    ],
  }

  productMenus.value.forEach(menu => {
    loadedMenus.value[menu.id] = JSON.parse(JSON.stringify(sampleData))
  })
}

let closeTimer = null

// === 開關 MegaMenu ===
function openMegaMenu(item) {
  clearTimeout(closeTimer)
  activeMenuId.value = item.id
  megaMenuData.value = loadedMenus.value[item.id] // ✅ 直接讀快取，不用 loading
}

function closeMegaMenu() {
  clearTimeout(closeTimer)
  // 延遲一點再關閉，給滑鼠移動時間
  closeTimer = setTimeout(() => {
    activeMenuId.value = null
  }, 200)
}

function clearCloseTimer() {
  clearTimeout(closeTimer)
}

// 品牌清單
const brandGroups = [
  ['21st Century', 'ACURE', 'ALLMAX', 'Beauty of Joseon'],
  ["Doctor's Best", 'Eucerin', 'Fairhaven Health', 'Garden of Life'],
  ['Life Extension', 'MegaFood', 'NOW Foods', "Nature's Bounty"],
  ['Solgar', 'Thorne', 'Vital Proteins', 'The Vitamin Shoppe']
]

const recommendedBrands = [
  { name: "Nature's Bounty", url: '/brands/natures-bounty' },
  { name: '21st Century', url: '/brands/21st-century' },
  { name: 'Fairhaven Health', url: '/brands/fairhaven-health' }
]

// ==================== 手機選單 ====================
function toggleMobileMenu() {
  showMobileMenu.value = !showMobileMenu.value
  document.body.style.overflow = showMobileMenu.value ? 'hidden' : ''
}

function closeMobileMenu() {
  showMobileMenu.value = false
  document.body.style.overflow = ''
}

// ==================== 品牌選單 ====================
function toggleBrands() {
  showBrands.value = !showBrands.value
}
function toggleBrandsInMobile() {
  showBrandsInMobile.value = !showBrandsInMobile.value
}

// 關閉前清理滾動鎖定
onBeforeUnmount(() => {
  document.body.style.overflow = ''
})

</script>

<style scoped>
@import url('https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.0/font/bootstrap-icons.css');

.main-navigation {
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  position: relative;
}

/* 🍔 漢堡按鈕 */
.hamburger-btn {
  position: fixed;
  top: 15px;
  left: 15px;
  z-index: 10000;
  width: 50px;
  height: 50px;
  background: rgb(77, 180, 193);
  border: none;
  border-radius: 12px;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  gap: 6px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  transition: all 0.3s ease;
}

.hamburger-btn:hover {
  background: rgb(0, 112, 131);
  transform: scale(1.05);
}

.hamburger-btn span {
  width: 28px;
  height: 3px;
  background: white;
  border-radius: 2px;
  transition: all 0.3s ease;
}

.hamburger-btn.active span:nth-child(1) {
  transform: rotate(45deg) translate(8px, 8px);
}

.hamburger-btn.active span:nth-child(2) {
  opacity: 0;
}

.hamburger-btn.active span:nth-child(3) {
  transform: rotate(-45deg) translate(8px, -8px);
}

/* 📱 手機版側邊選單 */
.mobile-menu {
  position: fixed;
  top: 0;
  left: 0;
  width: 85%;
  max-width: 350px;
  height: 100vh;
  background: white;
  z-index: 9999;
  box-shadow: 4px 0 20px rgba(0, 0, 0, 0.2);
  overflow-y: auto;
  display: flex;
  flex-direction: column;
}

.mobile-menu-header {
  background: linear-gradient(135deg, rgb(77, 180, 193), rgb(0, 112, 131));
  padding: 20px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  position: sticky;
  top: 0;
  z-index: 10;
}

.close-btn {
  background: rgba(255, 255, 255, 0.2);
  border: none;
  color: white;
  width: 40px;
  height: 40px;
  border-radius: 50%;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.2rem;
  transition: all 0.3s ease;
}

.close-btn:hover {
  background: rgba(255, 255, 255, 0.3);
  transform: rotate(90deg);
}

.mobile-menu-body {
  flex: 1;
  padding: 15px;
  overflow-y: auto;
}

/* 選單區塊 */
.menu-section {
  margin-bottom: 25px;
}

.menu-section-title {
  color: rgb(0, 112, 131);
  font-size: 0.85rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1px;
  padding: 10px 15px 5px;
  margin-bottom: 5px;
  border-bottom: 2px solid rgb(77, 180, 193);
  display: flex;
  justify-content: space-between;
  align-items: center;
  cursor: pointer;
}

.mobile-menu-item {
  display: flex;
  align-items: center;
  padding: 12px 15px;
  margin-bottom: 5px;
  border-radius: 10px;
  text-decoration: none;
  color: #333;
  transition: all 0.3s ease;
  gap: 12px;
}

.mobile-menu-item:hover,
.mobile-menu-item.active {
  background: linear-gradient(135deg, #e8f5e9, #c8e6c9);
  color: rgb(0, 112, 131);
  transform: translateX(5px);
}

.mobile-icon-wrapper {
  width: 35px;
  height: 35px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.mobile-icon-wrapper img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.text-only-item {
  padding-left: 10px;
}

/* 品牌列表 */
.brands-list {
  padding: 10px 0;
}

.brand-item {
  display: block;
  padding: 10px 20px;
  color: #666;
  text-decoration: none;
  font-size: 0.95rem;
  transition: all 0.2s ease;
  border-radius: 6px;
  margin-bottom: 3px;
}

.brand-item:hover {
  background: #f0f8ff;
  color: rgb(77, 180, 193);
  padding-left: 30px;
}

/* 背景遮罩 */
.mobile-menu-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(0, 0, 0, 0.5);
  z-index: 9998;
  backdrop-filter: blur(3px);
}

/* 🖥️ 桌面版樣式（保持原樣） */
.nav-pills {
  align-items: center !important;
}

.nav-item {
  display: flex;
  align-items: center;
}

.nav-link {
  color: rgb(0, 112, 131) !important;
  transition: all 0.3s ease;
  font-size: 1.2rem;
  display: flex !important;
  align-items: center !important;
  padding: 0.5rem 1rem;
  min-height: 52px;
}

.nav-link:hover {
  background-color: #e8f5e8;
  color: rgb(0, 112, 131) !important;
}

.nav-link.active {
  background-color: rgb(77, 180, 193);
  color: white !important;
}

.nav-link.has-icon {
  gap: 0.5rem;
  padding-left: 0.75rem;
  padding-right: 1rem;
}

.nav-link.text-only {
  padding-left: 1rem;
  padding-right: 1rem;
}

.nav-icon-wrapper {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.nav-icon {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.nav-link span {
  line-height: 1;
  white-space: nowrap;
}

/* Mega Menu（桌面版） */
.mega-menu-container {
  position: static !important;
}

.mega-menu {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  z-index: 9999;
  width: 100vw;
  max-height: 80vh;
  overflow-y: auto;
  border-top: 3px solid rgb(77, 180, 193);
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.brand-link {
  display: block;
  padding: 0.4rem 0.5rem;
  font-size: 0.95rem;
  transition: all 0.2s ease;
  border-radius: 4px;
}

.brand-link:hover {
  background-color: #f0f8ff;
  color: rgb(77, 180, 193) !important;
  padding-left: 1rem;
}

.recommended-brands {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.recommended-brand-item {
  display: flex;
  align-items: center;
  padding: 0.75rem 1rem;
  background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
  border-radius: 8px;
  text-decoration: none;
  color: #212529;
  transition: all 0.3s ease;
  border: 2px solid transparent;
}

.recommended-brand-item:hover {
  background: linear-gradient(135deg, #e8f5e9 0%, #c8e6c9 100%);
  border-color: rgb(77, 180, 193);
  transform: translateX(5px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
  color: rgb(0, 112, 131);
}

/* 🎬 動畫 */
.slide-enter-active,
.slide-leave-active {
  transition: transform 0.3s ease;
}

.slide-enter-from {
  transform: translateX(-100%);
}

.slide-leave-to {
  transform: translateX(-100%);
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease, transform 0.3s ease;
}

.fade-enter-from {
  opacity: 0;
  transform: translateY(-10px);
}

.fade-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}

.fade-mask-enter-active,
.fade-mask-leave-active {
  transition: opacity 0.3s ease;
}

.fade-mask-enter-from,
.fade-mask-leave-to {
  opacity: 0;
}

.expand-enter-active,
.expand-leave-active {
  transition: all 0.3s ease;
  max-height: 500px;
  overflow: hidden;
}

.expand-enter-from,
.expand-leave-to {
  max-height: 0;
  opacity: 0;
}

.main-navigation .container-fluid {
  max-width: 1200px;
  margin: 0 auto;
  transition: all 0.3s ease;
}


/* 📱 響應式 */
@media (min-width: 992px) {
  .hamburger-btn {
    display: none;
  }
  .main-navigation .container-fluid {
    max-width: 100%;
    padding-left: 0;
    padding-right: 0;
  }
}

@media (max-width: 991px) {
  .nav-pills {
    display: none !important;
  }
}
</style>