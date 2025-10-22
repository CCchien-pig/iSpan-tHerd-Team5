<template>
  <div class="container py-4">
    <!-- 1️⃣ 食材選擇 -->
    <section class="compare-step p-4 mb-4 rounded-3 shadow-sm bg-white">
      <h4 class="main-color-green-text mb-3">選擇要比較的食材</h4>

      <!-- 搜尋 + 下拉 -->
      <div class="row g-3 align-items-center">
        <div class="col-md-8">
          <input
            v-model.trim="state.sampleKeyword"
            type="search"
            class="form-control border-main-color-green"
            placeholder="輸入食材名稱或關鍵字…"
            @input="filterSamples"
          />
        </div>
        <div class="col-md-4 text-md-end">
          <button
            class="btn teal-reflect-button text-white px-4"
            @click="state.showSampleDropdown = !state.showSampleDropdown"
          >
            {{ state.showSampleDropdown ? '收起清單' : '展開全部食材' }}
          </button>
        </div>
      </div>

      <!-- 下拉清單 -->
      <div
        v-if="state.showSampleDropdown"
        class="mt-3 border rounded p-2 bg-light"
        style="max-height:300px;overflow-y:auto;"
      >
        <div
          v-for="s in ui.filteredSamples"
          :key="s.sampleId"
          class="py-1 d-flex justify-content-between align-items-center border-bottom"
        >
          <span>{{ s.sampleName }}</span>
          <button
            class="btn btn-sm btn-outline-secondary"
            :disabled="ui.compareList.some(c => c.sampleId===s.sampleId)"
            @click="addSample(s)"
          >
            加入
          </button>
        </div>
      </div>

      <!-- 已選食材 -->
      <div class="mt-4">
        <strong>已選食材：</strong>
        <span
          v-for="c in ui.compareList"
          :key="c.sampleId"
          class="badge bg-light border text-dark me-2 d-inline-flex align-items-center"
        >
          {{ c.sampleName }}
          <button
            class="btn btn-sm btn-link text-danger ms-1"
            @click="removeSample(c.sampleId)"
          >
            ✕
          </button>
        </span>
        <small class="text-muted d-block mt-2">請選擇 2–6 種食材</small>
      </div>
    </section>

    <!-- 2️⃣ 營養素選擇 -->
    <section class="compare-step p-4 mb-4 rounded-3 shadow-sm bg-white">
      <div class="d-flex align-items-center justify-content-between mb-3">
        <h4 class="main-color-green-text m-0">選擇要比較的營養素</h4>

        <div class="d-flex align-items-center gap-3">
          <div class="form-check form-switch m-0">
            <input
              class="form-check-input"
              type="checkbox"
              id="toggleAll"
              v-model="state.showAllAnalytes"
              @change="loadAnalytes"
            />
            <label class="form-check-label" for="toggleAll">
              {{ state.showAllAnalytes ? '顯示：全部營養素' : '顯示：常見營養素' }}
            </label>
          </div>
          <button class="btn btn-sm btn-outline-secondary" @click="toggleAllGroups">
            {{ areAllGroupsCollapsed ? '全部展開' : '全部收合' }}
          </button>
        </div>
      </div>

      <!-- 快搜 + 全選/清空 -->
      <div class="row g-2 align-items-center mb-2">
        <div class="col-md-6">
          <input
            v-model.trim="state.analyteKeyword"
            type="search"
            class="form-control border-main-color-green"
            placeholder="搜尋營養素（中英文皆可）…"
            @input="filterAnalytes"
          />
        </div>
        <div class="col-md-6 text-md-end">
          <button
            class="btn btn-sm btn-outline-secondary me-2"
            @click="selectAllAnalytes"
          >
            全選目前篩選
          </button>
          <button
            class="btn btn-sm btn-outline-secondary"
            @click="ui.selectedAnalyteIds = []"
          >
            清空
          </button>
        </div>
      </div>

      <!-- 群組 -->
      <div class="d-flex flex-column gap-2">
        <div v-for="(group, gi) in ui.filteredAnalytesByCat" :key="group.category">
          <div
            class="group-header d-flex align-items-center gap-2 px-3 py-1 rounded-pill fw-semibold mb-2"
            :style="getGroupStyle(group.category, gi)"
            @click="toggleGroup(group.category)"
          >
            <span class="group-caret">{{ isGroupCollapsed(group.category) ? '▸' : '▾' }}</span>
            <span>{{ group.category }}</span>
          </div>

          <transition name="fade">
            <div
              v-show="!isGroupCollapsed(group.category)"
              class="d-flex flex-wrap gap-2 ms-1"
            >
              <label
                v-for="a in group.items"
                :key="a.analyteId"
                class="form-check-label analyte-item border rounded px-3 py-1 bg-light"
              >
                <input
                  type="checkbox"
                  v-model="ui.selectedAnalyteIds"
                  :value="a.analyteId"
                  class="form-check-input me-2"
                />
                {{ a.analyteName }}
              </label>
            </div>
          </transition>
        </div>
      </div>

      <small class="text-muted d-block mt-2">
        建議選擇 1–12 項。已選：{{ ui.selectedAnalyteIds.length }}
      </small>

      <div class="text-end mt-3">
        <button
          class="btn teal-reflect-button text-white px-4"
          @click="fetchCompare"
          :disabled="state.loading"
        >
          {{ state.loading ? '分析中…' : '開始比較' }}
        </button>
      </div>
    </section>

    <!-- 3️⃣ 圖表結果 -->
    <section
      v-if="ui.groups.length"
      class="compare-step p-4 rounded-3 shadow-sm bg-white"
    >
      <div class="d-flex align-items-center justify-content-between mb-3 flex-wrap gap-2">
        <h4 class="main-color-green-text m-0">比較結果（依單位分群）</h4>

        <div class="d-flex align-items-center gap-2">
          <label class="me-1 text-muted">視圖：</label>
          <select
            v-model="ui.chartType"
            class="form-select form-select-sm"
            style="width:auto"
            @change="renderAll"
          >
            <option value="bar">條狀圖（群組）</option>
            <option value="radar">雷達圖</option>
            <option value="heatmap">熱圖</option>
            <option value="stacked">堆疊百分比條圖（100%）</option>
            <option value="boxplot">箱型圖（分佈）</option>
          </select>

          <!-- 工具列 -->
          <button class="btn btn-sm btn-outline-success" @click="exportCharts">
            📤 匯出圖表
          </button>
          <button class="btn btn-sm btn-outline-primary" @click="generateShareLink">
            🔗 生成連結
          </button>
        </div>
      </div>

      <div v-for="(grp, gi) in ui.groups" :key="gi" class="mb-5">
        <h5 class="main-color-green-text mb-3">單位：{{ grp.unit }}</h5>
        <div
          :ref="el => chartRefs[gi] = el"
          class="chart-box border rounded-3 p-2 bg-light"
          style="height:520px;"
        ></div>
      </div>
    </section>

    <div v-else-if="state.loading" class="text-center py-5 text-muted">載入中…</div>
  </div>
