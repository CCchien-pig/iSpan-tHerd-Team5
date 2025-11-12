<template>
  <div class="center-narrow py-5">
    <h3 class="text-center mb-4 main-color-green-text">即時客服聊天室（SignalR）</h3>

    <div class="text-center mb-3">
  <span :class="connected ? 'text-success' : 'text-danger'">
    ● {{ connected ? '已連線' : '未連線' }}
  </span>
</div>

    <!-- 聊天框 -->
    <div class="chat-box border rounded-4 shadow-sm p-3 bg-white" style="height:400px; overflow-y:auto;">
      <div
        v-for="(m, i) in messages"
        :key="i"
        :class="['my-2', m.sender === userName ? 'text-end' : 'text-start']"
      >
        <div
          :class="[
            'd-inline-block px-3 py-2 rounded-4',
            m.sender === userName
              ? 'bg-primary text-white'
              : 'bg-light border'
          ]"
        >
          <small v-if="m.sender !== userName" class="text-muted">{{ m.sender }}：</small>
          {{ m.text }}
        </div>
      </div>
    </div>

    <!-- 輸入框 -->
    <div class="input-group mt-3">
      <input
        v-model="msg"
        type="text"
        class="form-control"
        placeholder="輸入訊息..."
        @keyup.enter="send"
      />
      <button class="btn btn-primary" @click="send">送出</button>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import * as signalR from '@microsoft/signalr'
import { useAuthStore } from '@/stores/auth'

const messages = ref([])
const msg = ref('')
const chatId = ref('chat-demo-001')
const connected = ref(false)
const auth = useAuthStore()
let connection = null

onMounted(async () => {
  // ✅ 從登入後的 auth store 取 Token
  const token = auth.accessToken
  console.log('目前 Token:', token)


  // ✅ 用 accessTokenFactory 傳給 Hub
  connection = new signalR.HubConnectionBuilder()
    .withUrl('https://localhost:7103/chatHub', {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect()
    .build()

  // === 狀態監控 ===
  connection.onreconnected(() => (connected.value = true))
  connection.onclose(() => (connected.value = false))

  // === 接收訊息事件 ===
  connection.on('ReceiveMessage', (sender, text) => {
    messages.value.push({ sender, text })
    scrollToBottom()
  })

  try {
    await connection.start()
    console.log('✅ SignalR 已連線')
    connected.value = true
    await connection.invoke('JoinChat', chatId.value)
  } catch (err) {
    console.error('❌ 連線失敗', err)
    connected.value = false
  }
})

async function send() {
  if (!msg.value.trim()) return
  if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
    alert('尚未連線，請稍候再試。')
    return
  }
  await connection.invoke('SendMessage', chatId.value, msg.value)
  msg.value = ''
}

function scrollToBottom() {
  const box = document.querySelector('.chat-box')
  if (box) box.scrollTop = box.scrollHeight
}

onUnmounted(() => {
  if (connection) connection.stop()
})
</script>




<style scoped>
.chat-box {
  background-color: #f9f9f9;
}
</style>
