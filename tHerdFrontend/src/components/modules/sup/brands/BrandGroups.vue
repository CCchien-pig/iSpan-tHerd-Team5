<template>
  <section class="groups">
    <!-- <div>
      <section class="groups"></section>
      <button @click="toast('測試通知', 'success')">測試通知</button>
    </div> -->
    <div v-if="loading" class="text-muted small">載入中…</div>

    <div v-else>
      <!-- 品牌收藏篩選單選框 -->
      <div class="flex space-x-6 mb-4">
        <label
          v-for="o in options"
          :key="o.value"
          class="cursor-pointer flex items-center space-x-2"
        >
          <input
            type="radio"
            :value="o.value"
            v-model="brandFilter"
            class="w-5 h-5 text-[rgb(77,180,193)] bg-gray-100 border-gray-300 focus:ring-[rgb(0,147,171)]"
          />
          <span class="select-none main-color-green-text font-semibold">{{ o.label }}</span>
        </label>
      </div>

      <div
        v-if="brandFilter === 'favorite' && filteredGroups.length === 0"
        class="no-favorites-msg"
      >
        目前沒有收藏中的品牌~
      </div>
      <!-- 列表改為使用 filteredGroups -->
      <div v-for="g in filteredGroups" :key="g.letter" class="group-section">
        <div class="group-title" :id="g.letter" ref="anchorRefs">
          <strong class="h5">{{ g.letter }}</strong>
        </div>

        <ul class="brand-text-list">
          <li v-for="b in g.brands" :key="b.brandId" class="brand-row">
            <button
              class="fav-btn"
              :aria-pressed="isFav(b.brandId)"
              @click.stop="toggleFav(b)"
              :title="isFav(b.brandId) ? '已收藏' : '加入收藏'"
            >
              <svg
                v-if="isFav(b.brandId)"
                viewBox="0 0 24 24"
                width="16"
                height="16"
                aria-hidden="true"
              >
                <path
                  fill="currentColor"
                  d="M12.1 21.55 12 21.65l-.1-.1C7.14 17.24 4 14.39 4 11.5 4 9.5 5.5 8 7.5 8c1.54 0 3.04.99 3.57 2.36h1.87C13.46 8.99 14.96 8 16.5 8 18.5 8 20 9.5 20 11.5c0 2.89-3.14 5.74-7.9 10.05Z"
                />
              </svg>
              <svg v-else viewBox="0 0 24 24" width="16" height="16" aria-hidden="true">
                <path
                  fill="currentColor"
                  d="M16.5 3c-1.74 0-3.41.81-4.5 2.09C10.91 3.81 9.24 3 7.5 3 4.42 3 2 5.42 2 8.5c0 3.78 3.4 6.86 8.55 11.54L12 21.35l1.45-1.32C18.6 15.36 22 12.28 22 8.5 22 5.42 19.58 3 16.5 3Zm-4.4 15.55-.1.1-.1-.1C7.14 14.24 4 11.39 4 8.5 4 6.5 5.5 5 7.5 5c1.54 0 3.04.99 3.57 2.36h1.87C13.46 5.99 14.96 5 16.5 5 18.5 5 20 6.5 20 8.5c0 2.89-3.14 5.74-7.9 10.05Z"
                />
              </svg>
            </button>
            <router-link :to="toBrandSlug(b.brandName, b.brandId)" class="brand-text-link">
              {{ b.brandName }}
            </router-link>
          </li>
        </ul>
      </div>
    </div>
  </section>
</template>

<script setup>
import { ref, onMounted, nextTick, watch, computed } from 'vue'
import { toBrandSlug } from '@/utils/supSlugify'
import axios from 'axios'
import { useAuthStore } from '@/stores/auth'
// [新增] 2. 直接匯入 notify
import { notify } from 'notiwind'

const props = defineProps({
  groups: { type: Array, default: () => [] },
  loading: { type: Boolean, default: false },
})

const emit = defineEmits(['mounted-anchors'])

const auth = useAuthStore()
const brandFilter = ref('all') // 預設選擇「所有品牌」
const favorites = ref(new Set())
// 讓 0-9 放在 Z 後面
const lettersOrder = [...Array.from({ length: 26 }, (_, i) => String.fromCharCode(65 + i)), '0-9']

