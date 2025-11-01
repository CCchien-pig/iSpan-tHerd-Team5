<!--
  ProducSearch.vue - 產品列表查詢範例 (Example) 
  功能：展示產品列表，包含標題、查看全部按鈕和產品卡片網格
  特色：響應式網格布局、事件傳遞、可配置標題
  用途：用於首頁、產品頁面等需要展示多個產品的區域
-->
<template>
  <div class="container py-5">
    <h2 class="mb-4">商品查詢</h2>
    <!-- 搜尋欄位 -->
    <div class="d-flex gap-2 mb-4">
      <input v-model="keyword" type="text" class="form-control" placeholder="輸入商品關鍵字" />
      <button class="btn teal-reflect-button text-white" @click="searchProducts(1)">搜尋</button>
    </div>

    <!-- 查詢結果 -->
  <!-- 商品卡片列表 -->

      <!-- 🧩 商品列表 -->
    <ProductList
      :key="pageIndex + '_' + keyword"
      :title="'搜尋結果'"
      :products="products"
      :total-count="totalCount"
      :page-size="pageSize"
      :page-index="pageIndex"
      @page-change="page => searchProducts(page)"
      @add-to-cart="addToCart"
    />
   <!-- 
  <div class="row g-4">
    <div class="col-12 col-sm-6 col-md-3 d-flex justify-content-center" v-for="p in filteredProducts" :key="p.id">
      <ProductCard class="w-100" :product="p" @add-to-cart="addToCart" />
         <img :src="p.image" class="card-img-top" :alt="p.name" />
          <div class="card-body">
            <h6 class="card-title">{{ p.name }}</h6>
            <p class="text-muted">NT$ {{ p.price }}</p>
          </div> 
      </div>
    </div>-->
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useLoading } from "@/composables/useLoading";
import ProductsApi from "@/api/modules/prod/ProductsApi";
//import ProductCard from "@/components/modules/prod/card/ProductCard.vue";
import ProductList from '@/components/modules/prod/list/ProductList.vue';

const route = useRoute()
const router = useRouter()
const { showLoading, hideLoading } = useLoading()

// const keyword = ref("")
// const products = ref([
  // { id: 1, name: "維他命C 膠囊", price: 450, image: "https://via.placeholder.com/300x200?text=Vitamin+C" },
  // { id: 2, name: "護手霜", price: 320, image: "https://via.placeholder.com/300x200?text=Hand+Cream" },
  // { id: 3, name: "洗衣精", price: 199, image: "https://via.placeholder.com/300x200?text=Detergent" }
// ])
const error = ref(null)

// 🔸 狀態變數
const keyword = ref('')
const products = ref([])
const totalCount = ref(0)
const pageIndex = ref(1)
const pageSize = ref(40) // 每頁40筆

// 篩選邏輯
const filteredProducts = computed(() => {
  if (!Array.isArray(products.value)) return [];
  return products.value.filter((p) =>
    !keyword.value || p.name.includes(keyword.value)
  );
})

const isLoading = ref(false)

// 搜尋動作（之後可接後端 API）
const searchProducts = async (page = 1) => {
  if (isLoading.value) return
  isLoading.value = true

  try {
    showLoading('載入商品中...')
    const res = await ProductsApi.getProductList({
      pageIndex: page,
      pageSize: 40,
      keyword: keyword.value,
      sortBy: 'date',
      sortDesc: true,
      isPublished: true,
      isFrontEnd: true
    })

    // 這裡改成確保 data 結構正確
    const data = res.data
    if (!data || !Array.isArray(data.items)) {
      console.warn('⚠️ 無 items 或格式錯誤', data)
      products.value = []
      totalCount.value = 0
      return
    }

    // ✅ 更新資料
    products.value = data.items
    totalCount.value = data.totalCount || 0
    pageIndex.value = data.pageIndex || 1
  } catch (err) {
    console.error('搜尋商品錯誤：', err)
    products.value = []
    totalCount.value = 0
  } finally {
    isLoading.value = false
    hideLoading()
  }
}


// 點擊商品跳轉
const goToProduct = (productId) => {
  router.push({ name: "product-detail", params: { id: productId } });
  window.scrollTo({ top: 0, behavior: "smooth" });
}

// 🔸 加入購物車（範例）
const addToCart = (product) => {
  console.log('加入購物車：', product.productName)
}

// 生命週期
// 初始載入商品列表
onMounted(() => {
  searchProducts(1);
})
</script>
