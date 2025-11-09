<template>
  <aside class="product-filter p-3 border rounded">
    <BrandFilter :brands="brands" @update:brands="filters.brand = $event" />
    <PriceFilter :priceRanges="priceRanges" @update:prices="filters.price = $event" />
    <RatingFilter :ratings="ratings" @update:ratings="filters.rating = $event" />
  </aside>
</template>

<script setup>
import { ref, reactive, watch } from 'vue'
import BrandFilter from './ProductFilters/BrandFilter.vue'
import PriceFilter from './ProductFilters/PriceFilter.vue'
import RatingFilter from './ProductFilters/RatingFilter.vue'

const filters = reactive({ brand: [], price: [], rating: [] })

// 模擬資料
const brands = ref([
  { id: 1, name: 'iHerb 自主研發品牌', count: 285 },
  { id: 2, name: '諾奧', count: 135 },
  { id: 3, name: 'California Gold Nutrition', count: 178 },
  { id: 4, name: 'Life Extension', count: 47 },
  { id: 5, name: 'Swanson', count: 327 },
  { id: 6, name: 'Now Foods', count: 220 },
  { id: 7, name: 'Doctor\'s Best', count: 99 }
])

const priceRanges = [
  { label: 'NT$0 - NT$200', count: 2164 },
  { label: 'NT$200 - NT$500', count: 6279 },
  { label: 'NT$500 - NT$1,000', count: 3539 },
  { label: 'NT$1,000 - NT$1,500', count: 721 },
  { label: 'NT$1,500+', count: 238 }
]

const ratings = [
  { value: 5, count: 3572 },
  { value: 4, count: 9115 },
  { value: 3, count: 152 },
  { value: 2, count: 10 },
  { value: 1, count: 2 }
]

watch(filters, newVal => {
  console.log('更新篩選條件：', newVal)
  // 🔹這裡可呼叫 API 重新載入商品清單
}, { deep: true })
</script>

<style>
.filter-section h6 {
  cursor: pointer;
}
.filter-section .form-check-label {
  cursor: pointer;
}
</style>