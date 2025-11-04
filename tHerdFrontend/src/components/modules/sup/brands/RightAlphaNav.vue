<!-- src/components/modules/sup/brands/RightAlphaNav.vue -->
<!-- （右側浮動索引） -->

<template>
  <aside class="right-alpha-nav">
    <ul class="alpha-list" ref="listRef">
      <li v-for="c in displayChars" :key="c" :class="{ active: isActive(c) }">
        <button
          class="alpha-btn"
          :ref="(el) => (itemRefs[c] = el)"
          type="button"
          @click="onJump(c)"
        >
          {{ c }}
        </button>
      </li>
    </ul>
  </aside>
</template>

<script setup>
import { computed, watch, ref, onMounted } from 'vue'

const props = defineProps({
  chars: { type: Array, required: true }, // 例如 ['0-9','A',...'Z']
  active: { type: String, default: null }, // 例如 'A' 或 '0-9'
})
const emit = defineEmits(['jump'])

const listRef = ref(null)
const itemRefs = ref({})

const displayChars = computed(() => {
  const arr = [...(props.chars || [])]
  if (arr[0] === '0-9') arr[0] = '9' // 顯示上用 9 取代 0-9
  return arr
})

const isActive = (c) => props.active === (c === '9' ? '0-9' : c)
const onJump = (c) => emit('jump', c === '9' ? '0-9' : c)

const scrollActiveIntoView = () => {
  const key = props.active === '0-9' ? '9' : props.active
  const el = itemRefs.value[key]
  el?.scrollIntoView?.({ block: 'nearest', inline: 'nearest', behavior: 'smooth' })
}

watch(
  () => props.active,
  () => scrollActiveIntoView(),
)
onMounted(() => scrollActiveIntoView())
</script>

<style scoped>
.right-alpha-nav {
  position: sticky;
  top: 120px; /* 依你的 Header 高度調整 */
  align-self: flex-start;
  background: rgba(255, 255, 255, 0.98);
  border: 1px solid #e5e7eb;
  border-radius: 14px;
  padding: 8px 6px;
  z-index: 50;
}

.alpha-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 6px;
  max-height: calc(100vh - 160px);
  overflow: auto;
  scrollbar-width: none; /* Firefox 隱藏 */
}
.alpha-list::-webkit-scrollbar {
  display: none;
} /* WebKit 隱藏 */

.alpha-btn {
  width: 40px;
  height: 28px;
  border: none;
  border-radius: 10px;
  background: transparent;
  color: #0f766e;
  font-weight: 700;
  cursor: pointer;
}
li.active .alpha-btn,
.alpha-btn:hover {
  background: #16794c;
  color: #fff;
}
</style>
