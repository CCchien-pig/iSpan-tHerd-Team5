<template>
  <div class="card bg-soft">
    <!-- 標題 -->
    <h4 class="mb-3">{{ content.contentTitle }}</h4>

    <!-- ✅ 內文渲染 -->
    <div class="rich-content" v-html="content.content"></div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  content: { type: Object, default: () => ({}) },
  accentRgb: { type: Object, default: () => ({ r: 0, g: 147, b: 171 }) },
})

// 動態主色字串（for CSS v-bind）
const accentColor = computed(
  () => `rgb(${props.accentRgb.r}, ${props.accentRgb.g}, ${props.accentRgb.b})`,
)
</script>

<style scoped>
.bg-soft {
  background-color: #f8f9fa;
  padding: 25px 10px 20px 25px;
}

/* ✅ 通用雙欄容器樣式 */
.rich-content :deep(.two-column-container) {
  display: flex;
  flex-wrap: wrap;
  gap: 1.5rem;
}
.rich-content :deep(.two-column-container .column) {
  flex: 1 1 48%;
}
@media (max-width: 992px) {
  .rich-content :deep(.two-column-container .column) {
    flex: 1 1 100%;
  }
}

/* ✅ 圓點樣式 for <li> (文章段落內) */
.rich-content :deep(ul) {
  list-style: none;
  padding-left: 0;
  margin: 0;
}
.rich-content :deep(li) {
  position: relative;
  padding-left: 1.25rem;
  line-height: 1.6;
  margin-bottom: 0.25rem;
}
.rich-content :deep(li)::before {
  content: '';
  position: absolute;
  left: 0;
  top: 0.6em;
  width: 0.5em;
  height: 0.5em;
  border-radius: 50%;
  background-color: v-bind(accentColor);
}

/* ✅ 圖片樣式 */
.rich-content :deep(img) {
  max-width: 100%;
  height: auto;
  border-radius: 0.25rem;
}
</style>
