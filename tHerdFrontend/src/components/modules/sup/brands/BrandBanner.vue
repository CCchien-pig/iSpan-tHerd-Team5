<!-- 接收 bannerUrl、brandName，純顯示 -->

<template>
  <img :src="url" :alt="alt" class="img-fluid w-100 rounded" />
</template>

<script setup>
defineProps({ url: String, alt: String })
</script>
