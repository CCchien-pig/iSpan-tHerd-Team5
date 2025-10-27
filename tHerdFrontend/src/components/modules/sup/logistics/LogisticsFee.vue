<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'
import LogisticsCalculator from './LogisticsCalculator.vue'

const baseAddress = 'https://localhost:7103'
const logisticsList = ref([])
const ratesMap = ref({})
const openIds = ref([])
const loading = ref(false)
const error = ref('')

onMounted(async () => {
  loading.value = true
  try {
    const { data } = await axios.get(`${baseAddress}/api/sup/Logistics`)
    logisticsList.value = data.data
    // console.log('logisticsList:', logisticsList.value)
  } catch (err) {
    error.value = '物流商載入失敗: ' + (err?.response?.data?.message || err.message)
  } finally {
    loading.value = false
  }
})

function isOpen(id) {
  return openIds.value.includes(id)
}
async function fetchRates(id) {
  if (!id || id === undefined) {
    console.error('fetchRates() 收到無效的 logisticsId：', id)
    return
  }
  if (ratesMap.value[id]) return
  const { data } = await axios.get(`${baseAddress}/api/sup/LogisticsRate/bylogistics/${id}`)
  ratesMap.value[id] = data.data ?? data
}
function toggle(id) {
  if (isOpen(id)) {
    openIds.value = openIds.value.filter((x) => x !== id)
  } else {
    openIds.value.push(id)
    fetchRates(id)
  }
}
</script>

<template>
  <div class="page-wrap">
    <!-- 1. 把運費試算器放表格上方 -->
    <LogisticsCalculator />

    <!-- 2. 原本的表格 -->
    <div class="table-wrap">
      <table class="tcat-table">
        <thead>
          <tr>
            <th style="width: 54px"></th>
            <th>物流商</th>
            <th>配送方式</th>
          </tr>
        </thead>
        <tbody>
          <template v-for="item in logisticsList" :key="item.logisticsId">
            <tr>
              <td class="accordion-cell">
                <button class="accordion-btn" @click="toggle(item.logisticsId)">
                  <span :class="{ arrow: true, open: isOpen(item.logisticsId) }">▶</span>
                </button>
              </td>
              <td>{{ item.logisticsName }}</td>
              <td>{{ item.shippingMethod }}</td>
            </tr>
            <tr v-if="isOpen(item.logisticsId)">
              <td colspan="3" class="expand-area">
                <table class="rate-inner-table">
                  <thead>
                    <tr>
                      <th>重量範圍 (kg)</th>
                      <th>運費</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-if="!ratesMap[item.logisticsId]">
                      <td colspan="2">運費分級載入中...</td>
                    </tr>
                    <tr
                      v-for="(rate, idx) in ratesMap[item.logisticsId] || []"
                      :key="rate.logisticsRateId"
                      :class="{ odd: idx % 2 === 1 }"
                    >
                      <td>{{ rate.weightMin }} kg ~ {{ rate.weightMax ?? '∞' }} kg</td>
                      <td>NTD$ {{ rate.shippingFee }}</td>
                    </tr>
                  </tbody>
                </table>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.page-wrap {
  min-height: 100vh;
}
.table-wrap {
  display: flex;
  justify-content: center;
  align-items: flex-start;
  margin-top: 20px;
}
/* 主表格 */
.tcat-table {
  background: #fff;
  border-collapse: separate;
  border-radius: 20px;
  font-family: 'Microsoft JhengHei', 'Apple LiGothic Medium', Arial, Helvetica, sans-serif;
  overflow: hidden;
  margin: 0 auto;
  max-width: 640px;
  width: 100%;
  table-layout: auto;
}
/* 外層表頭 */
.tcat-table thead th {
  /* background: #feec99; */
  background: rgb(0, 112, 131);
  color: rgb(248, 249, 250);
  font-size: 17px;
  font-weight: bold;
}
.tcat-table th,
.tcat-table td {
  color: #222;
  font-size: 16px;
  font-weight: 700;
  padding: 13px 6px;
  text-align: center;
  vertical-align: middle;
  border: 1px solid #e9e9e9;
  white-space: nowrap;
}
/* 外層主表格：箭頭所在儲存格底色 */
.tcat-table td.accordion-cell {
  background: #ddf4f9;
}
/* 展開行整段底色 */
.tcat-table .expand-area {
  background: #ddf4f9; /*改為指定色*/
  padding: 0;
}

/* 內層子表格 */
.expand-area {
  background: #fafafa;
  padding: 0;
}
.rate-inner-table {
  margin: 10px auto;
  border-radius: 10px;
  border-collapse: separate;
  width: 98%;
  table-layout: auto;
  box-sizing: border-box;
}
.rate-inner-table thead th {
  background: #4db4c1;
  font-size: 15px;
  font-weight: bold;
}
.rate-inner-table th,
.rate-inner-table td {
  border: 1px solid #64c1c9;
  padding: 10px 3px;
  font-size: 15px;
  background: #fff;
}
.rate-inner-table tr.odd td {
  background: #f1f1f1 !important;
}
.accordion-btn {
  background: none;
  border: none;
  cursor: pointer;
  font-size: 18px;
}
.arrow {
  display: inline-block;
  transition: transform 0.2s;
  color: rgb(0, 112, 131);
}
.arrow.open {
  transform: rotate(90deg);
}
</style>
