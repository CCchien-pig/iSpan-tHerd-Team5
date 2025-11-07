<!-- 接收 bannerUrl、brandName，純顯示 -->

<template>
  <a v-if="linkUrl" :href="linkUrl" target="_blank" rel="noopener">
    <img :src="url" :alt="alt" class="img-fluid w-100 rounded"
  /></a>
</template>

<script setup>
defineProps({ url: String, alt: String })
</script>
