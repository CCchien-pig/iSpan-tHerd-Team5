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
              class="nav-item position-relative mega-menu-container"
            >
              <!-- 🔸 改成 button，不用 router-link -->
              <button
                type="button"
                class="nav-link fw-medium rounded-pill border-0 bg-transparent d-flex align-items-center"
                :class="{
                  active: activeMenuId === item.id,
                  'has-icon': item.icon,
                  'text-only': !item.icon,
                }"
                @click="goCategory(item)"
              >
                <div v-if="item.icon" class="nav-icon-wrapper">
                  <img :src="item.icon" alt="" class="nav-icon" />
                </div>
                <span>{{ item.name }}</span>
              </button>
            </li>

            <li class="nav-divider mx-3"></li>

            <!-- 品牌 A-Z -->
            <li
              class="nav-item position-relative mega-menu-container"
              @mouseenter="showBrands = true"
              @mouseleave="showBrands = false"
            >
              <router-link
                to="/brands"
                class="nav-link fw-medium rounded-pill d-flex align-items-center text-only"
                :class="{ active: showBrands }"
                @click="showBrands = false"
              >
                <span>品牌 A-Z</span>
              </router-link>

              <!-- hover 展開的 mega menu -->
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
                        :key="`grp-${gIdx}`"
                      >
                        <ul class="list-unstyled mb-0">
                          <li v-for="b in group" :key="b.brandId" class="mb-2">
                            <router-link
                              :to="toBrandPath(b.brandName, b.brandId)"
                              class="text-dark text-decoration-none brand-link"
                              @click="showBrands = false"
                            >
                              {{ b.brandName }}
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
            <!-- ✅ 固定項目（品牌A-Z後面） -->
            <li v-for="item in staticMenus" :key="item.path" class="nav-item">
              <button
                type="button"
                class="nav-link fw-medium rounded-pill text-only bg-transparent border-0"
                :class="[
                  { active: $route.path.startsWith(item.path) },
                  item.name === '特惠' ? 'text-danger fw-bold' : ''  // 🔹 特惠顯示紅字加粗
                ]"
                @click="goStaticMenu(item)"
              >
                {{ item.name }}
              </button>
            </li>
          </ul>

          <!-- ✅ 使用抽離後的 MegaMenu 元件 -->
          <MegaMenu
            :visible="!!activeMenuId"
            :isLoading="isLoadingMenu"
            :data="megaMenuData"
            @mouseenter="clearCloseTimer"
            @mouseleave="closeMegaMenu"
            @close="closeMegaMenu"
          />
          
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
                    v-for="item in navigationItemsWithIcon.filter((i) => i.icon)"
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
                    v-for="item in navigationItemsWithIcon.filter((i) => !i.icon)"
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
                  <!-- 設定到總覽頁路由 -->
                  <router-link
                    to="/brands"
                    class="menu-section-title no-underline d-flex justify-content-between align-items-center"
                    @click="closeMobileMenu"
                  >
                    <span>品牌 A-Z</span>
                    <i class="bi bi-chevron-right"></i>
                  </router-link>

                  <!-- 展開清單 -->
                  <!-- <transition name="expand">
                    <div v-if="showBrandsInMobile" class="brands-list">
                      <template v-for="(group, gIdx) in brandGroups" :key="`group-${gIdx}`">
                        <router-link
                          v-for="b in group"
                          :key="b.brandId"
                          :to="toBrandPath(b.brandName, b.brandId)"
                          class="brand-item"
                          @click="closeMobileMenu"
                        >
                          {{ b.brandName }}
                        </router-link>
                      </template>
                    </div>
                  </transition> -->
                </div>
              </div>
            </div>
          </transition>

          <!-- 🎭 背景遮罩 -->
          <transition name="fade-mask">
            <div v-if="showMobileMenu" class="mobile-menu-overlay" @click="closeMobileMenu"></div>
          </transition>
        </div>
      </div>
    </div>
  </nav>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import ProductsApi from '@/api/modules/prod/ProductsApi'
import MegaMenu from '@/components/modules/prod/menu/MegaMenu.vue'
import { useCartStore } from '@/composables/modules/prod/cartStore'

// ==================== 狀態變數 ====================
const router = useRouter()
const showMobileMenu = ref(false)
const showBrands = ref(false)
const showBrandsInMobile = ref(false)
const productMenus = ref([])
const activeMenuId = ref(null)
const megaMenuData = ref(null)
const isLoadingMenu = ref(false)
const loadedMenus = ref({})
const cartStore = useCartStore()

