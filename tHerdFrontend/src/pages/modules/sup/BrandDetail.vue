<!-- 先以預設順序渲染；
未來後端若回傳 orderedBlocks，就能依序渲染切換版位而不動骨架。 -->

<!-- src/pages/modules/sup/BrandDetail.vue -->
<!--
依 getBrandDetail 取回 Banner、Buttons、Accordions。
版面：
1) BrandBanner：顯示 Banner。
2) BrandButtons：顯示分類按鈕（背景取主色；hover 反轉）。

-- 了解更多展開:(3/4 皆無時不顯示)
3) BrandInfo：產品資訊框
4) BrandMoreCard：卡片（左文群組、右圖；無圖時左側滿版）。
---
5) ProductList：產品清單卡片
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

      <!-- 了解更多（按鈕及折疊） -->
      <!-- 未展開：分隔線 + 中央按鈕 -->
      <div v-if="(vm.accordions?.length || vm.brandInfoAvailable) && !moreOpen" class="my-4">
        <div class="split-line anchor-to-top" ref="moreAnchor">
          <button
            class="btn btn-sm"
            :style="{ backgroundColor: 'rgb(0,112,131)', color: 'rgb(248,249,250)' }"
            @click="openMore"
          >
            了解更多關於 {{ vm.brandName || '品牌' }}
          </button>
        </div>
      </div>

      <!-- 展開卡片區域 -->
      <div v-show="moreOpen && (vm.accordions?.length || vm.brandInfoAvailable)" class="mb-2 pt-3">
        <BrandInfo
          v-if="vm.brandId"
          :brand-id="vm.brandId"
          class="mb-3"
          v-model:brandInfoAvailable="vm.brandInfoAvailable"
        />

        <BrandMoreCard
          v-if="vm.accordions?.length"
          :groups="vm.accordions"
          :images-right="imagesRight"
          :accent-rgb="vm.mainColor"
          :alt-text="vm.brandName"
        />
      </div>

      <!-- 展開：下方分隔線 + 中央關閉按鈕 -->
      <div v-if="moreOpen && (vm.accordions?.length || vm.brandInfoAvailable)" class="my-3">
        <div class="split-line">
          <button
            class="btn btn-sm"
            :style="{ backgroundColor: 'rgb(0,112,131)', color: 'rgb(248,249,250)' }"
            @click="closeMoreAndScrollToTop"
          >
            關閉
          </button>
        </div>
      </div>

      <!-- 品牌產品列表，放在banner下方 -->
      <ProductList
        :products="products"
        :totalCount="totalCount"
        :pageSize="pageSize"
        :pageIndex="currentPage"
        @page-change="onPageChange"
      />

      <!-- Loading / Empty -->
      <div v-if="loading" class="text-muted">載入中…</div>
      <div v-else-if="!loading && !vm.brandName" class="text-muted">查無品牌資料</div>
    </div>
  </section>
</template>

<script setup>
import { ref, onMounted, watch, nextTick } from 'vue'
import { useRoute } from 'vue-router'
import { getBrandDetail, getBrandContentImages } from '@/core/api/modules/sup/supBrands'
import axios from 'axios'
import { Vibrant } from 'node-vibrant/browser'

// 子元件
import BrandBanner from '@/components/modules/sup/brands/BrandBanner.vue'
import BrandButtons from '@/components/modules/sup/brands/BrandButtons.vue'
import BrandInfo from '@/components/modules/sup/brands/BrandInfo.vue'

import BrandMoreCard from '@/components/modules/sup/brands/BrandMoreCard.vue'
import ProductList from '@/components/modules/prod/list/ProductList.vue'

// 容器頁狀態
const route = useRoute()
const loading = ref(false)
const moreOpen = ref(false)
const moreAnchor = ref(null)

const imagesRight = ref([])

// 預設主色
const DEFAULT_RGB = { r: 0, g: 147, b: 171 }
const vm = ref({
  brandId: 0,
  brandName: '',
  bannerUrl: '',
  buttons: [],
  accordions: [],
  mainColor: { ...DEFAULT_RGB },
  brandInfoAvailable: false, // [新增] 明確初始化
})

