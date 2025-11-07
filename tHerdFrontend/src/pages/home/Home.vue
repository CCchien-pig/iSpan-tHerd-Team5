<!--
  Home.vue - 首頁組件
  功能：展示電商網站的首頁內容，包括英雄區塊、產品分類、熱銷產品等
  特色：完全組件化設計，每個區塊都是獨立的可重用組件
-->
<script setup>
// ===== 導入依賴 =====
// 導入Pinia狀態管理 - 用於控制全站Loading狀態
import { useLoadingStore } from '@/stores/loading'
// 導入 Vue Router
import { RouterLink } from 'vue-router'

// 導入產品相關組件
import ProductShowcase from '@/components/modules/prod/list/ProductShowcase.vue'

// 導入頁面區塊組件 - 每個區塊都是獨立的組件
import HeroSection from '@/components/sections/HeroSection.vue' // 英雄區塊
import CategorySection from '@/components/sections/CategorySection.vue' // 分類區塊
import BrandSection from '@/components/sections/BrandSection.vue' // 品牌展示
import TestimonialSection from '@/components/sections/TestimonialSection.vue' // 客戶評價
import FeatureSection from '@/components/sections/FeatureSection.vue' // 服務特色

//優惠券
import CouponList from '@/components/modules/mkt/CouponList.vue'

//彈出式廣告
import AdPopup from '@/components/modules/mkt/AdPopup.vue'

//遊戲

//輪播圖
// import HeroCarousel from '@/components/common/HeroCarousel.vue'
// ===== 狀態管理 =====
// 獲取Loading狀態管理實例
const loadingStore = useLoadingStore()

// ===== Loading測試功能 =====
/**
 * 測試基本Loading功能
 * 用途：演示Loading組件的使用方式
 * 時長：3秒
 */
const testLoading = () => {
  loadingStore.showLoading('測試載入中...')
  setTimeout(() => {
    loadingStore.hideLoading()
  }, 3000)
}

/**
 * 測試自定義文字Loading功能
 * 用途：演示Loading組件支持自定義文字
 * 時長：2秒
 */
const testLoadingWithCustomText = () => {
  loadingStore.showLoading('正在處理您的請求...')
  setTimeout(() => {
    loadingStore.hideLoading()
  }, 2000)
}

/**
 * 包裝函數 - 用於模板中調用自定義Loading
 * 用途：將內部函數暴露給模板使用
 */
const handleCustomLoading = () => {
  testLoadingWithCustomText()
}

// ===== 產品相關事件處理 =====
/**
 * 處理加入購物車事件
 * @param {Object} product - 產品對象
 * @param {string} product.name - 產品名稱
 * @param {number} product.price - 產品價格
 * TODO: 實際項目中需要連接到購物車API
 */
const handleAddToCart = (product) => {
  console.log('加入購物車:', product.name)
  // 這裡可以添加加入購物車的邏輯
  // 例如：調用購物車API、更新購物車狀態、顯示成功提示等
}

/**
 * 處理收藏/取消收藏事件
 * @param {Object} product - 產品對象
 * @param {boolean} isInWishlist - 是否在收藏清單中
 * TODO: 實際項目中需要連接到收藏API
 */
const handleToggleWishlist = (product, isInWishlist) => {
  console.log('收藏狀態:', product.name, isInWishlist ? '已收藏' : '取消收藏')
  // 這裡可以添加收藏邏輯
  // 例如：調用收藏API、更新收藏狀態、顯示提示訊息等
}

/**
 * 處理快速查看事件
 * @param {Object} product - 產品對象
 * TODO: 實際項目中需要實現快速查看功能
 */
const handleQuickView = (product) => {
  console.log('快速查看:', product.name)
  // 這裡可以添加快速查看邏輯
  // 例如：打開產品詳情彈窗、導航到產品詳情頁等
}
</script>