const navigationItemsWithIcon = [
        { name: '補充劑', type: 'pr', path: '/supplements', icon: '/homePageIcon/supplement.png', productTypeId: 2785 },
        { name: '運動營養', type: 'pr', path: '/sports-nutrition', icon: '/homePageIcon/sport.png', productTypeId: 1143 },
        { name: '沐浴', type: 'pr', path: '/bath', icon: '/homePageIcon/bath.png', productTypeId: 2786 },
        { name: '美容美妝', type: 'pr', path: '/beauty', icon: '/homePageIcon/makeup.png', productTypeId: 1410 },
        { name: '食品百貨', type: 'pr', path: '/grocery', icon: '/homePageIcon/food.png', productTypeId: 1225 },
        { name: '健康家居', type: 'pr', path: '/healthy-home', icon: '/homePageIcon/health.png', productTypeId: 1160 },
        { name: '嬰童用品', type: 'pr', path: '/baby-kids', icon: '/homePageIcon/baby.png', productTypeId: 1204 },
        { name: '寵物用品', type: 'pr', path: '/pet-supplies', icon: '/homePageIcon/pet.png', productTypeId: 1007 },
      ]

// 品牌A-Z後的固定連結
const staticMenus = [
  //{ name: '健康主題', path: '/health-topics' },
  { name: '特惠', path: '/specials' },
  { name: '暢銷', path: '/bestsellers' },
  { name: '試用', path: '/trials' },
  { name: '新產品', path: '/new-products' },
  { name: '健康中心', path: '/cnt' },
]

// === 初始化 ===
onMounted(async () => {
  // ① 導覽初始化
  productMenus.value = navigationItemsWithIcon
    .filter(i => i.type === 'pr')
    .map((item, index) => ({
      ...item,
      id: `menu-${index + 1}`,
      productTypeCode: item.path.replace('/', '')
    }))

  // ② 更新購物車紅點
  setTimeout(async () => {
    await cartStore.refreshCartCount()
  }, 100)
})

// ==================== 點擊分類載入商品標籤 ====================
function goStaticMenu(item) {
  // 特殊處理：點擊「特惠」時轉到商品搜尋頁
  if (item.path === '/specials') {
    router.push({
      path: '/prod/products/search',
      query: { badge: 'discount' }
    })
  }
  else if (item.path === '/bestsellers') {
    router.push({
      path: '/prod/products/search',
      query: { other: 'Hot' }
    })
  }
  else if (item.path === '/trials') {
    router.push({
      path: '/prod/products/search',
      query: { badge: 'try' }
    })
  }
  else if (item.path === '/new-products') {
    router.push({
      path: '/prod/products/search',
      query: { badge: 'new' }
    })
  }
  else {
    // 其他項目維持原行為
    router.push(item.path)
  }
}

// ==================== 點擊分類載入 MegaMenu ====================
let lastClickedId = null
async function goCategory(item) {
  // 第一次點：打開 MegaMenu
  if (activeMenuId.value !== item.id) {
    activeMenuId.value = item.id
    await loadMegaMenuByCategory(item)
    lastClickedId = item.id
    return
  }

  // 第二次點相同分類 → 直接導向分類搜尋頁
  if (activeMenuId.value === item.id && lastClickedId === item.id) {
    router.push({
      name: 'product-type-search',
      params: {
        productTypeCode: item.productTypeCode,
        productTypeId: item.productTypeId
      }
    })
  }
}

// 動態載入該分類與子分類
async function loadMegaMenuByCategory(item) {
  try {
    isLoadingMenu.value = true
    const res = await ProductsApi.getProductCategoriesByTypeId(item.productTypeId)

    const apiData = res?.data
    const treeData = Array.isArray(apiData?.data)
      ? apiData.data
      : Array.isArray(apiData)
      ? apiData
      : []

    if (!treeData.length) {
      console.warn('⚠️ 無分類資料', apiData)
      megaMenuData.value = { columns: [] }
      return
    }

    const columns = buildMegaMenu(treeData)
    megaMenuData.value = { columns }
    loadedMenus.value[item.id] = { columns }
  } catch (err) {
    console.error(`❌ 無法載入 ${item.name} 的分類資料：`, err)
  } finally {
    isLoadingMenu.value = false
  }
}

