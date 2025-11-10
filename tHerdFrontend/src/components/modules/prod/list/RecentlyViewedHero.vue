<!--
  RecentlyViewedHero.vue - 最近瀏覽商品區塊（改為使用通用 ProductCard）
-->
<template>
  <div v-if="recentProducts.length > 0" class="recently-viewed-hero container position-relative">
    <h3 class="fw-bold text-center mb-4">最近瀏覽的商品</h3>

    <!-- 左右箭頭控制 -->
    <button class="scroll-btn left" @click="scrollLeft">
      <i class="bi bi-chevron-left"></i>
    </button>
    <button class="scroll-btn right" @click="scrollRight">
      <i class="bi bi-chevron-right"></i>
    </button>

    <!-- 橫向可捲動列 -->
    <div ref="scrollContainer" class="scroll-container d-flex flex-nowrap gap-3 overflow-auto px-2">
      <div
        v-for="p in recentProducts"
        :key="p.productId"
        class="flex-shrink-0"
        style="width: 220px;"
      >
        <ProductCard
          :product="p"
          @add-to-cart="$emit('add-to-cart', p)"
          @toggle-wishlist="$emit('toggle-wishlist', p)"
          @quick-view="$emit('quick-view', p)"
        />
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount, computed } from 'vue'
import ProductCard from '@/components/modules/prod/card/ProductCard.vue'

const STORAGE_KEY = 'recently_viewed_products'
const rawRecentProducts = ref([])
const scrollContainer = ref(null)

const recentProducts = computed(() =>
  rawRecentProducts.value.slice(0, 12).map(p => ({
    productId: p.productId ?? p.id,
    productName: p.productName ?? p.name ?? '',
    brandName: p.brandName ?? p.brand ?? '',
    imageUrl: p.imageUrl ?? p.image ?? '',
    billingPrice: p.currentPrice ?? p.billingPrice ?? p.price ?? 0,
    listPrice: p.originalPrice ?? p.listPrice ?? p.msrp ?? 0,
    avgRating: p.avgRating ?? p.rating ?? 0,
    reviewCount: p.reviewCount ?? p.reviews ?? 0,
    badgeName: p.badgeName ?? p.badge ?? null,
  }))
)

function loadRecentlyViewed() {
  const list = JSON.parse(localStorage.getItem(STORAGE_KEY) || '[]')
  rawRecentProducts.value = Array.isArray(list) ? list : []
}

function scrollLeft() {
  if (scrollContainer.value)
    scrollContainer.value.scrollBy({ left: -300, behavior: 'smooth' })
}
function scrollRight() {
  if (scrollContainer.value)
    scrollContainer.value.scrollBy({ left: 300, behavior: 'smooth' })
}

function handleRecentlyViewedUpdated() {
  loadRecentlyViewed()
}

onMounted(() => {
  loadRecentlyViewed()
  window.addEventListener('recently-viewed-updated', handleRecentlyViewedUpdated)
})
onBeforeUnmount(() => {
  window.removeEventListener('recently-viewed-updated', handleRecentlyViewedUpdated)
})
</script>

<style scoped>
.recently-viewed-hero {
  position: relative;
  padding-bottom: 2rem;
}

/* 橫向滾動區 */
.scroll-container {
  scroll-behavior: smooth;
  scrollbar-width: none; /* Firefox */
}
.scroll-container::-webkit-scrollbar {
  display: none; /* Chrome */
}

/* 左右箭頭按鈕 */
.scroll-btn {
  position: absolute;
  top: 45%;
  transform: translateY(-50%);
  z-index: 10;
  background: #0f5b5c;
  color: white;
  border: none;
  border-radius: 50%;
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: background 0.2s;
}
.scroll-btn:hover {
  background: #0b3f40;
}
.scroll-btn.left {
  left: -10px;
}
.scroll-btn.right {
  right: -10px;
}
</style>
