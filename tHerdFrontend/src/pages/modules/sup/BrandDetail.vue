<!-- 依 getBrandDetail API 取回 Banner、Buttons、Accordion，
先以預設順序渲染；
未來後端若回傳 orderedBlocks，就能依序渲染切換版位而不動骨架。 -->

<!-- src/pages/modules/sup/BrandDetail.vue -->
<!--
依 getBrandDetail 取回 Banner、Buttons、Accordions。
版面：
1) Banner
2) 分類按鈕（自動取主色；無資料則不顯示）
3) 了解更多（展開卡片：同 ContentTitle 一組，內含 li）
-->

<template>
  <section class="container py-3">
    <!-- 標題 -->
    <header class="d-flex align-items-center gap-2 mb-3">
      <h1 class="h4 m-0">{{ detail?.brandName || '品牌' }}</h1>
    </header>

    <!-- Banner -->
    <div v-if="detail?.bannerUrl" class="mb-4">
      <img :src="detail.bannerUrl" :alt="detail.brandName" class="img-fluid w-100 rounded" />
    </div>

    <!-- 分類按鈕：有資料才顯示 -->
    <div
      v-if="hasButtons"
      class="brand-buttons-wrap mb-4 py-3 rounded"
      :style="{ backgroundColor: buttonsBg }"
    >
      <nav class="d-flex flex-wrap gap-2 justify-content-center">
        <button
          v-for="btn in detail.buttons"
          :key="btn.id"
          type="button"
          class="brand-btn btn-sm fw-semibold"
          :style="{
            '--hover-text': hoverTextColor, // 主色字
            '--btn-text': buttonsTextColor, // 預設字色（白或深）
          }"
          @click="onFilter(btn)"
        >
          {{ btn.text }}
        </button>
      </nav>
    </div>

    <!-- 了解更多（展開） -->
    <div class="mb-3 text-center">
      <button
        class="btn btn-sm"
        :style="{
          backgroundColor: 'rgb(0,112,131)',
          color: 'rgb(248,249,250)',
        }"
        @click="moreOpen = !moreOpen"
      >
        了解更多關於 {{ detail?.brandName || '品牌' }}
      </button>
    </div>

    <!-- 展開內容（單一卡片；每組一個標題，組內 li 條列） -->
    <transition name="fade">
      <div v-if="moreOpen" class="brand-more mb-4">
        <div class="card">
          <div class="card-body">
            <section v-for="grp in detail?.accordions || []" :key="grp.contentKey" class="mb-3">
              <h3 class="h6 mb-2">{{ findGroupTitle(grp) }}</h3>
              <ul class="list-unstyled m-0">
                <li
                  v-for="item in grp.items"
                  :key="`${grp.contentKey}-${item.order}`"
                  class="d-flex align-items-start gap-2 py-1"
                >
                  <!-- 可替換為專案 SVG Icon -->
                  <span class="check-dot" :style="{ backgroundColor: accentDot }"></span>
                  <span v-html="item.body"></span>
                </li>
              </ul>
            </section>
          </div>
        </div>
      </div>
    </transition>

    <!-- Loading / Empty -->
    <div v-if="loading" class="text-muted">載入中…</div>
    <div v-else-if="!loading && !detail" class="text-muted">查無品牌資料</div>
  </section>
</template>

<script setup>
/* Imports */
import { ref, onMounted, watch, computed } from 'vue'
import { useRoute } from 'vue-router'
import { getBrandDetail } from '@/core/api/modules/sup/supBrands'
import { Vibrant } from 'node-vibrant/browser'

/* 顏色工具 */
const DEFAULT_BG = { r: 0, g: 147, b: 171 }
const toRgb = ({ r, g, b }) => `rgb(${r}, ${g}, ${b})`
const getLuma = ({ r, g, b }) => 0.2126 * r + 0.7152 * g + 0.0722 * b
const pickTextColor = (bg, light = '#ffffff', dark = '#0b3a3f') =>
  getLuma(bg) > 180 ? dark : light