</template>

<script setup>
import { reactive, ref, computed, onMounted, nextTick } from "vue"
import * as echarts from "echarts"
import Swal from "sweetalert2"
import { getNutritionList, getNutritionCompare, getAnalyteList } from "@/pages/modules/cnt/api/cntService"

const state = reactive({
  showSampleDropdown: false,
  sampleKeyword: "",
  analyteKeyword: "",
  showAllAnalytes: false,
  loading: false,
})

const ui = reactive({
  allSamples: [],
  filteredSamples: [],
  compareList: [],
  analyteOptions: [],
  filteredAnalytesByCat: [],
  selectedAnalyteIds: [],
  collapsedGroups: new Set(),
  groups: [],
  chartType: "bar",
})

const chartRefs = reactive({})

/* ---------- 初始化 ---------- */
onMounted(async () => {
  await Promise.all([loadSamples(), loadAnalytes()])
})

/* ---------- 載入食材 ---------- */
async function loadSamples() {
  const res = await getNutritionList({ all: true })
  ui.allSamples = res.items || []
  ui.filteredSamples = ui.allSamples
}

function filterSamples() {
  const kw = state.sampleKeyword.trim().toLowerCase()
  ui.filteredSamples = !kw
    ? ui.allSamples
    : ui.allSamples.filter((s) => s.sampleName.toLowerCase().includes(kw))
}

/* ---------- 載入營養素 ---------- */
async function loadAnalytes() {
  const res = await getAnalyteList(!state.showAllAnalytes ? true : false)
  const items = res.items || []
  groupAnalytes(items)
}

function groupAnalytes(items) {
  const map = new Map()
  for (const a of items) {
    const cat = a.category || "未分類"
    if (!map.has(cat)) map.set(cat, [])
    map.get(cat).push(a)
  }
  ui.filteredAnalytesByCat = Array.from(map, ([category, items]) => ({
    category,
    items,
  }))
}

/* ---------- 群組收合 ---------- */
function toggleGroup(cat) {
  if (ui.collapsedGroups.has(cat)) ui.collapsedGroups.delete(cat)
  else ui.collapsedGroups.add(cat)
}
function isGroupCollapsed(cat) {
  return ui.collapsedGroups.has(cat)
}
function toggleAllGroups() {
  if (ui.filteredAnalytesByCat.length === ui.collapsedGroups.size)
    ui.collapsedGroups.clear()
  else
    ui.filteredAnalytesByCat.forEach((g) => ui.collapsedGroups.add(g.category))
}

/* ---------- 選取 ---------- */
function addSample(s) {
  if (ui.compareList.length >= 6) return showAlert("最多只能比較 6 種食材")
  ui.compareList.push(s)
}
function removeSample(id) {
  ui.compareList = ui.compareList.filter((x) => x.sampleId !== id)
}

