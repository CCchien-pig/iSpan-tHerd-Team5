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
  <!-- 單一 template 根 -->
  <section class="container py-3">
    <div class="content-wrap">
      <!-- 版本/版型渲染 -->
      <template v-if="useLayoutMode">
        <section class="container py-3" v-if="useLayoutMode">
          <div class="content-wrap">
            <component
              v-for="(blk, i) in renderQueue"
              :key="i"
              :is="componentMap[blk.type]"
              v-bind="buildPropsByBlock(blk)"
              @tap="onFilter"
              @page-change="onPageChange"
              v-model:brandInfoAvailable="vm.brandInfoAvailable"
            />
          </div>
        </section>
      </template>

      <!-- 回退固定順序 -->
      <template v-else>
        <section class="container py-3">
          <div class="content-wrap">
            <header class="d-flex align-items-center justify-content-between mb-3">
              <div class="d-flex align-items-center gap-2">
                <h1 class="h4 m-0">{{ vm.brandName || '品牌' }}</h1>
              </div>

              <!-- 收藏愛心 -->
              <button
                class="brand-fav-btn"
                :aria-pressed="isFav(vm.brandId)"
                :title="isFav(vm.brandId) ? '已收藏' : '加入收藏'"
                @click="toggleFavOnDetail()"
              >
                <!-- 實心 -->
                <svg
                  v-if="isFav(vm.brandId)"
                  viewBox="0 0 24 24"
                  width="20"
                  height="20"
                  aria-hidden="true"
                >
                  <path
                    fill="currentColor"
                    d="M12.1 21.55 12 21.65l-.1-.1C7.14 17.24 4 14.39 4 11.5 4 9.5 5.5 8 7.5 8c1.54 0 3.04.99 3.57 2.36h1.87C13.46 8.99 14.96 8 16.5 8 18.5 8 20 9.5 20 11.5c0 2.89-3.14 5.74-7.9 10.05Z"
                  />
                </svg>
                <!-- 空心 -->
                <svg v-else viewBox="0 0 24 24" width="20" height="20" aria-hidden="true">
                  <path
                    fill="currentColor"
                    d="M16.5 3c-1.74 0-3.41.81-4.5 2.09C10.91 3.81 9.24 3 7.5 3 4.42 3 2 5.42 2 8.5c0 3.78 3.4 6.86 8.55 11.54L12 21.35l1.45-1.32C18.6 15.36 22 12.28 22 8.5 22 5.42 19.58 3 16.5 3Zm-4.4 15.55-.1.1-.1-.1C7.14 14.24 4 11.39 4 8.5 4 6.5 5.5 5 7.5 5c1.54 0 3.04.99 3.57 2.36h1.87C13.46 5.99 14.96 5 16.5 5 18.5 5 20 6.5 20 8.5c0 2.89-3.14 5.74-7.9 10.05Z"
                  />
                </svg>
              </button>
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
            <div
              v-show="moreOpen && (vm.accordions?.length || vm.brandInfoAvailable)"
              class="mb-2 pt-3"
            >
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
    </div>
  </section>
</template>

