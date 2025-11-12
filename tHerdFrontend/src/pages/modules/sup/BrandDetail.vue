<!-- src/pages/modules/sup/BrandDetail.vue -->
<template>
  <section class="container py-3">
    <div class="content-wrap">
      <header class="d-flex align-items-center justify-content-between gap-2 mb-3">
        <!-- 左側：品牌名稱 -->
        <h1 class="h4 m-0">{{ vm.brandName || '品牌' }}</h1>

        <!-- 右側：收藏按鈕 -->
        <button
          class="fav-btn"
          :aria-pressed="isFav"
          @click.stop="toggleFav"
          :title="isFav ? '已收藏' : '加入收藏'"
        >
          <svg v-if="isFav" viewBox="0 0 24 24" width="18" height="18" aria-hidden="true">
            <path
              fill="currentColor"
              d="M12.1 21.55 12 21.65l-.1-.1C7.14 17.24 4 14.39 4 11.5 4 9.5 5.5 8 7.5 8c1.54 0 3.04.99 3.57 2.36h1.87C13.46 8.99 14.96 8 16.5 8 18.5 8 20 9.5 20 11.5c0 2.89-3.14 5.74-7.9 10.05Z"
            />
          </svg>
          <svg v-else viewBox="0 0 24 24" width="15" height="15" aria-hidden="true">
            <path
              fill="currentColor"
              d="M16.5 3c-1.74 0-3.41.81-4.5 2.09C10.91 3.81 9.24 3 7.5 3 4.42 3 2 5.42 2 8.5c0 3.78 3.4 6.86 8.55 11.54L12 21.35l1.45-1.32C18.6 15.36 22 12.28 22 8.5 22 5.42 19.58 3 16.5 3Zm-4.4 15.55-.1.1-.1-.1C7.14 14.24 4 11.39 4 8.5 4 6.5 5.5 5 7.5 5c1.54 0 3.04.99 3.57 2.36h1.87C13.46 5.99 14.96 5 16.5 5 18.5 5 20 6.5 20 8.5c0 2.89-3.14 5.74-7.9 10.05Z"
            />
          </svg>
        </button>
      </header>

      <!-- 🔸 折扣活動條 -->
      <div
        v-if="discountInfo"
        class="discount-bar text-center py-2 px-3 fw-semibold"
        :style="barStyle"
      >
        <span class="me-2"> {{ displayRate }} 品牌特惠中 </span>
        <span v-if="discountInfo.endDate">
          至 {{ new Date(discountInfo.endDate).toLocaleDateString() }}
        </span>
        <template v-if="discountInfo.note"> ｜{{ discountInfo.note }} </template>
      </div>

      <!-- 固定第一排 Banner -->
      <BrandBanner
        v-if="vm.bannerUrl"
        :url="vm.bannerUrl"
        :alt="vm.brandName"
        :link-url="layoutBlocks.find((b) => b.type === 'Banner')?.data?.linkUrl"
        :main-color="vm.mainColor"
      />

      <!-- 固定第二排 分類按鈕 -->
      <BrandButtons
        v-if="vm.buttons?.length"
        class="mb-3 mt-1"
        :buttons="vm.buttons"
        :bg-rgb="vm.mainColor"
        @tap="onFilter"
      />

      <!-- 🟢 了解更多按鈕：只在未展開時顯示 -->
      <div v-if="!moreOpen" class="my-4">
        <div class="split-line anchor-to-top" ref="moreAnchor">
          <button class="btn btn-sm btn-toggle" @click="openMore">
            了解更多關於 {{ vm.brandName || '品牌' }}
          </button>
        </div>
      </div>

      <!-- 🟣 展開內容 -->
      <transition name="fade">
        <div v-show="moreOpen" class="mb-4 pt-3">
          <!-- 固定第一個 BrandInfo -->
          <BrandInfo
            v-if="vm.brandId"
            ref="infoSection"
            :brand-id="vm.brandId"
            class="mb-3"
            v-model:brandInfoAvailable="vm.brandInfoAvailable"
          />

          <!-- 若有 Layout 設定 -->
          <template v-if="layoutBlocks.length">
            <section
              v-for="blk in layoutBlocks"
              :key="`${blk.type}-${blk.data?.contentId}`"
              class="mb-4"
            >
              <BrandAccordionBlock
                v-if="blk.type === 'Accordion'"
                :content="blk.data"
                :accent-rgb="vm.mainColor"
              />
              <BrandArticleBlock
                v-else-if="blk.type === 'Article'"
                :content="blk.data"
                :accent-rgb="vm.mainColor"
              />
            </section>
          </template>

          <!-- 若無 Layout 設定 -->
          <template v-else>
            <BrandMoreCard
              v-if="Array.isArray(vm.accordions) && vm.accordions.length"
              :groups="vm.accordions"
              :images-right="imagesRight"
              :accent-rgb="vm.mainColor"
              :alt-text="vm.brandName"
            />
          </template>

          <!-- ✅ 關閉按鈕：移至展開內容底部 -->
          <div class="split-line mt-4">
            <button class="btn btn-sm btn-toggle" @click="closeMoreAndScrollToTop">關閉</button>
          </div>
        </div>
      </transition>

      <!-- 固定最後一排 商品清單 -->
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
import { ref, onMounted, watch, nextTick, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import axios from 'axios'
import { Vibrant } from 'node-vibrant/browser'
import { getBrandDetail, getBrandContentImages } from '@/core/api/modules/sup/supBrands'
import { useAuthStore } from '@/stores/auth'
import { notify } from 'notiwind'

// 子元件
import BrandBanner from '@/components/modules/sup/brands/BrandBanner.vue'
import BrandButtons from '@/components/modules/sup/brands/BrandButtons.vue'
import BrandInfo from '@/components/modules/sup/brands/BrandInfo.vue'
import BrandAccordionBlock from '@/components/modules/sup/brands/BrandAccordionBlock.vue'
import BrandArticleBlock from '@/components/modules/sup/brands/BrandArticleBlock.vue'
import BrandMoreCard from '@/components/modules/sup/brands/BrandMoreCard.vue'
import ProductList from '@/components/modules/prod/list/ProductList.vue'

// === 狀態 ===
const route = useRoute()
const router = useRouter()
const loading = ref(false)
const imagesRight = ref([])
const layoutBlocks = ref([])

const discountInfo = ref(null)

const DEFAULT_RGB = { r: 0, g: 147, b: 171 }
const vm = ref({
  brandId: 0,
  brandName: '',
  bannerUrl: '',
  buttons: [],
  accordions: [],
  mainColor: { ...DEFAULT_RGB },
  brandInfoAvailable: true,
})

const products = ref([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = 40

const auth = useAuthStore()
const isFav = ref(false)

// 色彩分析
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

// 取得 layout 設定與對應內容
const fetchActiveLayout = async (brandId) => {
  try {
    const res = await axios.get(`/api/sup/Brands/${brandId}/layout/active`)
    if (!res?.data?.layoutJson) {
      layoutBlocks.value = []
      return
    }

    const layoutItems = JSON.parse(res.data.layoutJson)
    const promises = layoutItems.map(async (block) => {
      switch (block.type) {
        case 'Banner': {
          // 🟢 Banner 不需額外 API，只要保留 linkUrl
          // console.log('[Banner Block]', block)
          return { type: 'Banner', data: { linkUrl: block.linkUrl } }
        }
        case 'Accordion': {
          const a = await axios.get(`/api/sup/Brands/${brandId}/accordion/${block.contentId}`)
          // console.log('[Accordion API raw]', a.data)
          // 🟢 直接解構成正確格式
          return { type: 'Accordion', data: a.data.data }
        }
        case 'Article': {
          const a = await axios.get(`/api/sup/Brands/${brandId}/article/${block.contentId}`)
          // console.log('[Article API raw]', a.data)
          return { type: 'Article', data: a.data.data }
        }
        default:
          return null
      }
    })

    layoutBlocks.value = (await Promise.all(promises)).filter(Boolean)
    // console.log('[Layout Blocks Final]', layoutBlocks.value)
  } catch (e) {
    if (e?.response?.status !== 404) console.error('[BrandDetail] fetchActiveLayout failed', e)
    layoutBlocks.value = []
  }
}

// 品牌詳情
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
      brandInfoAvailable: true,
    }

    const detected = await extractDominantByPopulation(vm.value.bannerUrl, DEFAULT_RGB)
    vm.value.mainColor = detected

    const imgsRes = await getBrandContentImages(vm.value.brandId, {
      folderId: 8,
      altText: vm.value.brandName,
    })
    const urls = imgsRes?.data?.data?.urls
    imagesRight.value = Array.isArray(urls) ? urls : []

    await fetchActiveLayout(vm.value.brandId)
  } catch (err) {
    console.error('[BrandDetail] fetchDetail error =', err)
  } finally {
    loading.value = false
  }

  await fetchBrandDiscount(vm.value.brandId)
  await loadFavoriteStatus()
}

