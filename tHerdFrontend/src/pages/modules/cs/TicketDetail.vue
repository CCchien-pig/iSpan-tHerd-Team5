<template>
  <div class="center-narrow py-5">
    <h3 class="text-center mb-4 main-color-green-text">工單詳情</h3>

    <div v-if="loading" class="text-center my-5">
      <div class="spinner-border text-success"></div>
    </div>

    <div v-else-if="!ticket" class="text-center text-danger">
      無法載入工單資料。
    </div>

    <div v-else>
      <div class="card shadow-sm border-0 rounded-4 p-4 mb-4">
        <p><b>主旨：</b>{{ ticket.subject }}</p>
        <p><b>分類：</b>{{ ticket.categoryName }}</p>
        <p><b>建立時間：</b>{{ formatDate(ticket.createdDate) }}</p>
      </div>

      <!-- 對話紀錄 -->
      <div class="chat-box p-3 bg-light rounded-4 shadow-sm mb-3">
        <div
          v-for="m in messages"
          :key="m.messageId"
          class="mb-3"
          :class="m.senderType === 1 ? 'text-end' : 'text-start'"
        >
          <div
            :class="[
              'p-2 rounded-3 d-inline-block',
              m.senderType === 1
                ? 'bg-white border text-dark'
                : 'bg-success text-white'
            ]"
          >
            {{ m.messageText }}
          </div>
          <div class="text-muted small mt-1">{{ formatDate(m.createdDate) }}</div>
        </div>
      </div>

      <div class="text-center">
        <router-link to="/cs/tickets" class="btn btn-outline-secondary">返回我的工單</router-link>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import http from '@/api/http'

const route = useRoute()
const ticket = ref(null)
const messages = ref([])
const loading = ref(true)

onMounted(async () => {
  const id = route.params.id
  try {
    const [tRes, mRes] = await Promise.all([
      http.get(`/api/cs/cstickets/${id}`),
      http.get(`/api/cs/cstickets/${id}/messages`)
    ])
    if (tRes.data?.success) ticket.value = tRes.data.data
    if (mRes.data?.success) messages.value = mRes.data.data
  } catch (err) {
    console.error('載入工單失敗', err)
  } finally {
    loading.value = false
  }
})

function formatDate(dt) {
  return new Date(dt).toLocaleString('zh-TW', { hour12: false })
}
</script>

<style scoped>
.chat-box {
  max-height: 500px;
  overflow-y: auto;
}
</style>
