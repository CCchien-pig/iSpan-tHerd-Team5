<!-- BrandAccordionBlock.vue -->
<template>
  <div class="card bg-soft">
    <div class="card-body">
      <h4 class="mb-3 ms-2">{{ content?.contentTitle || '(未命名 Accordion)' }}</h4>
      <div v-if="content?.content" class="accordion-html" v-html="content.content"></div>
      <div v-else class="text-muted small">(無內容)</div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  content: { type: Object, default: () => ({}) },
  accentRgb: { type: Object, default: () => ({ r: 0, g: 147, b: 171 }) },
})
</script>

<style scoped>
.bg-soft {
  background-color: #f8f9fa;
}

/* ✅ 展開 Bootstrap 折疊內容 */
.accordion-html :deep(.accordion-collapse.collapse) {
  display: block !important;
  visibility: visible !important;
}

/* ✅ 圓點樣式（用主色） */
.accordion-html :deep(ul) {
  list-style: none;
  padding-left: 0;
  margin: 0;
}

.accordion-html :deep(li) {
  position: relative;
  padding-left: 1.25rem;
  line-height: 1.6;
  margin-bottom: 0.25rem;
}

.accordion-html :deep(li)::before {
  content: '';
  position: absolute;
  left: 0;
  top: 0.6em;
  width: 0.5em;
  height: 0.5em;
  border-radius: 50%;
  background-color: v-bind('`rgb(${accentRgb.r}, ${accentRgb.g}, ${accentRgb.b})`');
}
</style>
