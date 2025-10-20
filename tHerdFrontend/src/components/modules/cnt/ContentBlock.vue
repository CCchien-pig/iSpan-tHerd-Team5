<!-- src/components/cnt/ContentBlock.vue -->
<script setup>
import { computed } from "vue";
import {
  absoluteImageUrl,
  safeHtml,        // 會修正 <img src="../../../uploads/...">
  onImgError
} from "@/utils/imageHelper";

// 🔸 若你的 Block 結構不同，這裡的鍵名請對應調整
const props = defineProps({
  block: { type: Object, required: true } // { blockType: 'richtext' | 'image' | 'video', content: '...' }
});

const t = (v) => String(v || "").toLowerCase();
const isRich  = computed(() => t(props.block?.blockType) === "richtext");
const isImage = computed(() => t(props.block?.blockType) === "image");
const isVideo = computed(() => t(props.block?.blockType) === "video");

// 1) 富文本：先修正 <img>，再把 <iframe>/<video> 包成 .video-responsive
function wrapEmbeds(html = "") {
  // 已先透過 safeHtml 修正 <img> 來源
  let out = safeHtml(html);

  // 不是已包裹的 iframe → 包 .video-responsive
  out = out.replace(/(<iframe\b[^>]*>[\s\S]*?<\/iframe>)/gi, (m) => {
    // 已經包過就不重複
    if (/class=["'][^"']*video-responsive/i.test(m)) return m;
    return `<div class="video-responsive">${m}</div>`;
  });

  // <video> 也包起來（保留 controls）
  out = out.replace(/(<video\b[^>]*>[\s\S]*?<\/video>)/gi, (m) => {
    if (/class=["'][^"']*video-responsive/i.test(m)) return m;
    return `<div class="video-responsive">${m}</div>`;
  });

  return out;
}
const html = computed(() => wrapEmbeds(props.block?.content || ""));

// 2) 單圖區塊：把相對路徑補成完整網址
const imgSrc = computed(() => absoluteImageUrl(props.block?.content || ""));

// 3) 影片區塊（若你有獨立 video block）
const videoSrc = computed(() => absoluteImageUrl(props.block?.content || ""));
</script>

<template>
  <!-- RichText -->
  <div v-if="isRich">
    <div class="prose max-w-none" v-html="html"></div>
  </div>

  <!-- Image -->
  <figure v-else-if="isImage" class="my-3">
    <img :src="imgSrc" class="img-fluid rounded shadow-sm" @error="onImgError" />
  </figure>

  <!-- Video block（可選） -->
  <div v-else-if="isVideo" class="video-responsive my-3">
    <video :src="videoSrc" controls playsinline></video>
  </div>

  <div v-else class="text-muted small">（未支援的內容區塊）</div>
</template>

<style scoped>
/* 富文本內的圖片也需要限制寬度 */
.prose :deep(img) {
  max-width: 100%;
  height: auto;
  border-radius: .5rem;
}

/* 16:9 響應式容器：適用 iframe / video */
.video-responsive {
  position: relative;
  width: 100%;
  padding-top: 56.25%; /* 16:9 */
  overflow: hidden;
  border-radius: .5rem;
  box-shadow: 0 4px 20px rgba(0,0,0,.06);
}
.video-responsive :deep(iframe),
.video-responsive :deep(video) {
  position: absolute; inset: 0;
  width: 100%; height: 100%;
  border: 0; object-fit: cover;
}
</style>
