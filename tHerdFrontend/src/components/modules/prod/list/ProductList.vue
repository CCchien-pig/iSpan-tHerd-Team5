<!--
  ProductList.vue - 產品列表組件
  功能：展示產品列表，包含標題、查看全部按鈕和產品卡片網格
  特色：響應式網格布局、事件傳遞、可配置標題
  用途：用於首頁、產品頁面等需要展示多個產品的區域
-->
<template>
  <!-- 產品列表區塊容器 -->
  <section class="products-section py-5 bg-light">
    <div class="container">
      <!-- 標題和查看全部按鈕 -->
      <div class="d-flex justify-content-between align-items-center mb-5">
        <h2>{{ title }}</h2>
        <a href="#" class="btn btn-outline-primary">{{ viewAllText }}</a>
        <p>共 {{ products.length }} 筆商品</p>
      </div>
      <!-- 產品卡片網格 -->
      <!-- 若有資料才顯示 -->
      <div v-if="products && products.length > 0" class="row g-4">
        <div
          v-for="product in products"
          :key="product.productId"
          class="col-lg-3 col-md-6"
        >
          <ProductCard
            :product="product"
            @add-to-cart="handleAddToCart"
            @toggle-wishlist="handleToggleWishlist"
            @quick-view="handleQuickView"
          />
        </div>
      </div>

      <!-- 若沒有資料顯示提示 -->
      <div v-else class="text-center text-muted py-5 fs-5">
        找不到符合的商品
      </div>

      <!-- 分頁按鈕 -->
      <nav v-if="totalPages > 1" class="mt-5">
        <ul class="pagination justify-content-center mb-0">

          <!-- 第一頁 -->
          <li class="page-item" :class="{ disabled: currentPage === 1 }">
            <a
              class="page-link"
              href="#"
              @click.prevent="changePage(1)"
            >第一頁</a>
          </li>

          <!-- 上一頁 -->
          <li class="page-item" :class="{ disabled: currentPage === 1 }">
            <a
              class="page-link"
              href="#"
              @click.prevent="changePage(currentPage - 1)"
            >上一頁</a>
          </li>

          <!-- 動態頁碼 -->
          <li
            v-for="page in visiblePages"
            :key="page"
            class="page-item"
            :class="{ active: currentPage === page }"
          >
            <a class="page-link" href="#" @click.prevent="changePage(page)">
              {{ page }}
            </a>
          </li>

          <!-- 下一頁 -->
          <li class="page-item" :class="{ disabled: currentPage === totalPages }">
            <a
              class="page-link"
              href="#"
              @click.prevent="changePage(currentPage + 1)"
            >下一頁</a>
          </li>

          <!-- 最後一頁 -->
          <li class="page-item" :class="{ disabled: currentPage === totalPages }">
            <a
              class="page-link"
              href="#"
              @click.prevent="changePage(totalPages)"
            >最後一頁</a>
          </li>
        </ul>
      </nav>
    </div>
  </section>
</template>

<script>
// 導入產品卡片組件
import ProductCard from '@/components/modules/prod/card/ProductCard.vue'

/**
 * ProductList.vue 組件配置
 * 功能：可重用的產品列表組件
 * 特色：支持動態產品數據、事件傳遞、響應式布局
 */
export default {
  name: 'ProductList', // 組件名稱

  /**
   * 子組件註冊
   */
  components: {
    ProductCard,
  },

  /**
   * Props定義 - 組件的可配置屬性
   */
  props: {
    // 列表標題
    title: {
      type: String,
      default: '熱銷產品',
    },
    // 產品數據數組
    products: {
      type: Array,
      required: true, // 必須提供產品數據
      default: () => [], // 默認為空數組
    },
    // 查看全部按鈕文字
    viewAllText: {
      type: String,
      default: '查看全部',
    },

    totalCount: { type: Number, default: 0 }, // API 回傳的總筆數
    pageSize: { type: Number, default: 40 },
    pageIndex: { type: Number, default: 1 },
  },

  data() {
    return {
      currentPage: this.pageIndex,
    }
  },

  computed: {
    totalPages() {
      return Math.ceil(this.totalCount / this.pageSize)
    },
    visiblePages() {
      const maxVisible = 5
      const pages = []
      let start = Math.max(this.currentPage - Math.floor(maxVisible / 2), 1)
      let end = Math.min(start + maxVisible - 1, this.totalPages)
      if (end - start < maxVisible - 1) {
        start = Math.max(end - maxVisible + 1, 1)
      }
      for (let i = start; i <= end; i++) pages.push(i)
      return pages
    },
  },

  watch: {
    pageIndex(newVal) {
      this.currentPage = newVal
    },
  },

  /**
   * 方法定義 - 處理產品相關事件
   * 所有方法都通過emit向父組件發送事件
   */
  methods: {
    /**
     * 處理加入購物車事件
     * @param {Object} product - 產品對象
     * 發送add-to-cart事件給父組件
     */
    handleAddToCart(product) {
      this.$emit('add-to-cart', product)
    },

    /**
     * 處理收藏切換事件
     * @param {Object} product - 產品對象
     * @param {boolean} isInWishlist - 是否在收藏清單中
     * 發送toggle-wishlist事件給父組件
     */
    handleToggleWishlist(product, isInWishlist) {
      this.$emit('toggle-wishlist', product, isInWishlist)
    },

    /**
     * 處理快速查看事件
     * @param {Object} product - 產品對象
     * 發送quick-view事件給父組件
     */
    handleQuickView(product) {
      this.$emit('quick-view', product)
    },
    
    changePage(page) {
      if (page < 1 || page > this.totalPages) return
      this.currentPage = page
      this.$emit('page-change', page)
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
