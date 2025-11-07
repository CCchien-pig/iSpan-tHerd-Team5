<!--
  ProductShowcase.vue - 產品清單預覽 (有查看全部)
  功能：
    - 從資料庫（API）載入最新或熱門商品，展示前 N 筆資料
    - 可設定標題與查看全部按鈕
    - 每個商品顯示卡片，支援加入購物車 / 收藏 / 快速查看事件
  特色：
    - 響應式網格布局（Bootstrap Row + Col）
    - 使用 ProductCard 子組件顯示商品資訊
    - 無分頁功能（只取前幾筆）
  用途：
    - 首頁區塊展示「熱銷產品」、「最新上架」等
    - 可重複使用於不同主題區塊
-->
<template>
  <!-- 產品列表區塊容器 -->
  <section class="products-section py-5 bg-light">
    <div class="container">
      <!-- 標題和查看全部按鈕 -->
      <div class="d-flex justify-content-between align-items-center mb-5 flex-wrap gap-2">
        <h2 class="mb-0">{{ title }}</h2>
        
          <!-- ✅ 改成用 goToSearch 事件 -->
        <a
          href="#"
          class="btn btn-outline-primary"
          @click.prevent="goToSearch"
        >
          {{ viewAllText }}
        </a>

        <p v-if="products">共 {{ products.length }} 筆商品</p>
      </div>
      <!-- 🔹 商品卡片區 -->
      <div v-if="products && products.length > 0" class="row g-4">
        <div
          v-for="product in products"
          :key="product.productId"
          class="col-lg-3 col-md-6"
        >
          <!-- 單一商品卡片 -->
          <ProductCard
            :product="product"
            @add-to-cart="handleAddToCart"
            @toggle-wishlist="handleToggleWishlist"
            @quick-view="handleQuickView"
          />
        </div>
      </div>

      <!-- 🟡 無商品提示 -->
      <div v-else class="text-center py-5 text-muted">
        尚無商品資料。
      </div>
    </div>
  </section>
</template>

<script>
// 導入產品卡片組件
import ProductCard from '@/components/modules/prod/card/ProductCard.vue'
import ProductsApi from '@/api/modules/prod/ProductsApi'

export default {
  name: 'ProductShowcase',

  components: {
    ProductCard,
  },

  props: {
    /** 區塊標題（例：熱銷商品 / 最新上架） */
    title: {
      type: String,
      default: '熱銷產品',
    },

    /** 查看全部按鈕文字 */
    viewAllText: {
      type: String,
      default: '查看全部',
    },
    /** 顯示筆數上限（預設顯示4筆） */
    pageSize: { type: Number, default: 4 },
  },

  // 補上 data() 定義
  data() {
    return {
      products: [],      // 商品清單
      totalCount: 0,     // 商品總數
      isLoading: false,  // 載入中狀態
    }
  },

  mounted() {
    // 🔸 組件載入時即抓取商品清單
    this.fetchProducts()
  },

  methods: {
    /**
     * 從後端 API 取得商品清單（僅取前 N 筆）
     * 使用 ProductsApi.getProductList() 與主查詢頁相同邏輯
     */
    async fetchProducts() {
      if (this.isLoading) return
      this.isLoading = true

      try {
        const res = await ProductsApi.getProductList({
          pageIndex: 1,
          pageSize: this.pageSize,
          sortBy: 'date',
          sortDesc: true,
          isPublished: true,
          isFrontEnd: true,
          Other: 'Hot',
        })

        const data = res.data
        if (!data || !Array.isArray(data.items)) {
          console.warn('⚠️ 無 items 或格式錯誤', data)
          this.products = []
          this.totalCount = 0
          return
        }

        this.products = data.items
        this.totalCount = data.totalCount || 0
      } catch (err) {
        console.error('❌ 載入商品錯誤：', err)
        this.products = []
        this.totalCount = 0
      } finally {
        this.isLoading = false
      }
    },

    /**
     * 點擊「查看全部」導向搜尋或商品主頁
     */
    goToSearch() {
      this.$router.push({
        name: 'product-main-search', // 對應 router 的 name
      })
    },

    /**
     * 加入購物車事件
     */
    handleAddToCart(product) {
      this.$emit('add-to-cart', product)
    },

    /**
     * 收藏切換事件
     */
    handleToggleWishlist(product, isInWishlist) {
      this.$emit('toggle-wishlist', product, isInWishlist)
    },

    /**
     * 快速查看事件
     */
    handleQuickView(product) {
      this.$emit('quick-view', product)
    },
  },
}
</script>

<style scoped>
/* 使用Bootstrap類，無需自定義CSS */
.pagination .page-link {
  color: #0d6efd;
  transition: all 0.2s;
}

.pagination .page-link:hover {
  background-color: #0d6efd;
  color: #fff;
}

.pagination .page-item.disabled .page-link {
  color: #999;
  pointer-events: none;
  background-color: #f8f9fa;
}
</style>