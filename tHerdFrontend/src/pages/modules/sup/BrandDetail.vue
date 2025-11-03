<!-- 先以預設順序渲染；
未來後端若回傳 orderedBlocks，就能依序渲染切換版位而不動骨架。 -->

<!-- src/pages/modules/sup/BrandDetail.vue -->
<!--
依 getBrandDetail 取回 Banner、Buttons、Accordions。
版面：
1) BrandBanner：顯示 Banner。
2) BrandButtons：顯示分類按鈕（背景取主色；hover 反轉）。
3) BrandMoreCard：了解更多卡片（左文群組、右圖；無圖時左側滿版）。
-->

<template>
  <section class="container py-3">
    <div class="content-wrap">
      <header class="d-flex align-items-center gap-2 mb-3">
        <h1 class="h4 m-0">{{ vm.brandName || '品牌' }}</h1>
      </header>

      <!-- Banner -->
      <BrandBanner v-if="vm.bannerUrl" :url="vm.bannerUrl" :alt="vm.brandName" class="mb-2" />

      <!-- 分類按鈕（有資料才顯示） -->
      <BrandButtons
        v-if="vm.buttons?.length"
        class="mb-2"
        :buttons="vm.buttons"
        :bg-rgb="vm.mainColor"
        @tap="onFilter"
      />

      <!-- 了解更多（有 Accordion 才顯示），按鈕配色固定 -->
      <!-- 未展開：分隔線 + 中央按鈕 -->
      <div v-if="vm.accordions?.length && !moreOpen" class="my-4">
        <div class="split-line">
          <button
            class="btn btn-sm"
            :style="{ backgroundColor: 'rgb(0,112,131)', color: 'rgb(248,249,250)' }"
            @click="moreOpen = true"
          >
            了解更多關於 {{ vm.brandName || '品牌' }}
          </button>
        </div>
      </div>

      <!-- 展開卡片 -->
      <BrandMoreCard
        v-if="vm.accordions?.length && moreOpen"
        class="mb-2 pt-3"
        :groups="vm.accordions"
        :images-right="imagesRight"
        :accent-rgb="vm.mainColor"
        :alt-text="vm.brandName"
      />

      <!-- 展開：下方分隔線 + 中央關閉按鈕 -->
      <div v-if="vm.accordions?.length && moreOpen" class="my-3">
        <div class="split-line">
          <button
            class="btn btn-sm"
            :style="{ backgroundColor: 'rgb(0,112,131)', color: 'rgb(248,249,250)' }"
            @click="moreOpen = false"
          >
            關閉
          </button>
        </div>
      </div>

      <!-- 除錯區：直接展示 imagesRight 清單 -->
      <!-- <div class="small text-muted">
      <div>Debug imagesRight count: {{ imagesRight.length }}</div>
      <div v-for="(u, i) in imagesRight" :key="i">{{ u }}</div>
    </div>
    <div class="small text-muted">right count: {{ imagesRight?.length }}</div>
    {{ moreOpen }} -->

      <!-- Loading / Empty -->
      <div v-if="loading" class="text-muted">載入中…</div>
      <div v-else-if="!loading && !vm.brandName" class="text-muted">查無品牌資料</div>
    </div>
  </section>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { getBrandDetail, getBrandContentImages } from '@/core/api/modules/sup/supBrands'
import { Vibrant } from 'node-vibrant/browser'

// 子元件
import BrandBanner from '@/components/modules/sup/brands/BrandBanner.vue'
import BrandButtons from '@/components/modules/sup/brands/BrandButtons.vue'
import BrandMoreCard from '@/components/modules/sup/brands/BrandMoreCard.vue'

// 容器頁狀態
const route = useRoute()
const loading = ref(false)
const moreOpen = ref(false)

const imagesRight = ref([])

const vm = ref({
  brandId: 0,
  brandName: '',
  bannerUrl: '',
  buttons: [],
  accordions: [], // 後端已以 Title 分組並排序（依 ContentId 最小值），組內 items 依 Order
  mainColor: { r: 0, g: 147, b: 171 },
})

const getLuma = ({ r, g, b }) => 0.2126 * r + 0.7152 * g + 0.0722 * b

// 以人口最大色近似「最大面積色」，排除過亮/過暗色
async function extractDominantByPopulation(imgUrl, fallback = { r: 0, g: 147, b: 171 }) {
  try {
    if (!imgUrl) return fallback
    const palette = await Vibrant.from(imgUrl).getPalette()
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

// 載入詳頁
const fetchDetail = async () => {
  loading.value = true
  try {
    const id = Number(route.params.brandId)
    // console.log('[BrandDetail] brandId param =', id)
    if (!Number.isInteger(id)) {
      //   console.warn('[BrandDetail] invalid brandId param')
      return
    }

    const resp = await getBrandDetail(id)
    // console.log('[BrandDetail] detail resp =', resp)
    const data = resp?.data?.data ?? null
    // console.log('[BrandDetail] detail data =', data)
    if (!data) {
      vm.value.brandId = id
      //   console.warn('[BrandDetail] empty detail data')
      return
    }

    // 安全排序
    const buttons = Array.isArray(data.buttons)
      ? [...data.buttons].sort((a, b) => a.order - b.order)
      : []
    const acc = Array.isArray(data.accordions)
      ? data.accordions.map((g) => ({
          ...g,
          items: [...g.items].sort((a, b) => a.order - b.order),
        }))
      : []

    vm.value = {
      brandId: data.brandId,
      brandName: data.brandName,
      bannerUrl: data.bannerUrl || '',
      buttons,
      accordions: acc,
      mainColor: vm.value.mainColor,
    }
    // console.log('[BrandDetail] vm after detail =', vm.value)

    // 主色（從 Banner 取）
    vm.value.mainColor = await extractDominantByPopulation(vm.value.bannerUrl)
    // console.log('[BrandDetail] mainColor =', vm.value.mainColor)

    // 右側圖片（不分組；可傳品牌名當 altText，也可略過讓後端預設）
    const imgsRes = await getBrandContentImages(vm.value.brandId, {
      folderId: 8,
      altText: vm.value.brandName,
    })
    // console.log('[BrandDetail] images resp =', imgsRes)
    const urls = imgsRes?.data?.data?.urls
    imagesRight.value = Array.isArray(urls) ? urls : []
    // console.log('[BrandDetail] imagesRight =', imagesRight.value)
  } catch (err) {
    console.error('[BrandDetail] fetchDetail error =', err)
  } finally {
    loading.value = false
  }
}

onMounted(fetchDetail)
watch(() => route.params.brandId, fetchDetail)

/* 點擊分類按鈕（依需求導頁或查詢） */
// TODO:分類導向
const onFilter = (btn) => {
  // e.g. router.push({ name: 'ProductList', query: { brandId: vm.value.brandId, typeId: btn.id } })
  //   console.log('[BrandDetail] click filter btn =', btn)
}
</script>

<style scoped>
/* 內容寬度與左右留白：大螢幕保留舒適邊距 */
.content-wrap {
  max-width: 1200px; /* 可調：1140~1320 */
  margin: 0 auto;
  padding-left: 1rem;
  padding-right: 1rem;
}
@media (min-width: 1400px) {
  .content-wrap {
    padding-left: 2rem;
    padding-right: 2rem;
  }
}

/* 中央分隔線：按鈕置中，左右延伸 */
.split-line {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin: 0.5rem 0;
}
.split-line::before,
.split-line::after {
  content: '';
  flex: 1 1 auto;
  height: 1px;
  background-color: #e9ecef;
}

/* 展開動畫（保留） */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
