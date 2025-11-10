<template>
  <aside class="product-sidebar" :style="{ top: topOffset + 'px' }">
    <!-- 🔹 滾動區：只包含篩選項目 -->
    <div class="sidebar-scroll">
      <h5 class="fw-bold mb-3">篩選條件</h5>

      <BrandFilter v-model="filters.brandIds" />
      <PriceFilter v-model="filters.priceRange" />
      <RatingFilter v-model="filters.rating" />

      <hr />

      <AttributeFilterAccordion
        v-model="filters.attributeFilters"
        :attributes="attributes"
      />
    </div>

    <!-- 🔹 固定底部：重設／套用按鈕 -->
    <div class="sidebar-footer d-flex justify-content-between p-2 border-top bg-white">
      <button class="btn btn-outline-secondary btn-sm" @click="resetFilters">重設</button>
      <button class="btn btn-success btn-sm" @click="applyFilters">套用</button>
    </div>
  </aside>
</template>

<script setup>
import { ref, onMounted, onUnmounted, watch } from 'vue'
import BrandFilter from './sub/BrandFilter.vue'
import PriceFilter from './sub/PriceFilter.vue'
import RatingFilter from './sub/RatingFilter.vue'
import AttributeFilterAccordion from './sub/AttributeFilterAccordion.vue'
import ProductsApi from '@/api/modules/prod/ProductsApi'

const props = defineProps({
  resetKey: Number  // ✅ 外部傳進來的重設觸發值
})

const emit = defineEmits(['filter-change'])

const filters = ref({
  brandIds: [],
  priceRange: { min: null, max: null },
  rating: [],
  attributeFilters: []
})

watch(() => props.resetKey, (newVal, oldVal) => {
  if (newVal === undefined || newVal === oldVal) return
    filters.value.brandIds = []
    filters.value.priceRange = { min: null, max: null }
    filters.value.rating = []
    filters.value.attributeFilters = []

  // 🔥 重設後立即通知父層重新查詢
  emit('filter-change', { ...filters.value })
})

// 🔁 重設篩選條件
function resetFilters() {
  filters.value.brandIds = []
  filters.value.priceRange = { min: null, max: null }
  filters.value.rating = []
  filters.value.attributeFilters = []
  emit('filter-change', { ...filters.value })
}

const attributes = ref([])
const topOffset = ref(300)

onMounted(async () => {
  try {
    const res = await ProductsApi.getFilterAttributes()
    attributes.value = Array.isArray(res) ? res : []
  } catch (err) {
    console.error('❌ 載入屬性資料失敗:', err)
  }
  window.addEventListener('scroll', handleScroll)
})

onUnmounted(() => {
  window.removeEventListener('scroll', handleScroll)
})

function handleScroll() {
  const scrollY = window.scrollY
  topOffset.value = scrollY > 200 ? 100 : 300
}

function applyFilters() {
  emit('filter-change', { ...filters.value })
}
</script>

<style scoped>
.product-sidebar {
  position: fixed;
  left: 0;
  width: 300px;
  height: calc(100vh - 100px);
  background: #fff;
  border-right: 1px solid #dee2e6;
  box-shadow: 2px 0 6px rgba(0, 0, 0, 0.05);
  border-radius: 0 4px 4px 0;
  display: flex;
  flex-direction: column; /* ✅ 上下分佈 */
  transition: top 0.4s ease;
  z-index: 1000;
}

/* 🔹 可捲動區域 */
.sidebar-scroll {
  flex: 1; /* 讓它自動填滿可用空間 */
  overflow-y: auto;
  padding: 1rem;
}

/* 🔹 固定底部按鈕區 */
.sidebar-footer {
  flex-shrink: 0;
  position: sticky;
  bottom: 0;
  background: #fff;
  border-top: 1px solid #dee2e6;
  padding: 0.75rem;
}

/* ✅ 自訂滾輪 */
.sidebar-scroll::-webkit-scrollbar {
  width: 6px;
}
.sidebar-scroll::-webkit-scrollbar-thumb {
  background-color: rgba(0, 0, 0, 0.15);
  border-radius: 6px;
}
.sidebar-scroll::-webkit-scrollbar-thumb:hover {
  background-color: rgba(0, 0, 0, 0.3);
}
</style>
