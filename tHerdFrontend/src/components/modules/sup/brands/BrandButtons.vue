<!-- 接收 buttons、主色設定（bgRgb、hoverTextColor），內含 hover 規則與空清單處理。 -->

<template>
  <div class="brand-buttons-wrap py-3 rounded" :style="{ backgroundColor: bgCss }">
    <nav class="d-flex flex-wrap gap-2 justify-content-center">
      <button
        v-for="btn in buttons"
        :key="btn.id"
        type="button"
        class="brand-btn btn-sm fw-semibold"
        :style="{ '--hover-text': hoverText, '--btn-text': textColor }"
        @click="$emit('tap', btn)"
      >
        {{ btn.text }}
      </button>
    </nav>
  </div>
</template>

<script setup>
const props = defineProps({
  buttons: { type: Array, default: () => [] },
  bgRgb: { type: Object, default: () => ({ r: 0, g: 147, b: 171 }) },
})
const bgCss = `rgb(${props.bgRgb.r}, ${props.bgRgb.g}, ${props.bgRgb.b})`
const luma = 0.2126 * props.bgRgb.r + 0.7152 * props.bgRgb.g + 0.0722 * props.bgRgb.b
const textColor = luma > 180 ? '#0b3a3f' : '#ffffff'
const hoverText = `rgb(${props.bgRgb.r}, ${props.bgRgb.g}, ${props.bgRgb.b})`
</script>

<style scoped>
.brand-buttons-wrap {
  text-align: center;
}
.brand-btn {
  background: transparent;
  border: 1px solid rgba(255, 255, 255, 0.2);
  color: var(--btn-text, #fff);
  transition:
    background-color 0.15s ease,
    color 0.15s ease,
    border-color 0.15s ease;
}
.brand-btn:hover {
  background: #fff;
  color: var(--hover-text, #0793ab);
  border-color: rgba(0, 0, 0, 0.08);
}
</style>