const filteredGroups = computed(() => {
  const allGroups = JSON.parse(JSON.stringify(props.groups))
  allGroups.forEach((group) => {
    group.brands = group.brands.filter((b) => {
      if (brandFilter.value === 'favorite') {
        return favorites.value.has(b.brandId)
      }
      return true
    })
  })
  return allGroups.filter((g) => g.brands.length > 0)
})
// 單選框選項
const options = [
  { value: 'all', label: '所有品牌' },
  { value: 'favorite', label: '已收藏品牌' },
]

const isFav = (id) => favorites.value.has(id)

async function loadFavorites() {
  if (!auth.isAuthenticated) return
  try {
    const res = await axios.get('/api/sup/BrandFavorites/my', {
      headers: { Authorization: `Bearer ${auth.accessToken}` },
    })
    if (res.data.success && Array.isArray(res.data.data)) {
      favorites.value = new Set(res.data.data.map((item) => item.brandId))
    }
  } catch (err) {
    console.error('載入收藏失敗', err)
  }
}

/**
 * 顯示 Toast 通知
 * @param {string} msg - 訊息內容
 * @param {('info'|'success'|'error')} [type='info'] - 通知類型
 */
function toast(msg, type = 'info') {
  console.log('toast:', msg, 'type:', type) // 確認是否有執行
  notify({
    text: msg,
    type,
    duration: 2000, // 顯示的毫秒數
    group: 'bottom-center', // <-- 必須使用 'group' 並對應 App.vue
  })
}

async function toggleFav(b) {
  console.log('toggleFav triggered for brand:', b)
  if (!auth.isAuthenticated) {
    toast('請先登入會員', 'error')
    console.log('User not authenticated')
    return
  }
  try {
    if (isFav(b.brandId)) {
      const res = await axios.delete(`/api/sup/BrandFavorites/${b.brandId}`, {
        headers: { Authorization: `Bearer ${auth.accessToken}` },
      })
      if (res.data.success) {
        favorites.value.delete(b.brandId)
        toast('已移除收藏', 'success')
      } else {
        toast(res.data.message || '取消收藏失敗', 'error')
      }
    } else {
      const res = await axios.post(
        '/api/sup/BrandFavorites',
        { brandId: b.brandId },
        {
          headers: { Authorization: `Bearer ${auth.accessToken}` },
        },
      )
      if (res.data.success) {
        favorites.value.add(b.brandId)
        toast('已加入收藏', 'success')
      } else {
        toast(res.data.message || '加入收藏失敗', 'error')
      }
    }
  } catch (err) {
    toast(err.response?.data?.message || '操作失敗', 'error')
  }
}

const anchorRefs = ref([])

const buildMapAndEmit = async () => {
  await nextTick()
  const map = new Map()
  for (const L of lettersOrder) {
    const el = document.getElementById(L)
    if (el) map.set(L, el)
  }
  emit('mounted-anchors', map)
}

onMounted(() => {
  buildMapAndEmit()
  loadFavorites()
})

watch(() => props.groups, buildMapAndEmit)
</script>

<style scoped>
.group-section {
  margin-bottom: 24px;
}
.group-title {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 6px 0;
  border-bottom: 1px solid #e6eaed;
  position: sticky;
  top: 80px;
  background: #fff;
  z-index: 1;
}
.brand-text-list {
  list-style: none;
  margin: 12px 0 0;
  padding: 0;
  columns: 2;
  column-gap: 16px;
}
.brand-text-link {
  display: inline-block;
  padding: 6px 4px;
  color: #007083;
  text-decoration: none;
  font-size: 18px;
  font-weight: 700;
}
.brand-text-link:hover {
  color: #4db4c1;
}
.brand-text-list li {
  break-inside: avoid;
  -webkit-column-break-inside: avoid;
}
.brand-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 0;
}
.fav-btn {
  width: 28px;
  height: 28px;
  border-radius: 9999px;
  border: 1px solid #e5e7eb;
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
@media (min-width: 576px) {
  .brand-text-list {
    columns: 3;
  }
}
@media (min-width: 768px) {
  .brand-text-list {
    columns: 4;
  }
}
@media (min-width: 992px) {
  .brand-text-list {
    columns: 5;
  }
}
.no-favorites-msg {
  text-align: center;
  padding: 2rem 0;
  font-size: 1.2rem;
  color: #666;
}
</style>
