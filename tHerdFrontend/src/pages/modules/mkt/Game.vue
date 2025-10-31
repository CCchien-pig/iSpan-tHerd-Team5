<template>
  <div class="game-page">
    <!-- 🧩 確保 MemoryGame 只渲染一次 -->
    <MemoryGame v-if="mountedOnce" />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import MemoryGame from '@/components/modules/mkt/Game/MemoryGame.vue'

const mountedOnce = ref(false)

onMounted(() => {
  if (!mountedOnce.value) {
    mountedOnce.value = true
    document.title = '翻牌記憶遊戲 | tHerd'
  }
})
</script>

<style scoped>
.game-page {
  min-height: 100vh;
  display: flex;
  justify-content: center;
  align-items: flex-start;
  background: #f9fafb;
  padding-top: 40px;
}
</style>
