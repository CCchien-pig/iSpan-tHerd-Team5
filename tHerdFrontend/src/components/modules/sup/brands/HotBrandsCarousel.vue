<template>
  <div class="hot-brands">
    <div class="carousel-viewport" @mouseenter="pause" @mouseleave="play">
      <div
        class="carousel-track"
        ref="trackRef"
        :class="{ 'no-transition': noTransition }"
        :style="{ transform: `translateX(-${offset}px)` }"
      >
        <div
          v-for="(b, idx) in looped"
          :key="`${b.brandId}-${idx}`"
          class="brand-item relative"
          :style="{ width: `${cardWidth}px`, flexBasis: `${cardWidth}px` }"
        >
          <!-- 計算原始名次（避免重複顯示） -->
          <template v-if="brands.length">
            <div class="rank-icon" v-if="idx % brands.length < 3">
              <Crown v-if="idx % brands.length === 0" class="w-6 h-6 text-yellow-400" />
              <Trophy v-else-if="idx % brands.length === 1" class="w-6 h-6 text-gray-400" />
              <Medal v-else class="w-6 h-6 text-amber-600" />
            </div>
            <div v-else class="rank-num">{{ (idx % brands.length) + 1 }}</div>
          </template>

          <router-link :to="toBrandSlug(b.brandName, b.brandId)" class="brand-link">
            <img
              class="brand-logo"
              :src="b.logoUrl || fallbackLogo"
              :alt="b.brandName"
              loading="lazy"
              @error="onImgError"
            />
            <div class="brand-name">{{ b.brandName }}</div>
          </router-link>
        </div>
      </div>

      <button class="ctrl prev" @click="step(-1)" aria-label="上一個">‹</button>
      <button class="ctrl next" @click="step(1)" aria-label="下一個">›</button>
    </div>

    <div v-if="loading" class="hint">載入中…</div>
    <div v-else-if="error" class="hint text-danger">{{ error }}</div>
  </div>
</template>

<script setup>
import axios from 'axios'
import { ref, computed, onMounted, onBeforeUnmount, nextTick } from 'vue'
import { toBrandSlug } from '@/utils/supSlugify'
import { Crown, Trophy, Medal } from 'lucide-vue-next'

const props = defineProps({
  interval: { type: Number, default: 3000 },
  stepCount: { type: Number, default: 1 },
})

const loading = ref(false)
const error = ref('')
const brands = ref([])
const trackRef = ref(null)
const timer = ref(null)

// 動畫控制
const offset = ref(0)
const noTransition = ref(false)
const isAnimating = ref(false)

// 響應式設定
const cardWidth = ref(160)
const visibleCount = ref(6)
const gapX = 16

const fallbackLogo = '/homePageIcon/tHerd-header.png'
function onImgError(e) {
  e.target.src = fallbackLogo
}

// 無縫資料複製
const looped = computed(() => [...brands.value, ...brands.value])
const itemTotalWidth = computed(() => cardWidth.value + gapX)

const recalc = () => {
  const w = window.innerWidth
  if (w >= 1200) {
    cardWidth.value = 180
    visibleCount.value = 6
  } else if (w >= 992) {
    cardWidth.value = 160
    visibleCount.value = 5
  } else if (w >= 768) {
    cardWidth.value = 150
    visibleCount.value = 4
  } else if (w >= 576) {
    cardWidth.value = 140
    visibleCount.value = 3
  } else {
    cardWidth.value = 130
    visibleCount.value = 2
  }
  resetPosition()
}

async function fetchTopBrands() {
  loading.value = true
  error.value = ''
  try {
    const res = await axios.get('/api/sup/Brands/top-sales')
    if (res.data?.success) {
      brands.value = Array.isArray(res.data.data) ? res.data.data.slice(0, 10) : []
    } else {
      brands.value = [
        { brandId: 1, brandName: 'Optimum Nutrition', logoUrl: '' },
        { brandId: 2, brandName: 'MuscleTech', logoUrl: '' },
        { brandId: 3, brandName: 'BSN', logoUrl: '' },
        { brandId: 4, brandName: 'MyProtein', logoUrl: '' },
        { brandId: 5, brandName: 'Dymatize', logoUrl: '' },
        { brandId: 6, brandName: 'Cellucor', logoUrl: '' },
        { brandId: 7, brandName: 'Now Foods', logoUrl: '' },
      ]
    }
  } catch (e) {
    error.value = e?.response?.data?.message || '載入失敗'
  } finally {
    loading.value = false
  }
}

