<!-- \tHerdFrontend\src\pages\modules\sup\BrandsAZ.vue -->
<!-- （頁面進入點） -->

<template>
  <section class="brands-az">
    <div class="container py-3">
      <div class="brands-layout">
        <div class="brands-main">
          <AlphaIndex class="mb-4" :chars="chars" @jump="onJumpTo" />
          <FeaturedCarousel class="mt-3" :brands="featuredBrands" :loading="loadingFeatured" />
          <BrandGroups class="mt-4" :groups="brandGroups" :loading="loadingGroups" @mounted-anchors="onAnchorsReady" />
        </div>

        <div class="brands-aside">
          <RightAlphaNav v-if="showRightNav" :chars="chars" :active="activeLetter" @jump="onJumpTo" />
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

const chars = ref(['0-9', ...Array.from({ length: 26 }, (_, i) => String.fromCharCode(65 + i))])

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
  result.push({
    letter: '0-9',
    brands: (map.get('0-9')?.brands ?? []).sort((a, b) => a.brandName.localeCompare(b.brandName)),
  })
  for (let i = 0; i < 26; i++) {
    const L = String.fromCharCode(65 + i)
    const item = map.get(L)
    result.push({
      letter: L,
      brands: (item?.brands ?? []).sort((a, b) => a.brandName.localeCompare(b.brandName)),
    })
  }
  return result
}

const onJumpTo = (letter) => {
  const el = anchorMap.get(letter)
  if (el) {
    window.scrollTo({
      top: el.getBoundingClientRect().top + window.scrollY - 80,
      behavior: 'smooth',
    })
  }
}
const onAnchorsReady = (map) => {
  anchorMap = map
}

const onScroll = () => {
  showRightNav.value = window.scrollY > 300
  let current = null
  const viewportTop = window.scrollY + 100
  for (const [letter, el] of anchorMap.entries()) {
    if (el.offsetTop <= viewportTop) current = letter
    else break
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
</style>