// ==================== 樹狀資料轉換 ====================
function buildMegaMenu(treeData) {
  function buildUrl(item) {
    const code = (item.productTypeCode || '').trim().toLowerCase()
    item.url = `/products/${code || 'id'}-${item.productTypeId}`
    if (Array.isArray(item.children) && item.children.length) {
      item.children.forEach(buildUrl)
    }
  }

  return treeData.map(parent => {
    return {
      id: parent.productTypeId,
      productTypeCode: parent.productTypeCode,
      title: parent.productTypeName,
      url: `/products/${parent.productTypeCode?.toLowerCase() || ''}-${parent.productTypeId}`,
      items: (parent.children || []).map(child => ({
        id: child.productTypeId,
        productTypeCode: child.productTypeCode,
        name: child.productTypeName,
        url: `/products/${child.productTypeCode?.toLowerCase() || ''}-${child.productTypeId}`,
      })),
    }
  })
}

// ==================== 關閉 MegaMenu ====================
let closeTimer = null
function closeMegaMenu() {
  clearTimeout(closeTimer)
  closeTimer = setTimeout(() => {
    activeMenuId.value = null
    megaMenuData.value = null
  }, 250)
}
function clearCloseTimer() {
  clearTimeout(closeTimer)
}

// 品牌清單
const brandGroups = [
  [
    { brandId: 1002, brandName: 'Animal' },
    { brandId: 1005, brandName: 'Bioschwartz' },
    { brandId: 1008, brandName: 'Codeage' },
    { brandId: 1010, brandName: 'Dr. Mercola' },
    { brandId: 1013, brandName: 'Eucerin' },
  ],
  [
    { brandId: 1016, brandName: 'Force Factor' },
    { brandId: 1019, brandName: 'Garden Of Life' },
    { brandId: 1022, brandName: 'Healths Harmony' },
    { brandId: 1024, brandName: 'Irwin Naturals' },
    { brandId: 1025, brandName: 'Idealove' },
  ],
  [
    { brandId: 1027, brandName: 'Jarrow formulas' },
    { brandId: 1035, brandName: 'Lake Avenue Nutrition' },
    { brandId: 1038, brandName: 'Mild By Nature' },
    { brandId: 1039, brandName: 'Natural Factors' },
    { brandId: 1042, brandName: 'Optimum Nutrition' },
  ],
  [
    { brandId: 1054, brandName: 'Solaray' },
    { brandId: 1058, brandName: 'Trace' },
    { brandId: 1061, brandName: 'Vitamatic' },
    { brandId: 1064, brandName: "Wiley's Finest" },
    { brandId: 1072, brandName: 'Zahler' },
  ],
]

const recommendedBrands = [
  { name: 'Frontier Co-op', url: '/brands/frontier-co-op-1017' },
  { name: 'Garden Of Life', url: '/brands/garden-of-life-1019' },
  { name: 'Life Extension', url: '/brands/life-extension-1033' },
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

function goBrandsAndClose() {
  this.$router.push('/brands')
  this.closeMobileMenu()
}

function toBrandPath(name, id) {
  const slug = String(name || '')
    .trim()
    .toLowerCase()
    .replace(/\s+/g, '-')
    .replace(/&/g, 'and')
    .replace(/[^a-z0-9-]/g, '')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '')
  return id ? `/brands/${slug}-${id}` : `/brands/${slug}`
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

.nav-link.text-danger {
  color: #dc3545 !important; /* Bootstrap 的紅色 */
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
  left: 50%;
  transform: translateX(-50%) translateY(0px);
  z-index: 9999;
  
  background: #fff;
  border-radius: 8px;
  width: 100%;
  max-width: 1200px;
  max-height: 400px;
  overflow-y: auto;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  border-top: 3px solid rgb(77, 180, 193);
  pointer-events: auto;
  transition: opacity 0.35s ease, transform 0.35s ease; /* 只針對透明與位移做動畫 */
}

/* 當顯示時 */
.mega-menu.show,
.mega-menu[style*="display: block"] {
  transform: translateX(-50%) translateY(0);
  pointer-events: auto;
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
  transition:
    opacity 0.3s ease,
    transform 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
  transform: translate(-50%, 10px); /* 固定X方向居中，僅在Y軸移動 */
}

.fade-enter-to,
.fade-leave-from {
  opacity: 1;
  transform: translate(-50%, 0); /* 確保X方向不變 */
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
