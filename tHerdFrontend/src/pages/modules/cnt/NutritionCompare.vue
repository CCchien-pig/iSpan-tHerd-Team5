<template>
  <div class="container py-4">
    <!-- 1️⃣ 食材選擇 -->
    <section class="compare-step p-4 mb-4 rounded-3 shadow-sm bg-white">
      <h4 class="main-color-green-text mb-3">選擇要比較的食材</h4>

      <!-- 搜尋 + 下拉 -->
      <div class="row g-3 align-items-center">
        <div class="col-md-8">
          <input v-model.trim="keyword" type="search"
                 class="form-control border-main-color-green"
                 placeholder="輸入食材名稱或關鍵字…" @input="filterDropdown"/>
        </div>
        <div class="col-md-4 text-md-end">
          <button class="btn teal-reflect-button text-white px-4"
                  @click="toggleDropdown">
            {{ showDropdown ? '收起清單' : '展開全部食材' }}
          </button>
        </div>
      </div>

      <!-- 下拉清單 -->
      <div v-if="showDropdown" class="mt-3 border rounded p-2 bg-light" style="max-height:300px;overflow-y:auto;">
        <div v-for="s in filteredSamples" :key="s.sampleId"
             class="py-1 d-flex justify-content-between align-items-center border-bottom">
          <span>{{ s.sampleName }}</span>
          <button class="btn btn-sm btn-outline-secondary"
                  :disabled="compareList.some(c => c.sampleId===s.sampleId)"
                  @click="addSample(s)">加入</button>
        </div>
      </div>

      <!-- 已選食材 -->
      <div class="mt-4">
        <strong>已選食材：</strong>
        <span v-for="c in compareList" :key="c.sampleId"
              class="badge bg-light border text-dark me-2 d-flex align-items-center">
          {{ c.sampleName }}
          <button class="btn btn-sm btn-link text-danger ms-1"
                  @click="removeSample(c.sampleId)">✕</button>
        </span>
        <small class="text-muted d-block mt-2">請選擇 2–6 種食材</small>
      </div>
    </section>

    <!-- 2️⃣ 營養素選擇 -->
    <section class="compare-step p-4 mb-4 rounded-3 shadow-sm bg-white">
      <h4 class="main-color-green-text mb-3">選擇要比較的營養素</h4>

      <div class="d-flex flex-wrap gap-2">
        <label v-for="a in analyteOptions" :key="a.analyteId"
               class="form-check-label border rounded px-3 py-1 bg-light">
          <input type="checkbox" v-model="selectedAnalytes"
                 :value="a.analyteId" class="form-check-input me-2"/>
          {{ a.analyteName }}
        </label>
      </div>
      <small class="text-muted d-block mt-2">建議選擇 5–10 種營養素</small>

      <div class="text-end mt-3">
        <button class="btn teal-reflect-button text-white px-4"
                @click="fetchCompare">開始比較</button>
      </div>
    </section>

    <!-- 3️⃣ 結果圖表 -->
    <section v-if="groups.length" class="compare-step p-4 rounded-3 shadow-sm bg-white">
      <div class="d-flex align-items-center justify-content-between mb-3">
        <h4 class="main-color-green-text m-0">比較結果（依單位分群）</h4>
        <div class="btn-group">
          <button class="btn btn-outline-primary"
                  :class="{active: chartType==='bar'}"
                  @click="chartType='bar'; renderAll()">條狀圖</button>
          <button class="btn btn-outline-primary"
                  :class="{active: chartType==='radar'}"
                  @click="chartType='radar'; renderAll()">雷達圖</button>
        </div>
      </div>

      <div v-for="(grp, gi) in groups" :key="gi" class="mb-5">
        <h5 class="main-color-green-text mb-3">單位：{{ grp.unit }}</h5>
        <div :ref="el => chartRefs[gi] = el"
             class="chart-box border rounded-3 p-2 bg-light"
             style="height:520px;"></div>
      </div>
    </section>

    <div v-else-if="loading" class="text-center py-5 text-muted">載入中…</div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, nextTick } from "vue"
import * as echarts from "echarts"
import { getNutritionList, getNutritionCompare } from "@/pages/modules/cnt/api/cntService"

const keyword = ref("")
const allSamples = ref([])
const filteredSamples = ref([])
const showDropdown = ref(false)
const compareList = ref([])
const selectedAnalytes = ref([])
const chartType = ref("bar")
const groups = ref([])
const loading = ref(false)
const chartRefs = reactive({})

