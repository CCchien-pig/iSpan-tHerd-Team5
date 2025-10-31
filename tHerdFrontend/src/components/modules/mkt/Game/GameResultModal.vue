<template>
  <div v-if="visible" id="game-result-overlay">
    <div id="game-result-modal">
      <h3 v-if="isClear">全部配對成功！</h3>
      <h3 v-else>時間到！</h3>

      <p>你的最終分數：<strong>{{ score }}</strong> 分</p>
      <p>可兌換優惠券：<strong>{{ score * 10 }}</strong> 元</p>

      <button @click="confirmSubmit">確認</button>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'

const props = defineProps({
  score: Number,
  isClear: Boolean
})
const emit = defineEmits(['submit'])

const visible = ref(true)

function confirmSubmit() {
  visible.value = false
  emit('submit')
}

onMounted(() => {
  // ✅ 保險：強制把彈窗掛在 body 底層
  document.body.appendChild(document.getElementById('game-result-overlay'))

  // ✅ 動態注入樣式，避免被 bootstrap/reset.css 改掉
  const style = document.createElement('style')
  style.innerHTML = `
    #game-result-overlay {
      position: fixed;
      inset: 0;
      background: rgba(0,0,0,0.6);
      display: flex;
      justify-content: center;
      align-items: center;
      z-index: 999999999 !important;
      opacity: 1 !important;
      visibility: visible !important;
    }
    #game-result-modal {
      background: white;
      padding: 25px;
      border-radius: 12px;
      width: 320px;
      text-align: center;
      box-shadow: 0 0 20px rgba(0,0,0,0.5);
      transform: scale(1);
      animation: popIn 0.3s ease-in-out;
      color: black;
      opacity: 1;
      visibility: visible;
      z-index: 999999999 !important;
    }
    #game-result-modal button {
      margin-top: 15px;
      background: #007083;
      color: #fff;
      border: none;
      border-radius: 8px;
      padding: 8px 16px;
      cursor: pointer;
      transition: background 0.3s;
    }
    #game-result-modal button:hover {
      background: #005c69;
    }
    @keyframes popIn {
      0% { transform: scale(0.8); opacity: 0; }
      100% { transform: scale(1); opacity: 1; }
    }
  `
  document.head.appendChild(style)
})

onUnmounted(() => {
  const el = document.getElementById('game-result-overlay')
  if (el) el.remove()
})
</script>
