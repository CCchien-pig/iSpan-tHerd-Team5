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
              <router-link :to="brandTo(b)" class="card-link" :aria-label="b.brandName">
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

const visibleCount = computed(() => {
  const w = window.innerWidth
  if (w >= 1200) return 6
  if (w >= 992) return 5
  if (w >= 768) return 4
  if (w >= 576) return 3
  return 2
})

// 根據起始索引計算目前可見的品牌
const visibleSlides = computed(() => {
  const total = props.brands.length
  const result = []
  for (let i = 0; i < visibleCount.value; i++) {
    result.push(props.brands[(startIndex.value + i) % total])
  }
  return result
})

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

const recalc = () => {
  const w = window.innerWidth
  if (w >= 1200) cardWidth.value = 180
  else if (w >= 768) cardWidth.value = 160
  else cardWidth.value = 140
}

const onImgError = (e) => {
  e.target.src = placeholder
}

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

const brandTo = (b) => `/brands/${b.brandId}`
</script>

<style scoped>
.featured {
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center; /* ✅ 整體置中 */
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

.carousel {
  flex: 1;
  overflow: hidden;
  display: flex;
  justify-content: center;
}

.viewport {
  overflow: hidden;
  width: 100%;
}

.track {
  display: flex;
  gap: 12px;
  transition: transform 0.45s ease;
  will-change: transform;
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
}

.card-link {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 14px 12px;
  text-decoration: none;
  color: inherit;
}

.thumb {
  height: 110px;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.thumb img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.name {
  margin-top: 10px;
  font-weight: 700;
  font-size: 15px;
  text-align: center;
  color: #1f2937;
  min-height: 40px;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

/* ✅ 左右按鈕樣式改進 */
.nav {
  position: absolute;
  top: 50%;
  transform: translateY(-50%);
  z-index: 5;
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
  left: -60px; /* ✅ 往外拉開 */
}
.nav.next {
  right: -60px;
}
</style>
