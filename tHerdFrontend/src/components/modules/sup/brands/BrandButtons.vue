<!-- 接收 buttons、主色設定（bgRgb、hoverTextColor），內含 hover 規則與空清單處理。 -->
<!-- src/components/modules/sup/brands/BrandButtons.vue -->

<template>
  <div class="brand-buttons-wrap py-3 rounded" :style="{ backgroundColor: bgCss }">
    <nav class="d-flex flex-wrap gap-2 justify-content-center">
      <button
        v-for="btn in buttons"
        :key="btn.id"
        type="button"
        class="brand-btn btn-sm fw-semibold"
        :style="buttonStyle"
        @click="$emit('tap', btn)"
      >
        {{ btn.text }}
      </button>
    </nav>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  buttons: { type: Array, default: () => [] },
  bgRgb: { type: Object, default: () => ({ r: 0, g: 147, b: 171 }) },
})

// === 顏色運算 ===
const bgCss = computed(() => `rgb(${props.bgRgb.r}, ${props.bgRgb.g}, ${props.bgRgb.b})`)
const luma = computed(() => 0.2126 * props.bgRgb.r + 0.7152 * props.bgRgb.g + 0.0722 * props.bgRgb.b)

// 文字顏色（依亮度自動調整）
const textColor = computed(() => (luma.value > 180 ? '#0b3a3f' : '#ffffff'))

// hover 狀態字體：亮色主色 → 黑；暗色主色 → 主色本身
const hoverText = computed(() =>
  luma.value > 200 ? '#111827' : `rgb(${props.bgRgb.r}, ${props.bgRgb.g}, ${props.bgRgb.b})`
)

const buttonStyle = computed(() => ({
  '--hover-text': hoverText.value,
  '--btn-text': textColor.value,
  '--btn-border': 'rgba(255, 255, 255, 0.25)',
}))
</script>

<style scoped>
.brand-buttons-wrap {
  text-align: center;
}

.brand-btn {
  background: transparent;
  border: 1px solid var(--btn-border, rgba(255, 255, 255, 0.25));
  color: var(--btn-text, #fff);
  border-radius: 999px;
  padding: 6px 14px;
  transition:
    background-color 0.2s ease,
    color 0.2s ease,
    border-color 0.2s ease;
}

/* ✅ Hover 狀態 */
.brand-btn:hover {
  background: #f8f9fa; /* 固定亮灰底 */
  color: var(--hover-text, #0793ab); /* 若主色太亮 → 黑色 */
  border-color: rgba(0, 0, 0, 0.08);
}
</style>

