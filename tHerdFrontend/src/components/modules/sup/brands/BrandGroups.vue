<!-- src/components/modules/sup/brands/BrandGroups.vue -->
<!-- （A–Z 分組清單） -->

<template>
  <section class="groups">
    <div v-if="loading" class="text-muted small">載入中…</div>
    <div v-else>
      <div v-for="g in groups" :key="g.letter" class="group-section">
        <div class="group-title" :id="g.letter" ref="anchorRefs">
          <div class="title-left">
            <strong class="h5">{{ g.letter }}</strong>
          </div>
          <!-- <div class="title-right">
            <a href="#top" class="small">返回頂部</a>
          </div> -->
        </div>

        <!-- 純文字品牌列表 -->
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
            <router-link :to="brandTo(b)" class="brand-text-link">{{ b.brandName }}</router-link>
          </li>
        </ul>
      </div>
    </div>
  </section>
</template>

<script setup>
import { ref, onMounted, nextTick, watch } from 'vue'

const props = defineProps({
  groups: { type: Array, default: () => [] },
  loading: { type: Boolean, default: false },
})

const emit = defineEmits(['mounted-anchors'])

const brandTo = () => '/brands'

/* 收藏邏輯（維持） */
const favorites = ref(new Set())
const isFav = (id) => favorites.value.has(id)
const toggleFav = (b) => {
  const s = new Set(favorites.value)
  s.has(b.brandId) ? s.delete(b.brandId) : s.add(b.brandId)
  favorites.value = s
  toast(s.has(b.brandId) ? '已加入收藏' : '已移除收藏')
}
const toast = (msg) => {
  const el = document.createElement('div')
  el.className = 'toast-mini'
  el.textContent = msg
  document.body.appendChild(el)
  setTimeout(() => el.remove(), 1200)
}

/* A–Z 固定順序，確保 Map 順序一致 */
const lettersOrder = ['0-9', ...Array.from({ length: 26 }, (_, i) => String.fromCharCode(65 + i))]

/* 由 template v-for 綁定的參考 */
const anchorRefs = ref([])

/* 建立錨點 Map（按固定順序插入）並 emit 給父層 */
const buildMapAndEmit = async () => {
  await nextTick()
  const map = new Map()
  // 以 id 直接查找元素較穩定，避免 anchorRefs 順序受 DOM 影響
  for (const L of lettersOrder) {
    const el = document.getElementById(L)
    if (el) map.set(L, el)
  }
  emit('mounted-anchors', map)
}

onMounted(buildMapAndEmit)
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
  top: 80px; /* 和 onJumpTo 的 80 對齊 */
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
  /* text-decoration: underline; */
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
} /* 不縮小 */

.toast-mini {
  position: fixed;
  left: 50%;
  bottom: 80px;
  transform: translateX(-50%);
  background: rgba(17, 24, 39, 0.92);
  color: #fff;
  padding: 8px 12px;
  border-radius: 8px;
  font-size: 12px;
  z-index: 2000;
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
</style>