<template>
  <!-- 首頁容器 - 使用組件化設計，每個區塊都是獨立的組件 -->
  <div class="homepage">
    <AdPopup imageUrl="/images/Ad/Ad1099-FreeFee.png" />
    <!--
      英雄區塊 - 首頁主要展示區域
      功能：展示網站主要價值主張和行動按鈕
      事件：支持Loading測試功能
    -->
      <!-- <HeroCarousel /> -->
    <HeroSection @test-loading="testLoading" @custom-loading="handleCustomLoading" />

    <!--
  優惠券區塊 - 展示可領取的優惠券
  功能：顯示使用者可領取的各種活動券
  -->
  <div class="container py-4">
    <h3 class="mb-3 fw-bold text-center main-color-green-text">熱門優惠券</h3>
    <CouponList />
  </div>
    <!--
      特色分類區塊 - 展示產品分類
      功能：幫助用戶快速找到感興趣的產品類別
      數據：categories數組，包含分類名稱、描述和圖標
    -->
    <CategorySection :categories="categories" />

    <!--
      熱銷產品區塊 - 展示推薦產品
      功能：展示精選的熱銷產品，促進銷售轉換
      事件：支持購物車、收藏、快速查看等操作
      組件：使用ProductShowcase組件，內部包含多個ProductCard
    -->
    <ProductShowcase
      title="熱銷產品"
      :products="featuredProducts"
      view-all-text="查看全部"
      @add-to-cart="handleAddToCart"
      @toggle-wishlist="handleToggleWishlist"
      @quick-view="handleQuickView"
    />

    <!--
      品牌展示區塊 - 展示合作品牌
      功能：建立品牌信任度，展示合作夥伴
      數據：brands數組，包含品牌名稱和Logo
    -->
    <BrandSection :brands="brands" />

  

    <!--
      客戶評價區塊 - 展示用戶評價
      功能：建立社會證明，增加用戶信任度
      數據：testimonials數組，包含用戶頭像、姓名、評價內容
    -->
    <TestimonialSection :testimonials="testimonials" />

    <!--
      服務特色區塊 - 展示服務優勢
      功能：突出服務特色，建立競爭優勢
      數據：features數組，包含特色標題、描述和圖標
    -->
    <FeatureSection :features="features" />
  </div>
</template>

<script>
/**
 * Home.vue 組件配置
 * 使用Options API風格，便於新人理解
 */
