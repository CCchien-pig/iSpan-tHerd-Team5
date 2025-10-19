<!-- src/components/cnt/ContentBlock.vue -->
<script setup>
import { computed } from "vue";
import { absoluteImageUrl, safeHtml, onImgError } from "@/utils/imageHelper.js";

const props = defineProps({
  block: { type: Object, required: true }  // { blockType: 'richtext' | 'image', content: '...' }
});

const isRich = computed(() => (props.block?.blockType || "").toLowerCase() === "richtext");
const isImage = computed(() => (props.block?.blockType || "").toLowerCase() === "image");

// 文字/富文本：先把 HTML 裡的 <img src="..."> 修正
const html = computed(() => safeHtml(props.block?.content || ""));

// 圖片：把相對路徑補正
const imgSrc = computed(() => absoluteImageUrl(props.block?.content || ""));
</script>

<template>
  <div v-if="isRich">
    <div class="prose max-w-none" v-html="html"></div>
  </div>

  <figure v-else-if="isImage" class="my-3">
    <img :src="imgSrc" class="img-fluid rounded shadow-sm" @error="onImgError" />
  </figure>

  <div v-else class="text-muted small">（未支援的內容區塊）</div>
</template>

<style scoped>
.prose :deep(img) {
  max-width: 100%;
  height: auto;
  border-radius: .5rem;
}
</style>
