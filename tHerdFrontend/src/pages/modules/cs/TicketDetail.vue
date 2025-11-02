<template>
  <div class="center-narrow py-5">
    <div v-if="loading" class="text-center my-5">
      <div class="spinner-border text-success"></div>
    </div>

    <div v-else-if="!ticket" class="text-center text-danger">
      無法載入工單資料。
    </div>

    <div v-else>
      <h3 class="text-center mb-4 main-color-green-text">工單詳情</h3>

      <div class="card p-4 shadow-sm mb-4">
        <p><b>工單編號：</b>{{ ticket.ticketId }}</p>
        <p><b>主旨：</b>{{ ticket.subject }}</p>
        <p><b>分類：</b>{{ ticket.categoryName }}</p>
        <p><b>狀態：</b>{{ ticket.statusText }}</p>
        <p><b>優先順序：</b>{{ ticket.priorityText }}</p>
        <p><b>建立時間：</b>{{ formatDate(ticket.createdDate) }}</p>
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
const loading = ref(true)

onMounted(async () => {
  const id = route.params.id
  try {
    const res = await http.get(`/api/cs/tickets/${id}`)
    if (res.data?.success) ticket.value = res.data.data
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
.center-narrow {
  max-width: 600px;
  margin: auto;
}
</style>
