<!-- src/components/modules/sup/brands/FeaturedCarousel.vue -->
<!-- （精選品牌 Carousel） -->

<template>
  <section class="featured">
    <h2 class="h5 mb-3 text-center">精選品牌</h2>

    <div v-if="loading" class="text-muted small text-center">載入中…</div>
    <div v-else-if="brands.length === 0" class="text-muted small text-center">目前沒有精選品牌</div>

    <div v-else class="carousel-wrapper" @mouseenter="stopAutoPlay" @mouseleave="startAutoPlay">
      <button class="nav prev" @click="prev" aria-label="prev">‹</button>

      <div class="carousel">
        <div class="viewport">
          <div
            class="track"
            :class="{ 'no-transition': noTransition }"
            :style="{ transform: `translateX(-${offsetX}px)` }"
            @transitionend="onTransitionEnd"
          >
            <div
              v-for="(b, i) in visibleSlides"
              :key="`${b.brandId}-${i}`"
              class="card"
              :style="{ width: cardWidth + 'px' }"
            >
              <router-link
                :to="toBrandSlug(b.brandName, b.brandId)"
                class="card-link"
                :aria-label="b.brandName"
              >
                <div class="thumb">
                  <img
                    :src="b.logoUrl || placeholder"
                    :alt="b.brandName"
                    loading="lazy"
                    decoding="async"
                    @error="onImgError($event)"
                  />
                </div>
                <div class="name">{{ b.brandName }}</div>
              </router-link>
            </div>
          </div>
        </div>
      </div>

      <button class="nav next" @click="next" aria-label="next">›</button>
    </div>
  </section>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount, watch } from 'vue'
import { toBrandSlug } from '@/utils/supSlugify'
// const brandTo = (b) => `/brands/${b.brandId}`
// const toBrandSlug = (name, id) => {
//   const slug = String(name || '')
//     .trim()
//     .toLowerCase()
//     .replace(/\s+/g, '-')
//     .replace(/&/g, 'and')
//     .replace(/[^a-z0-9-]/g, '')
//     .replace(/-+/g, '-')
//   return `/brands/${slug}-${id}`
// }

const props = defineProps({
  brands: { type: Array, default: () => [] },
  loading: { type: Boolean, default: false },
})

const placeholder = '/homePageIcon/tHerd-header.png'
const cardWidth = ref(180)
const gap = 12
const startIndex = ref(0)
const noTransition = ref(false)
const offsetX = ref(0)

const visibleCount = ref(0)

const recalc = () => {
  const w = window.innerWidth
  if (w >= 1200) {
    cardWidth.value = 180
    visibleCount.value = 6
  } else if (w >= 992) {
    cardWidth.value = 160
    visibleCount.value = 5
  } else if (w >= 768) {
    cardWidth.value = 160
    visibleCount.value = 4
  } else if (w >= 576) {
    cardWidth.value = 140
    visibleCount.value = 3
  } else {
    cardWidth.value = 140
    visibleCount.value = 2
  }
  // 重新計算起始位置與自動播放狀態
  startIndex.value = 0
  stopAutoPlay()
  startAutoPlay()
}

// 播放控制
const autoplayInterval = 3000
let timer = null

const move = (dir = 1) => {
  if (props.brands.length <= visibleCount.value) return
  noTransition.value = false
  offsetX.value = (cardWidth.value + gap) * dir
  setTimeout(() => {
    noTransition.value = true
    startIndex.value = (startIndex.value + dir + props.brands.length) % props.brands.length
    offsetX.value = 0
  }, 450) // 與 CSS transition 時間一致
}

const next = () => move(1)
const prev = () => move(-1)

const onTransitionEnd = () => {} // 這裡不需要重置（由 setTimeout 控制）

// 自動播放
const startAutoPlay = () => {
  stopAutoPlay()
  if (props.brands.length > visibleCount.value) {
    timer = setInterval(next, autoplayInterval)
  }
}
const stopAutoPlay = () => {
  if (timer) {
    clearInterval(timer)
    timer = null
  }
}

