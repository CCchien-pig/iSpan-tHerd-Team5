<template>
  <div class="wrap">
    <h3>配送資訊</h3>
    <div class="status" v-if="loading">載入中...</div>
    <div class="status" v-if="error">{{ error }}</div>

    <table v-if="!loading && logisticsList.length > 0">
      <thead>
        <tr>
          <th>物流商名稱</th>
          <th>運送方式</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="item in logisticsList" :key="item.logisticsId">
          <td>{{ item.logisticsName }}</td>
          <td>{{ item.shippingMethod }}</td>
        </tr>
      </tbody>
    </table>

    <div class="status" v-if="!loading && logisticsList.length === 0">目前無物流商資料</div>
  </div>
</template>

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
    logisticsList.value = res.data.data
  } catch (err) {
    error.value = '載入失敗: ' + (err?.response?.data?.message || err.message)
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
/* div {
  height: 80vh;
  margin-top: 50px;
} */
.wrap {
  margin-top: 6px;
}
.status {
  margin: 8px 0;
  color: #4a5568;
}
/* h2 {
  margin-top: 30px;
  margin-left: 13px;
} */
table {
  width: 100%;
  border-collapse: separate; /* 關鍵 */
  border-spacing: 0; /* 關鍵 */
  border: 1px solid rgb(77, 180, 193);
  border-radius: 8px; /* 直接套在 table 本身 */
  overflow: hidden; /* 防止內容溢出圓角 */
}

/* 表頭配色維持一致 */
thead {
  background: rgb(0, 112, 131);
  color: rgb(248, 249, 250);
}
th,
td {
  padding: 0.5rem;
  text-align: center;
  /* 避免內部格線把圓角切齊，改用列底線的視覺 */
  border: none;
  border-bottom: 1px solid rgba(77, 180, 193, 0.651);
}
/* 最後一列不畫底線，避免和外框重疊 */
tbody tr:last-child td {
  border-bottom: none;
}
</style>