// 🔸 載入收藏狀態
async function loadFavoriteStatus() {
  if (!auth.isAuthenticated) return
  try {
    const res = await axios.get('/api/sup/BrandFavorites/my', {
      headers: { Authorization: `Bearer ${auth.accessToken}` },
    })
    if (res.data.success && Array.isArray(res.data.data)) {
      const myFavs = res.data.data.map((f) => f.brandId)
      isFav.value = myFavs.includes(vm.value.brandId)
    }
  } catch (err) {
    console.error('[BrandDetail] 載入收藏狀態失敗', err)
  }
}

// 🔸 切換收藏
async function toggleFav() {
  if (!auth.isAuthenticated) {
    notify({ text: '請先登入會員', type: 'error', group: 'bottom-center' })
    return
  }

  try {
    if (isFav.value) {
      const res = await axios.delete(`/api/sup/BrandFavorites/${vm.value.brandId}`, {
        headers: { Authorization: `Bearer ${auth.accessToken}` },
      })
      if (res.data.success) {
        isFav.value = false
        notify({ text: '已移除收藏', type: 'success', group: 'bottom-center' })
      }
    } else {
      const res = await axios.post(
        '/api/sup/BrandFavorites',
        { brandId: vm.value.brandId },
        {
          headers: { Authorization: `Bearer ${auth.accessToken}` },
        },
      )
      if (res.data.success) {
        isFav.value = true
        notify({ text: '已加入收藏', type: 'success', group: 'bottom-center' })
      }
    }
  } catch (err) {
    notify({
      text: err.response?.data?.message || '操作失敗',
      type: 'error',
      group: 'bottom-center',
    })
  }
}

