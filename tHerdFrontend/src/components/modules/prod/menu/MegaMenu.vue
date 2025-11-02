<template>
  <div class="mega-menu-wrapper">
    <div class="mega-menu" @mouseenter="keepOpen" @mouseleave="closeMenu">
      <div v-if="loading" class="text-center p-4">載入中...</div>
      <div v-else-if="error" class="text-danger p-4">{{ error }}</div>
      <div v-else class="menu-columns">
        <div v-for="col in columns" :key="col.title" class="menu-column">
          <h4>{{ col.title }}</h4>
          <ul>
            <li v-for="item in col.items" :key="item.id">
              <a :href="item.url">{{ item.name }}</a>
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

// ✅ 初次載入就取資料（這個 API 不需要 categoryId）
onMounted(fetchData)

async function fetchData() {
  try {
    loading.value = true
    error.value = null
    columns.value = []
    brands.value = []

    // ✅ 呼叫後端 /api/prod/Products/ProductTypetree
    const res = await ProductsApi.getProductCategories()
    const result = res.data

    if (!result.success) {
      throw new Error(result.message || '查詢失敗')
    }

    const treeData = result.data || []

    // ✅ 將樹狀結構轉成 MegaMenu 欄位資料格式
    columns.value = treeData.map((parent) => ({
      title: parent.productTypeName,
      items: (parent.children || []).map((child) => ({
        id: child.productTypeId,
        name: child.productTypeName,
        url: `/products?type=${child.productTypeId}`
      }))
    }))
  } catch (err) {
    console.error('❌ [MegaMenu] 載入分類發生錯誤：', err)
    error.value = '載入失敗，請稍後再試'
  } finally {
    loading.value = false
  }
}

function keepOpen() {}
function closeMenu() {
  // 若希望滑開後保留資料可註解掉
  // columns.value = []
}
</script>

<style scoped>
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