export default {
  name: 'HomeView', // 組件名稱，用於Vue DevTools調試

  /**
   * 組件數據 - 包含首頁所需的所有靜態數據
   * 注意：實際項目中這些數據通常來自API
   */
  data() {
    return {
      // ===== 產品分類數據 =====
      // 用途：展示網站主要產品分類，幫助用戶導航
      // 結構：每個分類包含ID、名稱、描述和Bootstrap圖標
      // TODO: 更新為API數據
      categories: [
        {
          id: 1,
          name: '維生素與礦物質',
          description: '支持免疫系統和整體健康',
          icon: 'bi bi-heart-pulse',
        },
        {
          id: 2,
          name: '運動營養',
          description: '提升運動表現和恢復',
          icon: 'bi bi-trophy',
        },
        {
          id: 3,
          name: '美容護膚',
          description: '天然有機的美容產品',
          icon: 'bi bi-flower1',
        },
        {
          id: 4,
          name: '健康家居',
          description: '打造健康的生活環境',
          icon: 'bi bi-house-heart',
        },
      ],

      // ===== 服務特色數據 =====
      // 用途：展示網站的核心服務優勢，建立用戶信任
      // 結構：每個特色包含ID、標題、描述和Bootstrap圖標
      // TODO: 更新為API數據
      features: [
        {
          id: 1,
          title: '100% 正品保證',
          description: '所有產品均為正品，品質有保障',
          icon: 'bi bi-shield-check',
        },
        {
          id: 2,
          title: '快速配送',
          description: '訂單滿 NT$1,500 免運費',
          icon: 'bi bi-truck',
        },
        {
          id: 3,
          title: '24/7 客服',
          description: '專業客服團隊隨時為您服務',
          icon: 'bi bi-headset',
        },
        {
          id: 4,
          title: '7天退貨',
          description: '不滿意可於7天內退貨',
          icon: 'bi bi-arrow-clockwise',
        },
      ],

      // ===== 熱銷產品數據 =====
      // 用途：展示精選的推薦產品，促進銷售轉換
      // 結構：每個產品包含完整的產品信息，用於ProductCard組件
      // TODO: 更新為API數據
      featuredProducts: [
        {
          id: 1,
          name: '維生素D3 5000 IU',
          image: 'https://picsum.photos/300/300?random=1',
          price: 299,
          originalPrice: 399,
          badge: '熱銷',
          reviews: 1250,
        },
        {
          id: 2,
          name: '魚油 Omega-3',
          image: 'https://picsum.photos/300/300?random=2',
          price: 599,
          originalPrice: 799,
          badge: '新品',
          reviews: 890,
        },
        {
          id: 3,
          name: '維生素C 1000mg',
          image: 'https://picsum.photos/300/300?random=3',
          price: 199,
          originalPrice: null,
          badge: null,
          reviews: 2100,
        },
        {
          id: 4,
          name: '蛋白粉 乳清蛋白',
          image: 'https://picsum.photos/300/300?random=4',
          price: 1299,
          originalPrice: 1599,
          badge: '特價',
          reviews: 756,
        },
      ],

      // ===== 合作品牌數據 =====
      // 用途：展示合作夥伴品牌，建立品牌信任度
      // 結構：每個品牌包含名稱和Logo圖片URL
      // TODO: 更新為API數據
      brands: [
        {
          name: 'Life Extension',
          logo: 'https://picsum.photos/150/80?random=5',
        },
        {
          name: 'Now Foods',
          logo: 'https://picsum.photos/150/80?random=6',
        },
        {
          name: "Nature's Bounty",
          logo: 'https://picsum.photos/150/80?random=7',
        },
        {
          name: 'Garden of Life',
          logo: 'https://picsum.photos/150/80?random=8',
        },
        {
          name: 'Jarrow Formulas',
          logo: 'https://picsum.photos/150/80?random=9',
        },
        {
          name: 'Thorne',
          logo: 'https://picsum.photos/150/80?random=10',
        },
      ],

      // ===== 客戶評價數據 =====
      // 用途：展示真實用戶評價，建立社會證明和信任度
      // 結構：每個評價包含用戶信息、評價內容和頭像
      // TODO: 更新為API數據
      testimonials: [
        {
          id: 1,
          name: '張小明',
          title: '健康愛好者',
          text: 'tHerd的產品品質很好，配送也很快，已經在這裡購買了兩年多，非常滿意！',
          avatar: 'https://picsum.photos/60/60?random=11',
        },
        {
          id: 2,
          name: '李美華',
          title: '健身教練',
          text: '運動營養產品選擇很多，價格也很合理，推薦給所有健身的朋友。',
          avatar: 'https://picsum.photos/60/60?random=12',
        },
        {
          id: 3,
          name: '王大偉',
          title: '營養師',
          text: '作為營養師，我經常推薦客戶到tHerd購買補充品，品質有保障。',
          avatar: 'https://picsum.photos/60/60?random=13',
        },
      ],
    }
  },
}
</script>

<style scoped>
.homepage {
  min-height: 100vh;
}

@media (max-width: 768px) {
  .hero-section {
    padding: 2rem 0;
  }

  .display-3 {
    font-size: 2rem;
  }

  .category-card,
  .testimonial-card {
    margin-bottom: 1rem;
  }
}

.start-btn {
  background-color: #007083;
  color: white;
  border: none;
  border-radius: 12px;
  padding: 12px 28px;
  font-size: 1.2rem;
  font-weight: bold;
  cursor: pointer;
  transition: all 0.3s;
}
.start-btn:hover {
  background-color: #009aa8;
  transform: scale(1.05);
}
</style>
