<template>
  <div class="center-narrow py-5">
    <h3 class="text-center mb-4 main-color-green-text">聯絡客服</h3>

    <form @submit.prevent="submitTicket" class="card p-4 shadow-sm">
      <div class="mb-3">
        <label class="form-label">問題分類</label>
        <select v-model="form.categoryId" class="form-select">
          <option disabled value="">請選擇</option>
          <option value="1001">訂單問題</option>
          <option value="1002">付款問題</option>
          <option value="1003">運送問題</option>
        </select>
      </div>

      <div class="mb-3">
        <label class="form-label">主旨</label>
        <input v-model="form.subject" class="form-control" placeholder="請輸入問題主旨" />
      </div>

      <div class="mb-3">
        <label class="form-label">問題描述</label>
        <textarea v-model="form.messageText" rows="4" class="form-control"></textarea>
      </div>

      <div class="text-center">
        <button class="btn btn-success px-4" type="submit" :disabled="loading">
          <span v-if="!loading">送出工單</span>
          <span v-else class="spinner-border spinner-border-sm"></span>
        </button>
      </div>
    </form>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import http from '@/api/http' // 你專案裡的 axios 全域設定

const loading = ref(false)
const form = ref({
  userId: 1, // 測試階段可固定
  categoryId: '',
  subject: '',
  priority: 2,
  messageText: ''
})

async function submitTicket() {
  try {
    loading.value = true
    const res = await http.post('/api/cs/tickets/create', form.value)
    if (res.data?.success) {
      alert('工單建立成功，您的工單編號是：' + res.data.data.ticketId)
    } else {
      alert(res.data?.message || '建立失敗')
    }
  } catch (err) {
    console.error(err)
    alert('伺服器錯誤')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.center-narrow {
  max-width: 600px;
  margin: auto;
}
</style>