const onImgError = (e) => {
  e.target.src = placeholder
}

const visibleSlides = computed(() => {
  const total = props.brands.length
  const result = []
  for (let i = 0; i < visibleCount.value; i++) {
    result.push(props.brands[(startIndex.value + i) % total])
  }
  return result
})

onMounted(() => {
  recalc()
  window.addEventListener('resize', recalc)
  startAutoPlay()
})
onBeforeUnmount(() => {
  window.removeEventListener('resize', recalc)
  stopAutoPlay()
})

watch(
  () => props.brands,
  () => {
    startIndex.value = 0
    startAutoPlay()
  },
)
</script>

<style scoped>
.featured {
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center; /* ✅ 整體置中 */
  position: relative;
  /* 保留一些基本間距 */
  padding: 1.5rem 0;
}

.carousel-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center; /* ✅ 中間置中 */
  gap: 20px;
  width: 100%;
  max-width: 1200px; /* ✅ 限制最大寬度，居中 */
  margin: 0 auto;
}

/* [調整] 將 overflow: hidden 上移到此層，並增加 padding 作為 hover 效果的緩衝區 */
.carousel {
  flex: 1;
  display: grid;
  place-items: center; /* 完全置中 */
  overflow: hidden;
  padding: 20px 0; /* ✅ 增加垂直 padding */
}

/* [調整] 移除 overflow: hidden，讓 hover 效果可見 */
.viewport {
  overflow: visible; /* ✅ 改為 visible */
  width: calc(var(--vis) * (var(--card) + var(--gap)) - var(--gap));
  max-width: 100%;
  /* [新增] 負 margin 用於抵銷父層的 padding，維持視覺對齊 */
  margin: -15px 0;
}

.track {
  display: flex;
  gap: 12px;
  transition: transform 0.45s ease;
  will-change: transform;
  overflow: visible; /* ✅ 確保 hover 陰影可見 */
  /* 增加一點垂直空間給陰影 */
  padding: 8px 0;
}
.track.no-transition {
  transition: none;
}

.card {
  flex: 0 0 auto;
  border: 1px solid #e5e9ef;
  border-radius: 12px;
  background: #fff;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
  overflow: hidden;
  min-height: 160px;
  position: relative;
  z-index: 1;
  transition: all 0.25s ease-out; /* [新增] 讓 hover 效果更平滑 */
}
.card-link {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 12px;
  text-decoration: none;
  color: inherit;
}
/* hover 時提高 z-index，確保卡片在最上層 */
.card:hover {
  border-color: #c9ebdc;
  box-shadow:
    inset 0 0 0 2px rgba(22, 121, 76, 0.18),
    0 6px 16px rgba(0, 0, 0, 0.1);
  transform: translateY(-5px) scale(1.06);
  z-index: 10; /* ✅ 提高層級，蓋過相鄰卡片 */
}

.thumb {
  height: 110px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #fff;
  border-radius: 8px;
  overflow: hidden;
}
.thumb img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.name {
  margin-top: 8px;
  font-weight: 700;
  font-size: 15px;
  line-height: 1.2;
  text-align: center;
  color: #1f2937;

  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  line-clamp: 2;
  box-orient: vertical;
  overflow: hidden;

  height: 2.4em;
  min-height: 2.4em;
}

.nav {
  position: absolute;
  top: 50%;
  transform: translateY(-50%);
  z-index: 15; /* ✅ 確保按鈕在 hover 的卡片之上 */
  border: none;
  background: rgba(255, 255, 255, 0.9);
  border-radius: 50%;
  width: 48px;
  height: 48px;
  color: #16794c;
  font-size: 24px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition:
    background 0.2s,
    transform 0.2s;
}
.nav:hover {
  background: #e9f7f0;
  transform: translateY(-50%) scale(1.05);
}
.nav:active {
  transform: translateY(-50%) scale(0.95);
}
.nav.prev {
  left: -60px;
}
.nav.next {
  right: -60px;
}
</style>