// 產品列表狀態與分頁
const products = ref([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = 40

const getLuma = ({ r, g, b }) => 0.2126 * r + 0.7152 * g + 0.0722 * b

// 以入口最大色近似「最大面積色」，排除過亮/過暗色
async function extractDominantByPopulation(imgUrl, fallback = { ...DEFAULT_RGB }) {
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

/* 進入新頁或路由切換時，重置狀態，避免顏色殘留 */
function resetStateForNewPage() {
  loading.value = false
  moreOpen.value = false
  imagesRight.value = []
  vm.value = {
    brandId: 0,
    brandName: '',
    bannerUrl: '',
    buttons: [],
    accordions: [],
    mainColor: { ...DEFAULT_RGB },
    brandInfoAvailable: false, // [新增] 重置時也還原為 false
  }
  products.value = []
  totalCount.value = 0
  currentPage.value = 1
}

/* 依路由 query 控制展開 */
const syncExpandFromRoute = () => {
  moreOpen.value = String(route.query.expand || '') === '1'
}

// 取得品牌詳情及Banner等資料
const fetchDetail = async () => {
  loading.value = true
  try {
    const id = Number(route.params.brandId)
    if (!Number.isInteger(id)) return

    const resp = await getBrandDetail(id)
    const data = resp?.data?.data ?? null
    if (!data) {
      vm.value.brandId = id
      return
    }

    // 先重置主色，避免使用上一頁的顏色先行渲染
    vm.value.mainColor = { ...DEFAULT_RGB }

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
      mainColor: { ...DEFAULT_RGB },
    }

    // 重新偵測本頁 Banner 主色（每頁都跑一次）
    const detected = await extractDominantByPopulation(vm.value.bannerUrl, DEFAULT_RGB)
    vm.value.mainColor = detected

    // 載右側圖
    const imgsRes = await getBrandContentImages(vm.value.brandId, {
      folderId: 8,
      altText: vm.value.brandName,
    })
    const urls = imgsRes?.data?.data?.urls
    imagesRight.value = Array.isArray(urls) ? urls : []
  } catch (err) {
    console.error('[BrandDetail] fetchDetail error =', err)
  } finally {
    loading.value = false
  }
}

/**
 * 取得品牌產品清單(依品牌id分頁查詢)
 * 使用/api/prod/Products/search API
 */
const fetchBrandProducts = async (page = 1) => {
  loading.value = true
  try {
    const brandId = Number(route.params.brandId)
    if (!brandId) return

    const filter = {
      BrandId: brandId,
      PageIndex: page,
      PageSize: pageSize,
    }

    const resp = await axios.post('/api/prod/Products/search', filter)
    if (resp.data && resp.data.data) {
      products.value = resp.data.data.items || []
      totalCount.value = resp.data.data.totalCount || 0
      currentPage.value = page
    } else {
      products.value = []
      totalCount.value = 0
      currentPage.value = 1
    }
  } catch (err) {
    console.error('[BrandDetail] fetchBrandProducts error =', err)
    products.value = []
    totalCount.value = 0
  } finally {
    loading.value = false
  }
}

// 分頁切換事件
const onPageChange = (page) => {
  fetchBrandProducts(page)
}

// 打開了解更多
const openMore = () => {
  moreOpen.value = true
}

// 關閉了解更多並滾動到目標位置
const closeMoreAndScrollToTop = async () => {
  moreOpen.value = false
  await nextTick()
  if (moreAnchor.value) {
    moreAnchor.value.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }
}

// 頁面進入時初始化資料
onMounted(() => {
  resetStateForNewPage()
  syncExpandFromRoute()
  fetchDetail()
  fetchBrandProducts(1)
})

// 監聽路由切換，重新加載品牌資料與產品清單
watch(
  () => route.fullPath,
  () => {
    resetStateForNewPage()
    syncExpandFromRoute()
    fetchDetail()
    fetchBrandProducts(1)
  },
)

// 點擊分類按鈕（依需求導頁或查詢）
const onFilter = (btn) => {
  // TODO:分類導向功能待實作
  // router.push({ name: 'ProductList', query: { brandId: vm.value.brandId, typeId: btn.id } })}
  console.log('[BrandDetail] click filter btn =', btn)
}
</script>

<style scoped>
.anchor-to-top {
  scroll-margin-top: 80px;
}

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
