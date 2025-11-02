<template>
  <div class="center-narrow py-5">
    <h3 class="text-center mb-4 main-color-green-text">我的工單</h3>

    <div v-if="loading" class="text-center my-5">
      <div class="spinner-border text-success"></div>
    </div>

    <div v-else-if="tickets.length === 0" class="text-center text-muted">
      目前尚無任何工單。
    </div>

    <div v-else>
      <div v-for="t in tickets" :key="t.ticketId"
           class="card mb-3 border-0 shadow-sm hover-card rounded-4 p-3">
        <div class="d-flex justify-content-between align-items-center mb-1">
          <h5 class="mb-0">{{ t.subject }}</h5>
          <small class="text-muted">{{ formatDate(t.createdDate) }}</small>
        </div>
        <p class="text-muted mb-1">分類：{{ t.categoryName || '未分類' }}</p>
        <p class="text-muted small">狀態：
          <span class="badge bg-secondary">{{ t.statusText }}</span>
        </p>

        <div class="text-end">
          <router-link
            class="btn btn-primary btn-sm"
            :to="`/cs/tickets/${t.ticketId}`"
          >
            查看回復
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import http from '@/api/http'

const tickets = ref([])
const loading = ref(true)

onMounted(async () => {
  try {
    const res = await http.get('/cs/cstickets/my')
    if (res.data?.success) tickets.value = res.data.data
  } catch (err) {
    console.error('取得工單清單失敗', err)
  } finally {
    loading.value = false
  }
})

function formatDate(dt) {
  return new Date(dt).toLocaleDateString('zh-TW', { hour12: false })
}
</script>

<style scoped>
.center-narrow {
  max-width: 720px; 
  margin: 0 auto;
}
.card {
  padding: 1rem 1.25rem;
}
.hover-card {
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}
.hover-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12);
}
.btn-primary {
  background-color:rgb(0, 147, 171);
  border-color: rgb( 77, 180, 193 );
}
</style>