// 商品清單
const fetchBrandProducts = async (page = 1) => {
  try {
    const brandId = Number(route.params.brandId)
    if (!brandId) return
    const filter = { BrandId: brandId, PageIndex: page, PageSize: pageSize }
    const resp = await axios.post('/api/prod/Products/search', filter)
    if (resp.data && resp.data.data) {
      products.value = resp.data.data.items || []
      totalCount.value = resp.data.data.totalCount || 0
      currentPage.value = page
    } else {
      products.value = []
      totalCount.value = 0
    }
  } catch (err) {
    console.error('[BrandDetail] fetchBrandProducts error =', err)
  }
}

const moreAnchor = ref(null)
const infoSection = ref(null)
const moreOpen = ref(false)

// 開啟了解更多：展開後自動滾到 BrandInfo 區域上緣
const openMore = async () => {
  moreOpen.value = true
  await nextTick()
  // 讓畫面滑動至 infoSection 或按鈕上緣
  const target = infoSection.value?.$el || moreAnchor.value
  if (target) {
    const top = target.getBoundingClientRect().top + window.scrollY - 80 // 🔹可微調
    window.scrollTo({ top, behavior: 'smooth' })
  }
}
// 關閉並滑回「了解更多」按鈕位置
const closeMoreAndScrollToTop = async () => {
  moreOpen.value = false
  await nextTick()
  if (moreAnchor.value) {
    moreAnchor.value.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }
}

