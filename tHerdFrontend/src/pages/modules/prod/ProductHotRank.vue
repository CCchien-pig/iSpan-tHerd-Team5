<!--
  ProductHotRank.vue - 暢銷排名頁
  功能：顯示前端熱門商品排行（兩欄並排）
-->
<template>
  <div class="container py-5">
    <!-- 🏷️ 頁面標題 -->
    <h2 class="fw-bold mb-4 text-center">暢銷排名</h2>

    <!-- 🧾 兩欄排行榜 -->
    <div class="row g-4">
      <!-- 左欄 -->
      <div class="col-md-6">
        <div v-for="(item, index) in leftColumn" :key="item.productId" class="rank-item d-flex align-items-center mb-3 p-3 border rounded-3 shadow-sm bg-white">
          <div class="rank-number me-3">{{ index + 1 }}</div>
          <img
            :src="item.imageUrl"
            alt="商品圖片"
            class="rank-img me-3"
          />
          <div class="rank-info flex-grow-1">
            <router-link
              :to="`/prod/products/${item.productId}`"
              class="fw-semibold text-decoration-none text-dark"
            >
              {{ item.productName }}
            </router-link>
            <div class="small text-muted">
              ⭐ {{ item.avgRating?.toFixed(1) || '0.0' }}（{{ item.reviewCount || 0 }} 則評價）
            </div>
            <div class="fw-bold text-danger">
                NT${{ Math.round(item.billingPrice || 0) }}
            </div>
          </div>
        </div>
      </div>

      <!-- 右欄 -->
      <div class="col-md-6">
        <div v-for="(item, index) in rightColumn" :key="item.productId" class="rank-item d-flex align-items-center mb-3 p-3 border rounded-3 shadow-sm bg-white">
          <div class="rank-number me-3">{{ index + leftColumn.length + 1 }}</div>
          <img
            :src="item.imageUrl"
            alt="商品圖片"
            class="rank-img me-3"
          />
          <div class="rank-info flex-grow-1">
            <router-link
              :to="`/prod/products/${item.productId}`"
              class="fw-semibold text-decoration-none text-dark"
            >
              {{ item.productName }}
            </router-link>
            <div class="small text-muted">
              ⭐ {{ item.avgRating?.toFixed(1) || '0.0' }}（{{ item.reviewCount || 0 }} 則評價）
            </div>
            <div class="fw-bold text-danger">
                NT${{ Math.round(item.billingPrice || 0) }}
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 錯誤提示 -->
    <div v-if="errorMessage" class="text-danger text-center mt-4">{{ errorMessage }}</div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from "vue";
import { useLoading } from "@/composables/useLoading";
import ProductsApi from "@/api/modules/prod/ProductsApi";

const { showLoading, hideLoading } = useLoading();
const products = ref([]);
const errorMessage = ref("");

// 左右欄切割
const leftColumn = computed(() => products.value.slice(0, 10));
const rightColumn = computed(() => products.value.slice(10, 20));

async function loadHotProducts() {
  try {
    showLoading("載入暢銷商品中...")

    const res = await ProductsApi.getProductList({
      pageIndex: 1,
      pageSize: 20,
      sortBy: "relevance",
      sortDesc: true,
      isPublished: true,
      isFrontEnd: true,
      other: "Hot" // ✅ 關鍵參數
    })

    const data = res.data || {}
    products.value = Array.isArray(data.items) ? data.items : []
  } catch (err) {
    console.error("❌ 載入暢銷商品錯誤：", err)
    errorMessage.value = "無法載入暢銷商品資料。"
  } finally {
    hideLoading()
  }
}

onMounted(() => {
  loadHotProducts();
});
</script>

<style scoped>
.rank-item {
  transition: transform 0.2s ease;
}
.rank-item:hover {
  transform: translateY(-2px);
}
.rank-number {
  font-size: 1.8rem;
  font-weight: 700;
  color: #6a1b9a;
  width: 40px;
  text-align: center;
}
.rank-img {
  width: 60px;
  height: 60px;
  object-fit: contain;
}
</style>
