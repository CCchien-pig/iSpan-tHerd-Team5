<script setup>
import { ref, onMounted, watch } from 'vue'
import axios from 'axios'

const baseAddress = 'https://localhost:7103'
const logisticsList = ref([])
const selectedMethod = ref(null)
const weight = ref('')
const fee = ref(null)
const error = ref('')
const loading = ref(false)
const rateLoading = ref(false)
const rates = ref([])

onMounted(async () => {
  loading.value = true
  try {
    const { data } = await axios.get(`${baseAddress}/api/sup/Logistics`)
    logisticsList.value = data.data
  } catch (err) {
    error.value = '物流商載入失敗: ' + (err?.response?.data?.message || err.message)
  } finally {
    loading.value = false
  }
})
watch(selectedMethod, async (val) => {
  fee.value = null
  weight.value = ''
  rates.value = []
  if (!val) return
  rateLoading.value = true
  try {
    const { data } = await axios.get(
      `${baseAddress}/api/sup/LogisticsRate/bylogistics/${val.logisticsId}`,
    )
    rates.value = data.data
  } catch (err) {
    error.value = '運費分級載入失敗: ' + (err?.response?.data?.message || err.message)
  } finally {
    rateLoading.value = false
  }
})

function calcFee() {
  error.value = ''
  fee.value = null
  if (!selectedMethod.value || !weight.value) {
    error.value = '請選擇配送方式並輸入重量'
    return
  }
  const w = parseFloat(weight.value)
  if (isNaN(w) || w <= 0) {
    error.value = '請輸入正確有效的重量'
    return
  }
  const rate = rates.value.find(
    (r) => w >= r.weightMin && (r.weightMax == null || w <= r.weightMax),
  )
  if (rate) fee.value = rate.shippingFee
  else error.value = '重量超出運費分級範圍'
}
</script>

<template>
  <div class="calculator-container">
    <div class="calc-title">物流運費試算</div>
    <form class="form-zone" @submit.prevent="calcFee" autocomplete="off">
      <label for="method-select"> 配送方式 </label>
      <select id="method-select" name="logisticsMethod" v-model="selectedMethod" required>
        <option :value="null" disabled>請選擇</option>
        <option v-for="item in logisticsList" :key="item.logisticsId" :value="item">
          {{ item.logisticsName }} - {{ item.shippingMethod }}
        </option>
      </select>

      <span v-if="loading" class="info-text">物流商載入中...</span>

      <label for="weight-input"> 包裹重量 (kg) </label>
      <input
        id="weight-input"
        name="weight"
        type="number"
        v-model="weight"
        :disabled="!selectedMethod"
        step="0.01"
        min="0"
        required
        placeholder="請輸入重量"
        autocomplete="off"
      />

      <button type="submit" :disabled="!selectedMethod || !weight || rateLoading">查詢運費</button>
      <span v-if="rateLoading" class="info-text">運費分級載入中...</span>
    </form>

    <div class="result-zone">
      <div v-if="fee !== null">
        運費：<span class="fee-text">NTD$ {{ fee }}</span>
      </div>
      <div v-if="error" class="error-text">{{ error }}</div>
    </div>
  </div>
</template>

<style scoped>
.calculator-container {
  max-width: 80vw;
  margin: 16px auto;
  padding: 12px 8px;
  border-radius: 18px;
  box-shadow: 0 0 10px rgb(203, 229, 235);
  background: #fff;
  font-family: 'Microsoft JhengHei', 'Apple LiGothic Medium', Arial, Helvetica, sans-serif;
}
.calc-title {
  font-size: 18px;
  font-weight: bold;
  text-align: center;
  margin-bottom: 10px;
  padding-top: 10px;
  line-height: 1;
}
.form-zone {
  display: flex;
  flex-direction: row;
  gap: 18px;
  justify-content: center;
  align-items: center;
  margin: 10px 10px 0 10px;
  min-height: 40px; /* 固定高度，避免跳動 */
  position: relative;
}
label {
  font-weight: bold;
  font-size: 16px;
  display: flex;
  gap: 8px;
  align-items: center;
  margin-bottom: 0;
}
select,
input {
  font-size: 15px;
  padding: 2px 7px;
  border-radius: 6px;
  border: 1px solid #ccc;
  margin-left: 6px;
  height: 32px;
}
button {
  font-size: 15px;
  padding: 4px 16px;
  border-radius: 7px;
  background: #d3ece8;
  font-weight: bold;
  border: none;
  cursor: pointer;
  margin-left: 0;
  min-height: 32px;
  transition: background 0.15s;
}
button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
.info-text {
  margin-left: 8px;
  font-size: 14px;
  color: #6b82a7;
  position: absolute; /* 絕對定位，不影響排版 */
  left: 50%;
  bottom: -20px; /* 放在表單下方 */
  transform: translateX(-50%);
  white-space: nowrap;
}
.result-zone {
  margin-top: 10px;
  min-height: 22px;
  font-size: 16px;
  text-align: center;
}
.fee-text {
  font-weight: bold;
  color: #367cad;
}
.error-text {
  color: #d50052;
  font-weight: bold;
}
</style>