// 分頁切換
const onPageChange = (page) => fetchBrandProducts(page)

// 初始化
onMounted(() => {
  fetchDetail()
  fetchBrandProducts(1)
})

// 動態折扣文字（0.95 → 95折，0.9 → 9折）
const displayRate = computed(() => {
  const rate = discountInfo.value?.discountRate
  if (!rate || rate >= 1) return ''
  const val = rate * 10
  // 若為整數（0.9），顯示「9折」；否則顯示「95折」
  return Number.isInteger(val) ? `${val}折` : `${Math.round(val * 10)}折`
})

// 動態樣式
const barStyle = computed(() => {
  const { r, g, b } = vm.value.mainColor
  const luma = getLuma({ r, g, b })
  const textColor = luma > 150 ? '#222' : `rgb(${r}, ${g}, ${b})`
  return {
    backgroundColor: `rgba(${r}, ${g}, ${b}, 0.1)`,
    color: textColor,
    border: `1px solid rgba(${r}, ${g}, ${b}, 0.3)`,
    borderRadius: '4px',
    marginBottom: '12px',
    fontSize: '0.95rem',
  }
})

async function fetchBrandDiscount(brandId) {
  try {
    const res = await axios.get(`/api/sup/Brands/discount/bybrand/${brandId}`)
    const data = res?.data?.data
    if (data && data.discountRate) {
      discountInfo.value = data
    } else {
      discountInfo.value = null
    }
  } catch (err) {
    console.error('[BrandDetail] fetchBrandDiscount error =', err)
    discountInfo.value = null
  }
}

watch(
  () => route.fullPath,
  () => {
    moreOpen.value = false // ✅ 切換品牌自動收合展開區
    fetchDetail()
    fetchBrandProducts(1)
  },
)

const onFilter = (btn) => {
  console.log('[BrandDetail] click filter btn =', btn)

  router.push({
    name: 'product-type-search',
    params: { slug: btn.slug },
    query: { title: btn.text },
  })
}
</script>

<style scoped>
.anchor-to-top {
  scroll-margin-top: 80px;
}
.content-wrap {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 1rem;
}
@media (min-width: 1400px) {
  .content-wrap {
    padding: 0 2rem;
  }
}
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
.bg-soft {
  background-color: #f8f9fa;
}
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

.btn-toggle {
  background-color: rgb(0, 112, 131);
  color: rgb(248, 249, 250);
  transition: background-color 0.2s ease;
}
.btn-toggle:hover {
  background-color: rgb(77, 180, 193);
  color: rgb(248, 249, 250);
}

.discount-bar {
  background-color: #f8efe2;
  color: #d9480f;
  border: 1px solid #ffe8cc;
  border-radius: 4px;
  /* margin-bottom: 12px; */
  font-size: 0.95rem;
}

.fav-btn {
  width: 25px;
  height: 25px;
  border-radius: 9999px;
  border: 2px solid #ffd6f3;
  background: #fff;
  color: #ef4444;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition:
    background 0.15s ease,
    border-color 0.15s ease,
    color 0.15s ease;
}
.fav-btn:hover {
  background: #fff1f2;
  border-color: #fecaca;
}
.fav-btn[aria-pressed='true'] {
  background: #fee2e2;
  border-color: #fca5a5;
  color: #dc2626;
}
.fav-btn:active {
  transform: none;
}
</style>
