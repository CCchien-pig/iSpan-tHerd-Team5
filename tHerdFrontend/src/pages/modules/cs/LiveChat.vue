<template>
  <div class="center-narrow py-5">
    <h3 class="text-center mb-4 main-color-green-text">智能客服助理</h3>

    <!-- Chatbase 區塊 -->
    <div id="chatbase-container" class="border rounded-4 shadow-sm p-3 bg-white"></div>

    <!-- 轉人工中狀態 -->
    <div v-if="loading" class="text-center text-muted mt-4">
      <div class="spinner-border text-success me-2"></div>
      客服連線中，請稍候 3～5 分鐘...
    </div>

    <div v-if="connected" class="alert alert-success mt-4 text-center">
      ✅ 已由 {{ agentName }} 客服為您服務
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { http } from '@/api/http'


const loading = ref(false)
const connected = ref(false)
const agentName = ref('')


onMounted(() => {
  // 1️⃣ 載入 Chatbase Widget
  const script = document.createElement('script')
  script.src = 'https://www.chatbase.co/embed.min.js'
  script.setAttribute('data-chatbot-id', '你的-chatbase-id') // ← 換成你的 bot id
  document.body.appendChild(script)

  // 2️⃣ 監聽 Chatbase 訊息事件
  window.addEventListener('message', async (event) => {
    if (!event.origin.includes('chatbase.co')) return
    const data = event.data

    // 偵測關鍵字「轉人工」
    if (typeof data === 'string' && data.includes('轉人工')) {
      console.log('偵測到轉人工請求')
      loading.value = true

      try {
        // 呼叫候位 API
        const res = await http.post('/api/cs/chat/enqueue')
        console.log('加入客服候位', res.data)
      } catch (err) {
        console.error('enqueue error', err)
      }
    }

    // 🔔 若未來你加上 SignalR，可在這裡監聽「connected」事件切換狀態
    if (typeof data === 'object' && data.type === 'chat_connected') {
      loading.value = false
      connected.value = true
      agentName.value = data.agentName || '客服'
    }
  })
})
</script>

<style scoped>
#chatbase-container {
  height: 500px;
  border: 1px solid #eaeaea;
}
</style>