function play() {
  stop()
  if (brands.value.length > visibleCount.value) {
    timer.value = setInterval(() => {
      slide(props.stepCount)
    }, props.interval)
  }
}

function pause() {
  stop()
}

function stop() {
  if (timer.value) {
    clearInterval(timer.value)
    timer.value = null
  }
}

function step(dir) {
  if (isAnimating.value) return
  pause()
  slide(dir > 0 ? props.stepCount : -props.stepCount)
}

function resetPosition() {
  noTransition.value = true
  offset.value = 0
  setTimeout(() => {
    noTransition.value = false
  }, 50)
}

function slide(n) {
  if (brands.value.length === 0) return
  isAnimating.value = true
  const dx = itemTotalWidth.value * n
  const maxOffset = itemTotalWidth.value * brands.value.length
  offset.value += dx

  setTimeout(() => {
    if (offset.value >= maxOffset) {
      noTransition.value = true
      offset.value -= maxOffset
      nextTick(() => {
        setTimeout(() => (noTransition.value = false), 50)
      })
    } else if (offset.value < 0) {
      noTransition.value = true
      offset.value += maxOffset
      nextTick(() => {
        setTimeout(() => (noTransition.value = false), 50)
      })
    }
    isAnimating.value = false
  }, 500)
}

onMounted(async () => {
  recalc()
  window.addEventListener('resize', recalc)
  await fetchTopBrands()
  if (brands.value.length > 0) play()
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', recalc)
  stop()
})
</script>

<style scoped>
.hot-brands {
  width: 100%;
  padding: 24px 0;
  position: relative;
}
.carousel-viewport {
  position: relative;
  overflow: hidden;
  width: 100%;
  padding: 10px 0;
}
.carousel-track {
  display: flex;
  align-items: stretch;
  will-change: transform;
  transition: transform 0.4s ease-in-out;
}
.carousel-track.no-transition {
  transition: none !important;
}
.brand-item {
  flex-shrink: 0;
  margin: 0 8px;
  text-align: center;
  position: relative;
}
.brand-link {
  text-decoration: none;
  color: inherit;
  display: block;
  padding: 12px;
  background: #fff;
  border-radius: 12px;
  border: 1px solid #f0f0f0;
  transition: all 0.3s ease;
}
.brand-link:hover {
  border-color: #007083;
  box-shadow: 0 4px 12px rgba(0, 112, 131, 0.15);
  transform: translateY(-3px);
}
.brand-logo {
  width: 100%;
  height: 64px;
  object-fit: contain;
  filter: grayscale(30%);
  opacity: 0.9;
  transition: all 0.3s ease;
}
.brand-link:hover .brand-logo {
  filter: grayscale(0%);
  opacity: 1;
}
.brand-name {
  margin-top: 8px;
  font-size: 0.9rem;
  font-weight: 600;
  color: #2c3e50;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* 🏅 名次徽章區 */
.rank-icon,
.rank-num {
  position: absolute;
  top: 6px;
  right: 8px;
  z-index: 5;
  display: flex;
  align-items: center;
  justify-content: center;
}
.rank-num {
  background-color: rgba(90, 199, 213, 0.599);
  color: #fff;
  font-size: 0.75rem;
  font-weight: 700;
  border-radius: 50%;
  width: 24px;
  height: 24px;
  line-height: 24px;
  text-align: center;
}

/* 控制按鈕 */
.ctrl {
  position: absolute;
  top: 50%;
  transform: translateY(-50%);
  width: 36px;
  height: 36px;
  border-radius: 50%;
  border: none;
  color: #007083;
  background: rgba(255, 255, 255, 0.9);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  cursor: pointer;
  font-size: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0;
  transition: all 0.3s ease;
  z-index: 2;
}
.carousel-viewport:hover .ctrl {
  opacity: 1;
}
.ctrl:hover {
  background: #007083;
  color: #fff;
}
.ctrl.prev {
  left: 10px;
}
.ctrl.next {
  right: 10px;
}
.hint {
  text-align: center;
  margin-top: 8px;
  color: #666;
}
</style>
