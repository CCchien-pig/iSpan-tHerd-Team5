<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'

const baseAddress = 'https://localhost:7103'

const logisticsList = ref([])
const loading = ref(false)
const error = ref('')

onMounted(async () => {
  loading.value = true
  try {
    // 取得全部啟用中物流商資料（用 baseAddress 組成完整 API 路徑）
    const res = await axios.get(`${baseAddress}/api/sup/Logistics/active`)
    logisticsList.value = res.data
  } catch (err) {
    error.value = '載入失敗: ' + (err?.response?.data?.message || err.message)
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div>
    <h2>運送資訊</h2>
    <div v-if="loading">載入中...</div>
    <div v-if="error">{{ error }}</div>
    <table v-if="!loading && logisticsList.length > 0">
      <thead>
        <tr>
          <!-- <th>物流商 ID</th> -->
          <th>物流商名稱</th>
          <th>運送方式</th>
          <!-- <th>狀態</th> -->
        </tr>
      </thead>
      <tbody>
        <tr v-for="item in logisticsList" :key="item.logisticsId">
          <!-- <td>{{ item.logisticsId }}</td> -->
          <td>{{ item.logisticsName }}</td>
          <td>{{ item.shippingMethod }}</td>
          <!-- <td>{{ item.isActive ? '啟用' : '停用' }}</td> -->
        </tr>
      </tbody>
    </table>
    <div v-if="!loading && logisticsList.length === 0">目前無物流商資料</div>
  </div>
</template>

<style scoped>
div {
  height: 80vh;
  margin-top: 50px;
}
h2 {
  margin-top: 30px;
  margin-left: 20px;
}
table {
  border-collapse: collapse;
  width: 90%;
  margin-top: 20px;
  margin-left: auto;
  margin-right: auto;
}
th,
td {
  border: 1px solid #ddd;
  padding: 0.5rem;
  text-align: center;
}
thead {
  background: #f7f7f7;
}
</style>
