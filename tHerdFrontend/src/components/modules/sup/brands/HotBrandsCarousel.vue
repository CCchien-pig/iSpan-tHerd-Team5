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
          class="brand-item"
          :style="{ width: `${cardWidth}px`, flexBasis: `${cardWidth}px` }"
        >
          <router-link :to="toBrandSlug(b.brandName, b.brandId)" class="brand-link">
            <img
              class="brand-logo"
              :src="b.logoUrl || fallbackLogo"
              :alt="b.brandName"
              loading="lazy"
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

const props = defineProps({
  // 自動播放間隔(毫秒)
  interval: { type: Number, default: 3000 },
  // 每次移動幾個
  stepCount: { type: Number, default: 1 },
})

const loading = ref(false)
const error = ref('')
const brands = ref([])
const trackRef = ref(null)
const timer = ref(null)

// 動畫控制
const offset = ref(0)
const noTransition = ref(false) // 控制是否關閉動畫(用於瞬間回彈)
const isAnimating = ref(false) // 防止快速點擊

// 響應式設定
const cardWidth = ref(160) // 單張卡片寬度
const visibleCount = ref(6) // 一屏顯示幾張
const gapX = 16 // 卡片左右 margin 總和 (margin: 0 8px => 8+8=16)

const fallbackLogo = '/assets/brand-fallback.svg'

// 讓資料無縫：複製一份接在後面 [A,B,C] => [A,B,C, A,B,C]
const looped = computed(() => [...brands.value, ...brands.value])

// 計算單一卡片佔據的總寬度 (含間距)
const itemTotalWidth = computed(() => cardWidth.value + gapX)

// 響應式計算：依據螢幕寬度決定卡片大小與顯示數量
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
    // 手機版小一點，讓使用者看到旁邊還有東西
    cardWidth.value = 130
    visibleCount.value = 2
  }
  // 視窗改變時重置位置，避免錯位
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
      // error.value = res.data?.message || '讀取失敗'
      // 測試用假資料 (若 API 失敗時可測試畫面)
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
  // 只有當品牌數量大於一屏顯示數量時才自動輪播
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
  pause() // 手動操作時先暫停自動播放
  slide(dir > 0 ? props.stepCount : -props.stepCount)
  // 操作完後若滑鼠還在區域內，因為有 @mouseleave="play"，移出時會自動恢復播放
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

  // 鎖定動畫狀態
  isAnimating.value = true

  // 計算單次移動距離
  const dx = itemTotalWidth.value * n
  // 計算回繞點：原始資料的總長度
  const maxOffset = itemTotalWidth.value * brands.value.length

  offset.value += dx

  // 動畫結束後的檢查點 (0.4s 後)
  setTimeout(() => {
    // 右向前進：如果超過一半(原始長度)，瞬間跳回前面
    if (offset.value >= maxOffset) {
      noTransition.value = true
      // 減去 maxOffset，無縫接回第一組的對應位置
      offset.value -= maxOffset
      // 強制瀏覽器重繪後再開啟動畫
      nextTick(() => {
        // 這裡用一個微小延遲確保 no-transition 生效
        setTimeout(() => {
          noTransition.value = false
        }, 50)
      })
    }
    // 左向後退：如果小於 0，瞬間跳到後面
    else if (offset.value < 0) {
      noTransition.value = true
      offset.value += maxOffset
      nextTick(() => {
        setTimeout(() => {
          noTransition.value = false
        }, 50)
      })
    }

    isAnimating.value = false
  }, 500) // 略大於 CSS transition 時間 (0.4s)
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
.section-title {
  text-align: center;
  font-weight: 700;
  margin-bottom: 12px;
}
.carousel-viewport {
  position: relative;
  overflow: hidden;
  width: 100%;
  /* 增加一點內距避免陰影被切掉 */
  padding: 10px 0;
}
.carousel-track {
  display: flex;
  align-items: stretch;
  will-change: transform;
  transition: transform 0.4s ease-in-out;
}
/* 瞬間移動時關閉動畫 */
.carousel-track.no-transition {
  transition: none !important;
}

.brand-item {
  /* 寬度由 JS 動態控制 */
  flex-shrink: 0;
  margin: 0 8px; /* 左右各 8px，總間距 16px (對應 gapX) */
  text-align: center;
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
  /* 預設一點點灰階，hover 時變彩色 */
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

/* 控制按鈕優化 */
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
  line-height: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0; /* 預設隱藏 */
  transition: all 0.3s ease;
  z-index: 2;
}
/* 滑鼠移入 viewport 才顯示按鈕 */
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