// 以 population 最大的色塊近似「最大面積色」，排除過亮/過暗
async function extractDominantByPopulation(imgUrl, fallback = DEFAULT_BG) {
  try {
    if (!imgUrl) return fallback
    const palette = await Vibrant.from(imgUrl)
      .maxColorCount(128) // 提高色數
      .quality(3) // 提升品質（1~5，數字越小品質越高耗時越長）
      .getPalette()

    const swatches = Object.values(palette || {}).filter(Boolean)
    if (!swatches.length) return fallback
    swatches.sort((a, b) => (b.population || 0) - (a.population || 0))
    const isBad = (rgb) => {
      const [r, g, b] = rgb
      const luma = getLuma({ r, g, b })
      return luma < 40 || luma > 235
    }
    const pick = swatches.find((s) => s.rgb && !isBad(s.rgb)) || swatches[0]
    const [r, g, b] = pick.rgb
    return { r: Math.round(r), g: Math.round(g), b: Math.round(b) }
  } catch {
    return fallback
  }
}

/* 狀態 */
const route = useRoute()
const detail = ref(null)
const loading = ref(false)
const moreOpen = ref(false)

// 主色與按鈕配色（容器背景＝主色；按鈕透明；字色自動；hover 反轉白底＋主色字）
const mainColor = ref(DEFAULT_BG)
const buttonsBg = computed(() => toRgb(mainColor.value))
const buttonsTextColor = computed(() => pickTextColor(mainColor.value))
const hoverTextColor = computed(() => toRgb(mainColor.value)) // 反轉時用主色字
const accentDot = computed(() => toRgb(mainColor.value)) // 展開 li 前的小圓點

const hasButtons = computed(
  () => Array.isArray(detail.value?.buttons) && detail.value.buttons.length > 0,
)

/* 方法 */
const setButtonsBgFromBanner = async (url) => {
  mainColor.value = await extractDominantByPopulation(url, DEFAULT_BG)
}

const fetchDetail = async () => {
  loading.value = true
  try {
    const id = Number(route.params.brandId)
    if (!Number.isInteger(id)) return
    const res = await getBrandDetail(id)
    const data = res?.data?.data ?? res?.data ?? null

    // 排序保險
    if (data?.buttons?.length) data.buttons = [...data.buttons].sort((a, b) => a.order - b.order)
    if (data?.accordions?.length) {
      // 後端請以 ContentTitle 分組；若仍為 contentKey，同樣可呈現
      data.accordions = data.accordions.map((g) => ({
        ...g,
        items: [...g.items].sort((a, b) => a.order - b.order),
      }))
    }

    detail.value = data
    if (data?.brandName) document.title = `${data.brandName}｜品牌`

    await setButtonsBgFromBanner(data?.bannerUrl)
  } finally {
    loading.value = false
  }
}

onMounted(fetchDetail)
watch(() => route.params.brandId, fetchDetail)

// 以第一筆 item.title 作為組標題
const findGroupTitle = (grp) => grp?.items?.[0]?.title || '更多資訊'

// 點擊分類按鈕（依需求導頁或過濾）
const onFilter = (btn) => {
  // 例如：router.push({ name:'ProductList', query:{ brandId: detail.value?.brandId, typeId: btn.id } })
}
</script>

<style scoped>
/* Buttons 容器 */
.brand-buttons-wrap {
  text-align: center;
}

/* Buttons：透明背景；hover 反轉白底＋主色字（透過 CSS 變數 --hover-text） */
.brand-btn {
  background: transparent;
  border: 1px solid rgba(255, 255, 255, 0.2);
  color: var(--btn-text, #fff);
  transition:
    background-color 0.15s ease,
    color 0.15s ease,
    border-color 0.15s ease;
}
.brand-btn:hover {
  background: #fff;
  color: var(--hover-text, #0793ab);
  border-color: rgba(0, 0, 0, 0.08);
}

/* 展開小圓點（可改成 SVG Icon） */
.check-dot {
  width: 8px;
  height: 8px;
  display: inline-block;
  border-radius: 50%;
  margin-top: 0.45rem; /* 與行高對齊 */
}

/* 展開動畫 */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
