<template>
  <div class="center-narrow py-5">
    <h3 class="text-center mb-4 main-color-green-text">我的客服紀錄</h3>

    <div v-if="loading" class="text-center my-5">
      <div class="spinner-border text-success"></div>
    </div>

    <div v-else-if="tickets.length === 0" class="text-center text-muted">
      您尚未建立任何客服工單。
    </div>

    <div v-else>
      <div v-for="t in tickets" :key="t.ticketId" class="ticket-card mb-3 shadow-sm">
        <div class="d-flex justify-content-between align-items-center">
          <h5 class="fw-bold mb-1">{{ t.subject }}</h5>
<span class="badge" :class="getStatusClass(t.statusText)">
  {{ t.statusText }}
</span>

        </div>

        <div class="small text-muted mb-2">
          <i class="bi bi-calendar-event"></i>
          {{ formatDate(t.createdDate) }}
          <span class="mx-2">｜</span>
          分類：{{ t.categoryName || '未分類' }}
        </div>

        <p class="mb-0 text-body-secondary small">
          {{ t.userMessage || '（沒有留下問題描述）' }}
        </p>
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
  return new Date(dt).toLocaleString('zh-TW', { hour12: false })
}
function getStatusClass(status) {
  switch (status) {
    case '待處理':
      return 'bg-warning text-dark'
    case '已回覆':
      return 'bg-success'
    case '已結案':
      return 'bg-secondary'
    default:
      return 'bg-light text-dark'
  }
}

</script>

<style scoped>
.center-narrow {
  max-width: 720px;
  margin: 0 auto;
}

.ticket-card {
  background: #fff;
  border: 1px solid #eaeaea;
  border-radius: 1rem;
  padding: 1rem 1.25rem;
  transition: all 0.25s ease;
}
.ticket-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12);
}

.badge.bg-status {
  background-color: rgb(0, 147, 171);
}

.main-color-green-text {
  color: rgb(0, 147, 171);
}
</style>
