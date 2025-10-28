<script setup lang="ts">
import { RouterView } from 'vue-router'
import { onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
const auth = useAuthStore()

onMounted(async () => {
  // 初始化：讀 localStorage 並（有 token 時）嘗試拉 /auth/me
  await auth.init()
})
</script>

<template>
  <RouterView />
</template>

<style></style>
