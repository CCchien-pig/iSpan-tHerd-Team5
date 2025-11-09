<template>
  <div class="card bg-soft">
    <!-- 標題 -->
    <h4 class="mb-3">{{ content.contentTitle }}</h4>

    <!-- ✅ 內文渲染：支援左右雙欄 -->
    <div class="rich-content" v-html="content.content"></div>
  </div>
</template>

<script setup>
defineProps({
  content: { type: Object, default: () => ({}) },
})
</script>

<style scoped>
.bg-soft {
  background-color: #f8f9fa;
  padding: 25px 10px 20px 25px;
}

/* ✅ 通用雙欄容器樣式 */
/* 讓 v-html 內容吃到樣式（:deep 用於 scoped + v-html） */
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
.rich-content :deep(img) {
  max-width: 100%;
  height: auto;
  border-radius: 0.25rem;
}
</style>
