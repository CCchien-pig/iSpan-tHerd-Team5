<!-- src/components/modules/sup/brands/AlphaIndex.vue -->
<!-- （上方 A–Z/0–9 索引） -->

<template>
  <nav class="alpha-index py-2">
    <ul class="alpha-list">
      <li v-for="c in chars" :key="c">
        <a href="javascript:void(0)" @click="$emit('jump', c)">{{ c }}</a>
      </li>
    </ul>
  </nav>
</template>

<script setup>
defineProps({
  chars: { type: Array, required: true },
})
defineEmits(['jump'])
</script>

<style scoped>
.alpha-index {
  width: 100%;
  max-width: 1200px; /* ✅ 與跑馬燈寬度一致 */
  margin: 0 auto 16px; /* 下方留距 */
  background: #f8fafc;
  border-radius: 10px;
  text-align: center;
}

.alpha-list {
  display: flex;
  justify-content: center; /* ✅ 置中 */
  flex-wrap: wrap;
  gap: 8px 12px;
  list-style: none;
  margin: 0;
  padding: 8px 12px;
}

.alpha-list li a {
  display: inline-block;
  padding: 6px 10px;
  border-radius: 999px;
  color: #0a7a3e;
  text-decoration: none;
  background: #fff;
  border: 1px solid #dfe5e8;
  font-weight: 600;
  transition: all 0.2s ease;
}
.alpha-list li a:hover {
  background: #73c8d3;
  color: #fff;
  border-color: #73c8d3;
}
</style>
