<template>
  <div class="container py-5">
    <h2 class="mb-4">商品查詢</h2>
    <!-- 搜尋欄位 -->
    <div class="d-flex gap-2 mb-4">
      <input v-model="keyword" type="text" class="form-control" placeholder="輸入商品關鍵字" />
      <button class="btn btn-primary" @click="searchProducts">搜尋</button>
    </div>

    <!-- 查詢結果 -->
  <!-- 商品卡片列表 -->
  <div class="row g-4">
    <div class="col-12 col-sm-6 col-md-3 d-flex justify-content-center" v-for="p in filteredProducts" :key="p.id">
      <ProductCard class="w-100" :product="p" @add-to-cart="addToCart" />
          <!-- <img :src="p.image" class="card-img-top" :alt="p.name" />
          <div class="card-body">
            <h6 class="card-title">{{ p.name }}</h6>
            <p class="text-muted">NT$ {{ p.price }}</p>
          </div> -->
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useLoading } from "@/composables/useLoading";
import ProductsApi from "@/api/modules/prod/ProductsApi";
import ProductCard from "@/components/modules/prod/card/ProductCard.vue";

const route = useRoute()
const router = useRouter()
const { showLoading, hideLoading } = useLoading()

const keyword = ref("")
const products = ref([
  // { id: 1, name: "維他命C 膠囊", price: 450, image: "https://via.placeholder.com/300x200?text=Vitamin+C" },
  // { id: 2, name: "護手霜", price: 320, image: "https://via.placeholder.com/300x200?text=Hand+Cream" },
  // { id: 3, name: "洗衣精", price: 199, image: "https://via.placeholder.com/300x200?text=Detergent" }
])
const error = ref(null)

// 篩選邏輯
const filteredProducts = computed(() => {
  if (!Array.isArray(products.value)) return [];
  return products.value.filter((p) =>
    !keyword.value || p.name.includes(keyword.value)
  );
})

// 搜尋動作（之後可接後端 API）
const searchProducts = async () => {
  try {
    showLoading("搜尋商品中...");
    const response = await ProductsApi.getProductList({ keyword: keyword.value });
    console.log("API 回傳：", response);

    // 確認回傳資料格式
    // products.value = Array.isArray(response.data) ? response.data : [];

    // 安全取值
    const items = response?.data?.items ?? [];
    products.value = Array.isArray(items) ? items : [];
  } catch (err) {
    console.error("搜尋商品錯誤：", err);
    products.value = [];
  } finally {
    hideLoading();
  }
}

// 點擊商品跳轉
const goToProduct = (productId) => {
  router.push({ name: "product-detail", params: { id: productId } });
  window.scrollTo({ top: 0, behavior: "smooth" });
}

// 生命週期
// 初始載入商品列表
onMounted(() => {
  searchProducts();
})
</script>