// 模擬營養素（可替換成動態查詢）
const analyteOptions = ref([
  { analyteId: 1105, analyteName: "α-維生素E當量(α-TE)" },
  { analyteId: 1106, analyteName: "酪酸(4:0)" },
  { analyteId: 1107, analyteName: "維生素C" },
  { analyteId: 1108, analyteName: "維生素A" },
  { analyteId: 1109, analyteName: "鈣" },
  { analyteId: 1110, analyteName: "鐵" },
])

// 初始化全部食材清單
onMounted(async () => {
  try {
    const res = await getNutritionList({ all: true })
    allSamples.value = res.items || []
    filteredSamples.value = allSamples.value
  } catch (err) {
    console.error("無法載入食材清單", err)
    alert("載入食材清單失敗，請檢查 API")
  }
})

// 下拉顯示/隱藏
function toggleDropdown() {
  showDropdown.value = !showDropdown.value
}

// 關鍵字篩選
function filterDropdown() {
  const kw = keyword.value.trim()
  filteredSamples.value = !kw
    ? allSamples.value
    : allSamples.value.filter(s => s.sampleName.includes(kw))
}

// 加入/移除食材
function addSample(s) {
  if (compareList.value.length >= 6) return alert("最多可比較 6 種食材")
  compareList.value.push(s)
}
function removeSample(id) {
  compareList.value = compareList.value.filter(x => x.sampleId !== id)
}

// 呼叫後端 /compare
async function fetchCompare() {
  if (compareList.value.length < 2 || selectedAnalytes.value.length < 1)
    return alert("請至少選 2 種食材與 1 種營養素")

  loading.value = true
  groups.value = []
  try {
    const sampleIds = compareList.value.map(x => x.sampleId).join(",")
    const analyteIds = selectedAnalytes.value.join(",")
    const res = await getNutritionCompare(sampleIds, analyteIds)
    groups.value = res.groups || []
    if (!groups.value.length) {
      alert("查無比較資料，請確認選擇的營養素與食材")
    } else {
      await nextTick()
      renderAll()
    }
  } catch (err) {
    console.error(err)
    alert("無法取得比較資料，請檢查後端 API")
  } finally {
    loading.value = false
  }
}

// 多圖繪製 + 平均線 + 標籤
function renderAll() {
  // 清除舊圖表
  Object.values(chartRefs).forEach(el => el?.__chartInstance?.dispose())

  groups.value.forEach((grp, gi) => {
    const el = chartRefs[gi]
    if (!el) return
    const chart = echarts.init(el)
    el.__chartInstance = chart

    const analyteNames = grp.analytes.map(a => a.analyteName)
    const sampleNames = grp.analytes[0]?.values.map(v => v.sampleName) || []
    const dataset = sampleNames.map(() => [])
    grp.analytes.forEach(a => {
      a.values.forEach((v, si) => dataset[si].push(v.value))
    })

    const averages = analyteNames.map((_, i) =>
      dataset.reduce((sum, arr) => sum + (arr[i] || 0), 0) / dataset.length
    )

    const option =
      chartType.value === "bar"
        ? {
            tooltip: { trigger: "axis" },
            legend: { data: sampleNames },
            xAxis: { type: "category", data: analyteNames },
            yAxis: { type: "value", name: grp.unit },
            series: [
              ...sampleNames.map((s, i) => ({
                name: s,
                type: "bar",
                data: dataset[i],
                label: { show: true, position: "top", fontSize: 12 },
              })),
              {
                name: "平均值",
                type: "line",
                data: averages,
                lineStyle: { type: "dashed", color: "#888" },
                symbol: "none",
              },
            ],
          }
        : {
            tooltip: {},
            legend: { data: sampleNames },
            radar: {
              indicator: analyteNames.map((n, i) => ({
                name: n,
                max: Math.max(...dataset.flat()) * 1.2 || 1,
              })),
            },
            series: [
              {
                type: "radar",
                data: sampleNames.map((s, i) => ({
                  name: s,
                  value: dataset[i],
                })),
              },
            ],
          }

    chart.setOption(option)
    // resize 事件（防抖）
    let resizeTimer
    window.addEventListener("resize", () => {
      clearTimeout(resizeTimer)
      resizeTimer = setTimeout(() => chart.resize(), 200)
    })
  })
}
</script>

<style scoped>
.compare-step { border: 1px solid #e9f6f6; }
.border-main-color-green { border-color: rgb(0,112,131) !important; }
.chart-box { width: 100%; }
</style>
