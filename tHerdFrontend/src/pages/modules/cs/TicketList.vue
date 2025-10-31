<template>
  <div class="center-narrow py-5">
    <h3 class="text-center mb-4 main-color-green-text">我的工單</h3>

    <div v-if="loading" class="text-center my-5">
      <div class="spinner-border text-success"></div>
    </div>

    <div v-else-if="tickets.length === 0" class="text-center text-muted">
      目前尚無任何工單。
    </div>

    <table v-else class="table table-hover align-middle">
      <thead class="table-light">
        <tr>
          <th>工單編號</th>
          <th>主旨</th>
          <th>分類</th>
          <th>狀態</th>
          <th>優先順序</th>
          <th>建立日期</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="t in tickets" :key="t.ticketId">
          <td>{{ t.ticketId }}</td>
          <td>{{ t.subject }}</td>
          <td>{{ t.categoryName }}</td>
          <td>{{ t.statusText }}</td>
          <td>{{ t.priorityText }}</td>
          <td>{{ formatDate(t.createdDate) }}</td>
          <td>
            <router-link
              :to="`/cs/tickets/${t.ticketId}`"
              class="btn btn-sm btn-outline-success"
            >
              查看詳情
            </router-link>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import http from '@/api/http'

const tickets = ref([])
const loading = ref(true)

onMounted(async () => {
  try {
    const res = await http.get('/api/cs/tickets/list')
    if (res.data?.success) tickets.value = res.data.data
  } catch (err) {
    console.error('取得工單清單失敗', err)
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
  max-width: 800px;
  margin: auto;
}
</style>
