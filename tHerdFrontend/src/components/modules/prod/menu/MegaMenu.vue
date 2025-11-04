<template>
  <div class="mega-menu-wrapper">
    <div class="mega-menu" @mouseenter="keepOpen" @mouseleave="closeMenu">
      <div v-if="loading" class="text-center p-4">載入中...</div>
      <div v-else-if="error" class="text-danger p-4">{{ error }}</div>
      <div v-else class="menu-columns">
        <div v-for="col in columns" :key="col.title" class="menu-column">
          <h4>
            <router-link 
              :to="`/products/${col.items?.[0]?.productTypeCode?.toLowerCase() || ''}`"
              class="brand-link fw-bold"
              @click="closeMenu"
            >
              {{ col.title }}
            </router-link>
          </h4>
          <ul>
            <li v-for="item in col.items" :key="item.id">
              <router-link
                :to="item.url"
                class="brand-link"
                @click="closeMenu"
              >
                {{ item.name }}
              </router-link>
            </li>
          </ul>
        </div>

        <div class="menu-column brands" v-if="brands.length">
          <h4>熱門品牌</h4>
          <div class="brand-list">
            <img
              v-for="b in brands"
              :key="b.id"
              :src="b.logoUrl"
              :alt="b.name"
            />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import ProductsApi from '@/api/modules/prod/ProductsApi'

const props = defineProps({
  categoryId: String,
  apiUrl: String,
})

const columns = ref([])
const brands = ref([])
const loading = ref(false)
const error = ref(null)
const loaded = ref(false) // ✅ 防止重複打 API


// ✅ 初次載入就取資料（這個 API 不需要 categoryId）
onMounted(fetchData)

async function fetchData() {
  if (loaded.value) return

  try {
    loading.value = true
    error.value = null
    columns.value = []
    brands.value = []

    const res = await ProductsApi.getProductCategories()
    const result = res.data

    if (!result.success) {
      throw new Error(result.message || '查詢失敗')
    }

    const treeData = result.data || []

    // ✅ 先遞迴建立 url（用 productTypeCode 小寫）
    function buildUrl(item, parentCode = '') {
      const path = parentCode
        ? `${parentCode}/${item.productTypeCode?.toLowerCase()}`
        : item.productTypeCode?.toLowerCase()

      item.url = `/products/${path}`
      if (item.children?.length) {
        item.children.forEach(c => buildUrl(c, path))
      }
    }
    treeData.forEach(i => buildUrl(i))

    // ✅ 把轉好的 url 帶入 columns 結構
    columns.value = treeData.map(parent => ({
      title: parent.productTypeName,
      items: (parent.children || []).map(child => ({
        id: child.productTypeId,
        name: child.productTypeName,
        url: child.url, // ✅ 已經是 /products/vitamins/b12
        productTypeCode: child.productTypeCode, // ✅ 給 h4 用
      })),
    }))

    loaded.value = true
  } catch (err) {
    console.error('❌ [MegaMenu] 載入分類發生錯誤：', err)
    error.value = '載入失敗，請稍後再試'
  } finally {
    loading.value = false
  }
}

// === 事件 ===
function keepOpen() {
  if (!loaded.value) fetchData() // ✅ 第一次 hover 才打 API
}

function closeMenu() {
  setTimeout(() => {
    columns.value = []
  }, 100)
}
</script>

<style scoped>
/* === MegaMenu 內部連結樣式（品牌 A-Z 同風格）=== */
.brand-link {
  display: inline-block;
  padding: 0.25rem 0.5rem;
  font-size: 0.95rem;
  color: #004d40; /* 深綠字體 */
  text-decoration: none;
  border-radius: 4px;
  transition: all 0.2s ease;
  cursor: pointer;
}

.brand-link:hover {
  color: rgb(0, 112, 131); /* hover 藍綠色 */
  background-color: rgba(0, 112, 131, 0.05); /* 淺底色 */
  text-decoration: underline; /* 底線效果 */
  padding-left: 0.75rem; /* 輕微滑動感 */
}

/* 標題風格（與品牌 A-Z 一致） */
.mega-menu h6 {
  color: rgb(0, 112, 131);
  font-weight: 700;
  font-size: 1rem;
  margin-bottom: 0.75rem;
}

.menu-link,
.menu-title-link {
  text-decoration: none;
  color: #333;
  transition: all 0.2s ease;
  display: inline-block;
  padding: 3px 0;
}

.menu-link:hover,
.menu-title-link:hover {
  color: rgb(77, 180, 193);
  transform: translateX(4px);
}

/* 這層要負責整個導航置中 */
.mega-menu-wrapper {
  position: absolute;
  top: 100%;
  left: 50%;
  transform: translateX(-50%);
  width: 80vw;
  max-width: 1200px;
  min-width: 700px;
  z-index: 9999;
}

/* MegaMenu 本體 */
.mega-menu {
  background: #fff;
  border-top: 3px solid rgb(77, 180, 193);
  border-radius: 12px;
  box-shadow: 0 8px 25px rgba(0, 0, 0, 0.15);
  padding: 2rem 2.5rem;
  animation: fadeDown 0.25s ease forwards;
}

@keyframes fadeDown {
  from {
    opacity: 0;
    transform: translateY(-10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* === 內容排列 === */
.menu-columns {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between; /* ✅ 平衡左右欄距離 */
  gap: 20px;
}

.menu-column {
  flex: 1 1 22%;
  min-width: 200px;
}

.menu-column h4 {
  font-weight: bold;
  margin-bottom: 10px;
  color: rgb(77, 180, 193);
}

/* === 品牌 Logo 區 === */
.brand-list {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 15px;
  margin-top: 10px;
}

.brand-list img {
  width: 80px;
  height: auto;
  object-fit: contain;
  filter: grayscale(20%);
  transition: all 0.3s ease;
}

.brand-list img:hover {
  filter: none;
  transform: scale(1.05);
}
</style>
