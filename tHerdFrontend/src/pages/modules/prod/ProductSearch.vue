<template>
  <div class="container py-5">
    <h2 class="mb-4">商品查詢</h2>
    <!-- 搜尋欄位 -->
    <div class="d-flex gap-2 mb-4">
      <input v-model="keyword" type="text" class="form-control" placeholder="輸入商品關鍵字" />
      <button class="btn btn-primary" @click="searchProducts">搜尋</button>
    </div>

    <!-- 查詢結果 -->
    <div class="row g-4">
      <div v-for="p in filteredProducts" :key="p.id" class="col-md-4">
        <div class="card h-100">
          <img :src="p.image" class="card-img-top" :alt="p.name" />
          <div class="card-body">
            <h6 class="card-title">{{ p.name }}</h6>
            <p class="text-muted">NT$ {{ p.price }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from "vue";

const keyword = ref("");
const products = ref([
  { id: 1, name: "維他命C 膠囊", price: 450, image: "https://via.placeholder.com/300x200?text=Vitamin+C" },
  { id: 2, name: "護手霜", price: 320, image: "https://via.placeholder.com/300x200?text=Hand+Cream" },
  { id: 3, name: "洗衣精", price: 199, image: "https://via.placeholder.com/300x200?text=Detergent" }
]);

const filteredProducts = computed(() =>
  products.value.filter((p) => !keyword.value || p.name.includes(keyword.value))
);

const searchProducts = () => {
  console.log("搜尋:", keyword.value);
};
</script>