/* ---------- 篩選 ---------- */
function filterAnalytes() {
  const kw = state.analyteKeyword.trim().toLowerCase()
  if (!kw) return loadAnalytes()
  ui.filteredAnalytesByCat.forEach((g) => {
    g.items = g.items.filter((a) =>
      a.analyteName.toLowerCase().includes(kw)
    )
  })
}

/* ---------- SweetAlert ---------- */
function showAlert(text, icon = "warning") {
  Swal.fire({
    title: text,
    icon,
    confirmButtonColor: "#007083",
    confirmButtonText: "確定",
  })
}

/* ---------- 驗證 ---------- */
function validateSelection() {
  if (ui.compareList.length < 2 || ui.compareList.length > 6)
    return showAlert("請選擇 2–6 種食材")
  if (ui.selectedAnalyteIds.length < 1 || ui.selectedAnalyteIds.length > 12)
    return showAlert("請選擇 1–12 種營養素")
  return true
}

/* ---------- 呼叫後端 ---------- */
async function fetchCompare() {
  if (!validateSelection()) return

  state.loading = true
  ui.groups = []
  try {
    const sampleIds = ui.compareList.map((x) => x.sampleId).join(",")
    const analyteIds = ui.selectedAnalyteIds.join(",")
    const res = await getNutritionCompare(sampleIds, analyteIds)
    ui.groups = res.groups || []
    if (!ui.groups.length)
      return showAlert("查無比較資料，請確認選擇的營養素與食材", "error")
    await nextTick()
    renderAll()
  } catch (err) {
    showAlert("無法取得比較資料，請檢查後端 API", "error")
  } finally {
    state.loading = false
  }
}

/* ---------- 匯出圖表 ---------- */
function exportCharts() {
  const canvasList = []
  Object.values(chartRefs).forEach((el, i) => {
    const chart = echarts.getInstanceByDom(el)
    if (chart) {
      const dataUrl = chart.getDataURL({ pixelRatio: 2, backgroundColor: "#fff" })
      const link = document.createElement("a")
      link.href = dataUrl
      link.download = `營養比較圖表_${i + 1}.png`
      link.click()
    }
  })
  showAlert("圖表已匯出為 PNG！", "success")
}

/* ---------- 生成分享連結 ---------- */
function generateShareLink() {
  if (!validateSelection()) return
  const params = new URLSearchParams({
    samples: ui.compareList.map((s) => s.sampleId).join(","),
    analytes: ui.selectedAnalyteIds.join(","),
  })
  const shareUrl = `${window.location.origin}${window.location.pathname}?${params.toString()}`
  navigator.clipboard.writeText(shareUrl)
  showAlert("已複製分享連結到剪貼簿！", "success")
}

/* ---------- 繪圖 ---------- */
function renderAll() {
  Object.values(chartRefs).forEach((el) => el?.__chartInstance?.dispose?.())
  ui.groups.forEach((grp, gi) => {
    const el = chartRefs[gi]
    if (!el) return
    const chart = echarts.init(el)
    el.__chartInstance = chart
    const analytes = grp.analytes
    const analyteNames = analytes.map((a) => a.analyteName)
    const sampleNames = analytes[0]?.values.map((v) => v.sampleName) || []
    const dataset = sampleNames.map(() => [])
    analytes.forEach((a) => {
      a.values.forEach((v, si) => dataset[si].push(Number(v.value) || 0))
    })
    chart.setOption({
      tooltip: { trigger: "axis" },
      legend: { data: sampleNames },
      xAxis: { type: "category", data: analyteNames },
      yAxis: { type: "value", name: grp.unit },
      series: sampleNames.map((s, i) => ({
        name: s,
        type: "bar",
        data: dataset[i],
        label: { show: true, position: "top" },
      })),
    })
  })
}

/* ---------- 樣式 ---------- */
function getGroupStyle(category, index) {
  const palette = [
    { bg: "#e9f6f6", border: "#007083", text: "#004b4b" },
    { bg: "#e6f0fa", border: "#005bbb", text: "#0c2f6b" },
    { bg: "#fff4e5", border: "#f7931e", text: "#8a4b00" },
    { bg: "#f8e9f6", border: "#b76ac4", text: "#6d2b7a" },
    { bg: "#f0f0f0", border: "#7a7a7a", text: "#3a3a3a" },
  ]
  const c = palette[index % palette.length]
  return {
    backgroundColor: c.bg,
    color: c.text,
    borderLeft: `6px solid ${c.border}`,
    cursor: "pointer",
  }
}
</script>

<style scoped>
.compare-step { border: 1px solid #e9f6f6; }
.border-main-color-green { border-color: rgb(0,112,131) !important; }
.chart-box { width: 100%; }
.group-caret { width: 1em; display: inline-block; }
.analyte-item:hover {
  background-color: #f2fbfb;
  box-shadow: 0 0 0 2px rgba(0,112,131,0.2);
}
.fade-enter-active, .fade-leave-active { transition: opacity 0.25s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>
