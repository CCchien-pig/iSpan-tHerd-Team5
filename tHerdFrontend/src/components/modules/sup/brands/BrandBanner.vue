<!-- 接收 bannerUrl、brandName，純顯示 -->

<template>
  <div class="banner-wrapper">
    <img v-if="url" :src="url" :alt="alt" class="brand-banner" />
  </div>
</template>

<script setup>
defineProps({
  url: String,
  alt: String,
})
</script>

<style scoped>
.banner-wrapper {
  display: flex;
  justify-content: center;
  align-items: center;
  max-height: 350px; /* ✅ 固定顯示區域高度 */
  overflow: hidden; /* ✅ 超出區域的部分隱藏 */
  border-radius: 0.25rem;
  background-color: #fff; /* ✅ 背景填補空白 */
}

.brand-banner {
  width: 100%;
  height: auto;
  object-fit: cover; /* ✅ 以裁切方式填滿寬度區域 */
  object-position: center; /* ✅ 圖片置中顯示 */
}
</style>