<script setup>
import axios from 'axios'
import { ref, onMounted, watch, nextTick, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { notify as toast } from 'notiwind'

import { getBrandDetail, getBrandContentImages } from '@/core/api/modules/sup/supBrands'
import { Vibrant } from 'node-vibrant/browser'

import BrandBanner from '@/components/modules/sup/brands/BrandBanner.vue'
import BrandButtons from '@/components/modules/sup/brands/BrandButtons.vue'
import BrandInfo from '@/components/modules/sup/brands/BrandInfo.vue'
import BrandMoreCard from '@/components/modules/sup/brands/BrandMoreCard.vue'
import ProductList from '@/components/modules/prod/list/ProductList.vue'
import BrandArticle from '@/components/modules/sup/brands/BrandArticle.vue'

const route = useRoute()
const auth = useAuthStore()

const loading = ref(false)
const moreOpen = ref(false)
const moreAnchor = ref(null)

const imagesRight = ref([])

// 建立對照表與狀態
const componentMap = {
  Banner: BrandBanner,
  Buttons: BrandButtons,
  BrandInfo: BrandInfo,
  Accordion: BrandMoreCard, // 後端 type=Accordion，前端用 BrandMoreCard 呈現群組
  Article: BrandArticle,
  ProductList: ProductList,
  // Unknown: 可建立一個簡單的占位元件
}
const layout = ref({
  layoutId: null,
  version: null,
  blocks: [], // 來源：/api/brands/{id}/layout/active 的 layoutJson
})
const useLayoutMode = computed(
  () => Array.isArray(layout.value.blocks) && layout.value.blocks.length > 0,
)

const DEFAULT_RGB = { r: 0, g: 147, b: 171 }
const vm = ref({
  brandId: 0,
  brandName: '',
  bannerUrl: '',
  buttons: [],
  accordions: [],
  mainColor: { ...DEFAULT_RGB },
  brandInfoAvailable: false,
})

const products = ref([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = 40

// 收藏狀態
const favorites = ref(new Set())
const isFav = (id) => favorites.value.has(id)

// 載入我的品牌收藏
async function loadFavorites() {
  if (!auth.isAuthenticated) return
  try {
    const res = await axios.get('/api/sup/BrandFavorites/my', {
      headers: { Authorization: `Bearer ${auth.accessToken}` },
    })
    if (res.data?.success && Array.isArray(res.data.data)) {
      favorites.value = new Set(res.data.data.map((x) => x.brandId))
    }
  } catch (e) {
    // 靜默忽略
    console.error('[BrandDetail] loadFavorites error =', e)
  }
}

// 詳情頁右上角收藏/取消收藏
async function toggleFavOnDetail() {
  const id = vm.value.brandId
  if (!id) return
  if (!auth.isAuthenticated) {
    toast({ text: '請先登入會員', type: 'error', duration: 2000, group: 'bottom-center' })
    return
  }
  try {
    if (isFav(id)) {
      const res = await axios.delete(`/api/sup/BrandFavorites/${id}`, {
        headers: { Authorization: `Bearer ${auth.accessToken}` },
      })
      if (res.data?.success) {
        favorites.value.delete(id)
        toast({ text: '已移除收藏', type: 'success', duration: 2000, group: 'bottom-center' })
      } else {
        toast({
          text: res.data?.message || '取消收藏失敗',
          type: 'error',
          duration: 2000,
          group: 'bottom-center',
        })
      }
    } else {
      const res = await axios.post(
        '/api/sup/BrandFavorites',
        { brandId: id },
        {
          headers: { Authorization: `Bearer ${auth.accessToken}` },
        },
      )
      if (res.data?.success) {
        favorites.value.add(id)
        toast({ text: '已加入收藏', type: 'success', duration: 2000, group: 'bottom-center' })
      } else {
        toast({
          text: res.data?.message || '加入收藏失敗',
          type: 'error',
          duration: 2000,
          group: 'bottom-center',
        })
      }
    }
  } catch (err) {
    toast({
      text: err?.response?.data?.message || '操作失敗',
      type: 'error',
      duration: 2000,
      group: 'bottom-center',
    })
  }
}

const getLuma = ({ r, g, b }) => 0.2126 * r + 0.7152 * g + 0.0722 * b

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
    brandInfoAvailable: false,
  }
  products.value = []
  totalCount.value = 0
  currentPage.value = 1
}

const syncExpandFromRoute = () => {
  moreOpen.value = String(route.query.expand || '') === '1'
}

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

    const detected = await extractDominantByPopulation(vm.value.bannerUrl, DEFAULT_RGB)
    vm.value.mainColor = detected

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

// 三支取資料的函式
async function fetchAccordionById(contentId) {
  // 若後端已在 getBrandDetail 帶到 vm.accordions，可本地過濾：
  const groups = Array.isArray(vm.value.accordions) ? vm.value.accordions : []
  return groups.find((g) => g.contentId === contentId || g.id === contentId)
    ? [groups.find((g) => g.contentId === contentId || g.id === contentId)]
    : []
  // 或直接呼叫：/api/sup/BrandAccordion/{contentId}
}
async function fetchArticleById(contentId) {
  const res = await axios.get(`/api/cms/articles/${contentId}`)
  if (res.data?.success) return res.data.data?.html || ''
  return ''
}
async function fetchBannerById(brandId) {
  // 若 contentId 對應 ImgId 或資產檔，請呼叫你們的資產 API 取 FileUrl
  // api/sup/Brands/1085/detail
  // 回傳 { url: '...', alt: '...' }
  const res = await axios.get(`/api/sup/Brands/${brandId}/detail`)
  if (res.data?.success)
    return { url: res.data.data?.fileUrl || '', alt: res.data.data?.altText || '' }
  return { url: '', alt: '' }
}

async function fetchActiveLayout(brandId) {
  if (!brandId) return
  try {
    const resp = await axios.get(`/api/sup/Brands/${brandId}/layout/active`)
    const data = resp?.data?.data
    if (data?.layoutJson) {
      const blocks = Array.isArray(data.layoutJson) ? data.layoutJson : JSON.parse(data.layoutJson)
      layout.value = { layoutId: data.layoutId, version: data.layoutVersion, blocks }
    } else {
      layout.value = { layoutId: null, version: null, blocks: [] }
    }
  } catch {
    layout.value = { layoutId: null, version: null, blocks: [] }
  }
}

// 架一個 props 建構器，讓 block 取用對應資料
async function buildPropsByBlock(blk) {
  switch (blk.type) {
    case 'Banner': {
      const { url, alt } = blk.contentId
        ? await fetchBannerById(blk.contentId)
        : { url: vm.value.bannerUrl, alt: vm.value.brandName }
      return { url, alt: alt || vm.value.brandName, linkUrl: blk.linkUrl || '', class: 'mb-2' }
    }
    case 'Buttons':
      return { buttons: vm.value.buttons || [], bgRgb: vm.value.mainColor, class: 'mb-2' }
    case 'BrandInfo':
      return { brandId: vm.value.brandId, class: 'mb-3' }
    case 'Accordion': {
      const groups = blk.contentId
        ? await fetchAccordionById(blk.contentId)
        : vm.value.accordions || []
      return {
        groups,
        imagesRight: true,
        accentRgb: vm.value.mainColor,
        altText: vm.value.brandName,
      }
    }
    case 'Article':
      return { contentId: blk.contentId, brandId: vm.value.brandId }
    case 'ProductList':
      return { products, totalCount, pageSize, pageIndex: currentPage }
    default:
      return {}
  }
}

//  block 必須「有資料才顯示」
const orderedBlocks = computed(() => {
  return (layout.value.blocks || []).filter((b) => {
    if (b.type === 'Buttons') return vm.value.buttons?.length
    if (b.type === 'Accordion') return vm.value.accordions?.length
    if (b.type === 'Banner') return !!vm.value.bannerUrl
    // 其餘照顯
    return true
  })
})

// 先把 blocks 轉換成「已綁資料的渲染隊列」
// 新增一個 reactive 陣列 renderQueue，進入版本模式後將每個 block 轉為 { comp, props }
const renderQueue = ref([])

async function buildRenderQueue() {
  renderQueue.value = []
  for (const blk of layout.value.blocks || []) {
    if (!componentMap[blk.type]) continue
    const props = await buildPropsByBlock(blk) // 這裡已取到實際資料
    // 資料檢查（無資料不顯示）
    if (blk.type === 'Banner' && !props.url) continue
    if (blk.type === 'Buttons' && !props.buttons?.length) continue
    if (blk.type === 'Accordion' && !props.groups?.length) continue
    renderQueue.value.push({ comp: componentMap[blk.type], props })
  }
}

const fetchBrandProducts = async (page = 1) => {
  loading.value = true
  try {
    const brandId = Number(route.params.brandId)
    if (!brandId) return

    const filter = { BrandId: brandId, PageIndex: page, PageSize: pageSize }
    const resp = await axios.post('/api/prod/Products/search', filter)
    if (resp.data?.data) {
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

const onPageChange = (page) => {
  fetchBrandProducts(page)
}
const openMore = () => {
  moreOpen.value = true
}
const closeMoreAndScrollToTop = async () => {
  moreOpen.value = false
  await nextTick()
  if (moreAnchor.value) moreAnchor.value.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

onMounted(async () => {
  resetStateForNewPage()
  syncExpandFromRoute()
  await fetchDetail()
  console.log(vm.value.brandId)
  await fetchActiveLayout(vm.value.brandId) // 放在 fetchDetail 後，確保 brandName 等資料已就緒
  await buildRenderQueue() // ← 新增
  fetchBrandProducts(1)
  loadFavorites() // 載入使用者收藏
})

watch(
  () => route.fullPath,
  async () => {
    resetStateForNewPage()
    syncExpandFromRoute()
    await fetchDetail()
    await fetchActiveLayout(vm.value.brandId)
    fetchBrandProducts(1)
    loadFavorites()
  },
)

// 導出供 template 綁定
const brandIsFav = computed(() => isFav(vm.value.brandId))

// BrandDetail.vue <script setup>
const onFilter = (btn) => {
  // TODO: 分類導向或查詢
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

.brand-fav-btn {
  border: none;
  background: transparent;
  color: #e63946; /* 主色可依設計 */
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border: 3px solid #e5e7eb;
  border-radius: 50%;
  transition:
    background-color 0.2s ease,
    transform 0.05s ease;
}
.brand-fav-btn:hover {
  background: rgba(230, 57, 70, 0.08);
}
.brand-fav-btn:active {
  transform: scale(0.96);
}
.brand-fav-btn[aria-pressed='true'] {
  color: #e03131;
}
</style>
