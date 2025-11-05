<!-- \tHerdFrontend\src\pages\modules\sup\BrandsAZ.vue -->
<!-- （品牌A-Z頁） -->

<template>
  <section class="brands-az">
    <AlphaIndex class="alpha-index-custom" :chars="chars" @jump="onJumpFromTop" />

    <div class="container py-3">
      <div class="brands-layout">
        <div class="brands-main">
          <div class="carousel-section">
            <FeaturedCarousel class="mt-3" :brands="featuredBrands" :loading="loadingFeatured" />
          </div>

          <BrandGroups
            class="mt-2"
            :groups="brandGroups"
            :loading="loadingGroups"
            @mounted-anchors="onAnchorsReady"
          />
        </div>

        <div class="brands-aside">
          <RightAlphaNav
            v-if="showRightNav"
            :chars="chars"
            :active="activeLetter"
            @jump="onJumpFromRight"
          />
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount } from 'vue'
import AlphaIndex from '@/components/modules/sup/brands/AlphaIndex.vue'
import FeaturedCarousel from '@/components/modules/sup/brands/FeaturedCarousel.vue'
import BrandGroups from '@/components/modules/sup/brands/BrandGroups.vue'
import RightAlphaNav from '@/components/modules/sup/brands/RightAlphaNav.vue'
// 用 supBrands.js 中匯出的兩個方法
import { getFeaturedBrands, getGroupedBrands } from '@/core/api/modules/sup/supBrands'

// 字母順序 先 A-Z，最後再 0-9
const chars = ref([...Array.from({ length: 26 }, (_, i) => String.fromCharCode(65 + i)), '0-9'])
const lettersOrder = [...Array.from({ length: 26 }, (_, i) => String.fromCharCode(65 + i)), '0-9']

// 儲存 API 回傳的資料與載入狀態
const featuredBrands = ref([])
const brandGroups = ref([])
const loadingFeatured = ref(false)
const loadingGroups = ref(false)

const showRightNav = ref(false)
const activeLetter = ref(null)
let anchorMap = new Map()

// 呼叫精選品牌 API
const fetchFeatured = async () => {
  loadingFeatured.value = true
  try {
    const res = await getFeaturedBrands({ isActive: true, isFeatured: true })
    // console.log('[AZ] raw featured res', res)
    // 應看到 res.status=200 與 res.data = Array
    // 兼容包裝一層 data 的情況
    const data = Array.isArray(res?.data)
      ? res.data
      : Array.isArray(res?.data?.data)
        ? res.data.data
        : []
    // console.log('[AZ] parsed featured data', data)
    featuredBrands.value = data
  } catch (e) {
    console.error('[AZ] fetchFeatured error', e)
    featuredBrands.value = []
  } finally {
    loadingFeatured.value = false
  }
}

// 呼叫分組品牌 API
const fetchGrouped = async () => {
  loadingGroups.value = true
  try {
    const res = await getGroupedBrands({ isActive: true })
    // console.log('[AZ] grouped raw', res?.data?.slice?.(0, 2) ?? res?.data)
    const data = Array.isArray(res.data)
      ? res.data
      : Array.isArray(res.data?.data)
        ? res.data.data
        : []
    brandGroups.value = normalizeGroups(data)
    // console.log('[AZ] grouped normalized A/0-9', brandGroups.value[0], brandGroups.value[1])
  } finally {
    loadingGroups.value = false
  }
}

const normalizeGroups = (groups) => {
  const map = new Map(groups.map((g) => [g.letter, g]))
  const result = []
  // 加 A-Z 的分組
  for (let i = 0; i < 26; i++) {
    const L = String.fromCharCode(65 + i)
    const item = map.get(L)
    result.push({
      letter: L,
      brands: (item?.brands ?? []).sort((a, b) => a.brandName.localeCompare(b.brandName)),
    })
  }
  // 加 0-9 分組
  result.push({
    letter: '0-9',
    brands: (map.get('0-9')?.brands ?? []).sort((a, b) => a.brandName.localeCompare(b.brandName)),
  })
  return result
}

const stickyOffset = 80 // 與 group-title sticky top 對齊

const resolveTargetEl = (letter) => {
  // 允許 fallback：回傳實際著陸的元素與字母
  let el = anchorMap.get(letter)
  if (el) return { el, actual: letter }
  let idx = lettersOrder.indexOf(letter)
  while (!el && idx < lettersOrder.length - 1) {
    idx += 1
    const nextLetter = lettersOrder[idx]
    el = anchorMap.get(nextLetter)
    if (el) return { el, actual: nextLetter }
  }
  return { el: null, actual: null }
}

const scrollToEl = (el) => {
  const y = el.getBoundingClientRect().top + window.scrollY - stickyOffset
  window.scrollTo({ top: y, behavior: 'smooth' })
}
const onJumpFromTop = (letter) => {
  const { el, actual } = resolveTargetEl(letter)
  if (!el) return
  activeLetter.value = actual // 右側立即亮起實際著陸字母
  scrollToEl(el)
}
const onJumpFromRight = (letter) => onJumpFromTop(letter)

// 由 BrandGroups 傳回 anchorMap（只記錄有 DOM 的字母，且順序正確）
const onAnchorsReady = (map) => {
  const ordered = new Map()
  for (const L of lettersOrder) {
    const el = map.get(L)
    if (el) ordered.set(L, el)
  }
  anchorMap = ordered
}

// 捲動時自動判斷目前位於哪個字母區
const onScroll = () => {
  showRightNav.value = window.scrollY > 300
  let current = null
  for (let i = lettersOrder.length - 1; i >= 0; i--) {
    const L = lettersOrder[i]
    const el = anchorMap.get(L)
    if (!el) continue
    // el 到視窗頂的距離（扣掉 sticky）
    const top = el.getBoundingClientRect().top - stickyOffset
    if (top <= 1) {
      current = L
      break
    }
  }
  activeLetter.value = current
}

// 頁面載入時觸發兩個 API
onMounted(async () => {
  await Promise.all([fetchFeatured(), fetchGrouped()])
  window.addEventListener('scroll', onScroll, { passive: true })
  //   console.log('[AZ] featuredBrands', featuredBrands.value) // 應該是陣列，每筆含 brandId, brandName, logoUrl
  //   console.log('[AZ] brandGroups', brandGroups.value) // 應該是 [{letter:'A', brands:[{brandName,...}]}]
})

onBeforeUnmount(() => {
  window.removeEventListener('scroll', onScroll)
})
</script>

<style scoped>
.brands-az {
  position: relative;
  min-height: 60vh;
}
.brands-layout {
  display: grid;
  grid-template-columns: 1fr 56px; /* 右側索引固定窄欄 */
  gap: 16px;
}
.brands-aside {
  /* 讓索引 sticky 生效 */
  position: relative;
}
.carousel-section {
  /* padding-top: 2rem; */
  /* padding-bottom: 1.2rem; */

  /* 確保容器本身不會裁切掉內容 */
  overflow: visible;
}
.brands-main {
  min-height: 800px;
}
</style>
