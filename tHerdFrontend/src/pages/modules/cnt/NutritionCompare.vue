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
          <button class="btn teal-reflect-button text-white px-4" @click="state.showSampleDropdown = !state.showSampleDropdown">
            {{ state.showSampleDropdown ? '收起清單' : '展開全部食材' }}
          </button>
        </div>
      </div>

      <!-- 下拉清單 -->
      <div v-if="state.showSampleDropdown" class="mt-3 border rounded p-2 bg-light" style="max-height:300px; overflow-y:auto;">
        <div
          v-for="s in ui.filteredSamples"
          :key="s.sampleId"
          class="py-1 d-flex justify-content-between align-items-center border-bottom"
        >
          <span>{{ s.sampleName }}</span>
          <button
            class="btn btn-sm btn-outline-secondary"
            :disabled="ui.compareList.some(c => c.sampleId === s.sampleId)"
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
          <button class="btn btn-sm btn-link text-danger ms-1" @click="removeSample(c.sampleId)">✕</button>
        </span>
        <small class="text-muted d-block mt-2">（請選擇 2 – 6 種食材！）</small>
      </div>
    </section>

    <!-- 2️⃣ 營養素選擇（動態載入 + 快搜 + 全部/常見切換 + 群組摺疊） -->
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

          <button class="btn silver-reflect-button btn-sm text-dark px-3" @click="toggleAllGroups">
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
          <button class="btn silver-reflect-button btn-sm text-dark px-3 me-2" @click="selectAllAnalytes">全選目前篩選</button>
          <button class="btn silver-reflect-button btn-sm text-dark px-3" @click="ui.selectedAnalyteIds = []">清空</button>
        </div>
      </div>

      <!-- 群組（可摺疊） -->
      <div class="d-flex flex-column">
        <div
          v-for="(group, gi) in ui.filteredAnalytesByCat"
          :key="group.category"
          class="mb-3 border-start ps-2"
        >
          <!-- 群組標題 -->
          <button
            class="group-header btn btn-sm d-inline-flex align-items-center gap-2 px-3 py-1 fw-semibold rounded-pill"
            :style="getGroupStyle(group.category, gi)"
            @click="toggleGroup(group.category)"
          >
            <i
              class="bi"
              :class="isGroupCollapsed(group.category)
                ? 'bi-caret-right-fill rotate-90'
                : 'bi-caret-down-fill rotate-0'"
            ></i>
            <span>{{ group.category }}</span>
          </button>

          <!-- ✅ 群組內容要包在「同一個 div」裡 -->
          <transition name="fade-collapse">
            <div
              v-show="!isGroupCollapsed(group.category)"
              class="analyte-group-content d-flex flex-wrap gap-2 mt-2"
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
        建議選擇 5–10 項（上限12項）。已選：{{ ui.selectedAnalyteIds.length }} / 可任意組合。
      </small>

      <div class="text-end mt-3">
        <button class="btn teal-reflect-button text-white px-4" @click="fetchCompare" :disabled="state.loading">
          {{ state.loading ? '分析中…' : '開始比較' }}
        </button>
      </div>
    </section>

    <!-- 3️⃣ 圖表結果：面板（依單位分群） -->
    <section v-if="ui.groups.length" class="compare-step p-4 rounded-3 shadow-sm bg-white">
      <div class="d-flex align-items-center justify-content-between mb-3">
        <h4 class="main-color-green-text m-0">比較結果（依單位分群/每100公克含量）</h4>
        <div class="d-flex align-items-center gap-2">
          <label class="me-1 text-muted">視圖：</label>
          <select v-model="ui.chartType" class="form-select form-select-sm" style="width:auto" @change="renderAll">
            <option value="bar">條狀圖（群組）</option>
            <option value="radar">雷達圖</option>
            <option value="heatmap">熱圖（樣本×營養素）</option>
            <option value="stacked">堆疊百分比條圖（100%）</option>
            <option value="boxplot">箱型圖（分佈）</option>
          </select>
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
import { reactive, ref, computed, onMounted, onBeforeUnmount, nextTick } from 'vue'
import * as echarts from 'echarts'
import { getNutritionList, getNutritionCompare, getAnalyteList } from '@/pages/modules/cnt/api/cntService'
import Swal from 'sweetalert2'
import 'sweetalert2/dist/sweetalert2.min.css' // （可選）加樣式

/* ---------- SweetAlert helper ---------- */
function showWarn(msg) {
  Swal.fire({
    text: msg,
    icon: 'warning',
    confirmButtonText: '確定',
    confirmButtonColor: 'rgb(0,112,131)'
  })
}

/* ---------- state ---------- */
const state = reactive({
  // samples
  sampleKeyword: '',
  showSampleDropdown: false,
  // analytes
  analyteKeyword: '',
  showAllAnalytes: false, // false=常見, true=全部
  // loading
  loading: false
})

const ui = reactive({
  /* samples */
  allSamples: [],
  filteredSamples: [],
  compareList: [],
  /* analytes */
  analyteOptions: [],           // 從後端動態載入（含 category）
  filteredAnalytesByCat: [],    // 依類別分組後的篩選結果
  selectedAnalyteIds: [],
  /* chart */
  groups: [],                   // compare 回傳
  chartType: 'bar'
})

// ✅ 改成這樣分開
const collapsedGroups = ref([]) // 獨立的 ref，Vue 才能正確追蹤
const chartRefs = reactive({})
let resizeHandler = null

/* ----------------------- lifecycle ----------------------- */
onMounted(async () => {
  await Promise.all([loadSamples(), loadAnalytes()])
  window.addEventListener('resize', (resizeHandler = debounce(resizeAll, 160)))
})

onBeforeUnmount(() => {
  if (resizeHandler) window.removeEventListener('resize', resizeHandler)
  Object.values(chartRefs).forEach(el => el?.__chartInstance?.dispose?.())
})

/* ----------------------- load data ----------------------- */
async function loadSamples() {
  try {
    const res = await getNutritionList({ all: true })
    ui.allSamples = res.items || []
    ui.filteredSamples = ui.allSamples
  } catch (e) {
    console.error('載入食材失敗', e)
    ui.allSamples = ui.filteredSamples = []
  }
}

let firstLoad = true
async function loadAnalytes() {
  try {
    const res = await getAnalyteList(!state.showAllAnalytes ? true : false)
    const items = res?.items || []
    ui.analyteOptions = items

    // ✅ 初次載入：建群組 + 預設展開
    if (firstLoad) {
      groupAnalytes(items)
      expandDefaults()
      firstLoad = false
    } else {
      // ✅ 切換「全部/常見」時保留現有收合狀態
      const prevCollapsed = [...collapsedGroups.value]
      groupAnalytes(items)
      collapsedGroups.value = collapsedGroups.value.filter(cat => prevCollapsed.includes(cat))
    }

    await nextTick()
    filterAnalytes() // ✅ 即時根據搜尋文字重新顯示
  } catch (e) {
    console.error('載入營養素失敗', e)
  }
}

/* ----------------------- analyte 群組處理 ----------------------- */
function groupAnalytes(items) {
  const map = new Map()
  for (const a of items) {
    const cat = a.category || '未分類'
    if (!map.has(cat)) map.set(cat, [])
    map.get(cat).push({ analyteId: a.analyteId, analyteName: a.analyteName })
  }

  const newGroups = Array.from(map, ([category, items]) => ({ category, items }))
  ui.filteredAnalytesByCat = newGroups

  // ✅ 僅保留原有狀態，不重建 collapsedGroups
  collapsedGroups.value = collapsedGroups.value.filter(cat =>
    newGroups.some(g => g.category === cat)
  )
}

/* ----------------------- filters ----------------------- */
function filterSamples() {
  const kw = state.sampleKeyword.trim().toLowerCase()
  ui.filteredSamples = !kw
    ? ui.allSamples
    : ui.allSamples.filter(s => (s.sampleName || '').toLowerCase().includes(kw))
}

function filterAnalytes() {
  const kw = state.analyteKeyword.trim().toLowerCase()

  if (!kw) {
    // ✅ 若清空搜尋 → 顯示全部 analyte
    groupAnalytes(ui.analyteOptions)
    return
  }

  // ✅ 即時搜尋（可跨群組）
  const map = new Map()
  for (const a of ui.analyteOptions) {
    if ((a.analyteName || '').toLowerCase().includes(kw)) {
      const cat = a.category || '未分類'
      if (!map.has(cat)) map.set(cat, [])
      map.get(cat).push({ analyteId: a.analyteId, analyteName: a.analyteName })
    }
  }

  ui.filteredAnalytesByCat = Array.from(map, ([category, items]) => ({ category, items }))
  // ✅ 搜尋時保留收合狀態（避免閃爍）
}

function selectAllAnalytes() {
  const ids = []
  ui.filteredAnalytesByCat.forEach(g => g.items.forEach(a => ids.push(a.analyteId)))
  const set = new Set([...ui.selectedAnalyteIds, ...ids])
  ui.selectedAnalyteIds = Array.from(set)
}

/* ----------------------- sample pick ----------------------- */
function addSample(s) {
  if (ui.compareList.length >= 6) return showWarn('最多可比較 6 種食材')
  ui.compareList.push(s)
}
function removeSample(id) {
  ui.compareList = ui.compareList.filter(x => x.sampleId !== id)
}

/* ---------- 群組收合（改為陣列可追蹤版） ---------- */

function isGroupCollapsed(cat) {
  return collapsedGroups.value.includes(cat)
}

function toggleGroup(cat) {
  const idx = collapsedGroups.value.indexOf(cat)
  if (idx > -1) {
    collapsedGroups.value.splice(idx, 1)
  } else {
    collapsedGroups.value.push(cat)
  }

  // 🔹 強制 Vue 重新追蹤（避免 v-show 不更新）
  collapsedGroups.value = [...collapsedGroups.value]
}

function toggleAllGroups() {
  if (areAllGroupsCollapsed.value) {
    // ✅ 全部展開
    collapsedGroups.value = []
  } else {
    // ✅ 全部收合
    collapsedGroups.value = ui.filteredAnalytesByCat.map(g => g.category)
  }

  // 🔸 強制刷新防止立即又回彈
  collapsedGroups.value = [...collapsedGroups.value]
}

const areAllGroupsCollapsed = computed(() => {
  if (!ui.filteredAnalytesByCat.length) return false
  const allCats = ui.filteredAnalytesByCat.map(g => g.category)
  return allCats.every(cat => collapsedGroups.value.includes(cat))
})

function expandDefaults() {
  const defaults = ['一般成分', '礦物質', '維生素B群 & C', '維生素E']
  // ✅ 只設定為「非預設群組收合」
  collapsedGroups.value = ui.filteredAnalytesByCat
    .filter(g => !defaults.includes(g.category))
    .map(g => g.category)
}

/* ----------------------- compare ----------------------- */
async function fetchCompare() {
  if (ui.compareList.length < 2) return showWarn('請至少選 2 種食材')
  if (ui.selectedAnalyteIds.length < 1) return showWarn('請至少選 1 種營養素')
  if (ui.selectedAnalyteIds.length > 12) return showWarn('最多可選擇 12 種營養素')

  state.loading = true
  ui.groups = []
  try {
    const sampleIds = ui.compareList.map(x => x.sampleId).join(',')
    const analyteIds = ui.selectedAnalyteIds.join(',')
    const res = await getNutritionCompare(sampleIds, analyteIds)
    ui.groups = Array.isArray(res?.groups) ? res.groups : []
    if (!ui.groups.length) {
      alert('查無比較資料，請確認選擇的營養素與食材')
      return
    }
    await nextTick()
    renderAll()
  } catch (e) {
    console.error(e)
    showWarn('無法取得比較資料，請檢查 API')
  } finally {
    state.loading = false
  }
}

/* ----------------------- charts ----------------------- */
function renderAll() {
  // 清掉舊圖
  Object.values(chartRefs).forEach(el => el?.__chartInstance?.dispose?.())

  ui.groups.forEach((grp, gi) => {
    const el = chartRefs[gi]
    if (!el) return
    const chart = echarts.init(el)
    el.__chartInstance = chart

    const analytes = grp.analytes || []
    const analyteNames = analytes.map(a => a.analyteName)
    const sampleNames = analytes[0]?.values?.map(v => v.sampleName) || []

    // dataset[y(sample)][x(analyte)] = value
    const dataset = sampleNames.map(() => [])
    analytes.forEach((a, ai) => {
      a.values.forEach((v, si) => {
        dataset[si][ai] = toNum(v.value)
      })
    })

    let option
    switch (ui.chartType) {
      case 'bar': option = optionBar(analyteNames, sampleNames, dataset, grp.unit); break
      case 'radar': option = optionRadar(analyteNames, sampleNames, dataset); break
      case 'heatmap': option = optionHeatmap(analyteNames, sampleNames, dataset, grp.unit); break
      case 'stacked': option = optionStacked100(analyteNames, sampleNames, dataset, grp.unit); break
      case 'boxplot': option = optionBoxplot(analyteNames, dataset, grp.unit); break
    }

    chart.setOption(option)
  })
}

function resizeAll() {
  Object.values(chartRefs).forEach(el => el?.__chartInstance?.resize?.())
}

/* ----------------------- chart options ----------------------- */
function optionBar(analyteNames, sampleNames, dataset, unit) {
  const averages = analyteNames.map((_, i) =>
    dataset.reduce((sum, arr) => sum + (arr[i] || 0), 0) / Math.max(1, dataset.length)
  )
  return {
    tooltip: { trigger: 'axis' },
    legend: { data: sampleNames },
    grid: { top: 40, right: 16, bottom: 72, left: 56 },
    xAxis: { type: 'category', data: analyteNames },
    yAxis: { type: 'value', name: unit },
    series: [
      ...sampleNames.map((s, i) => ({
        name: s,
        type: 'bar',
        data: dataset[i],
        label: { show: true, position: 'top', fontSize: 12 }
      })),
      { name: '平均值', type: 'line', data: averages, lineStyle: { type: 'dashed' }, symbol: 'none' }
    ]
  }
}

function optionRadar(analyteNames, sampleNames, dataset) {
  const maxVal = Math.max(1, ...dataset.flat().map(n => Number(n) || 0)) * 1.2
  return {
    tooltip: {},
    legend: { data: sampleNames },
    radar: { indicator: analyteNames.map(n => ({ name: n, max: maxVal })) },
    series: [{ type: 'radar', data: sampleNames.map((s, i) => ({ name: s, value: dataset[i] })) }]
  }
}

function optionHeatmap(analyteNames, sampleNames, dataset, unit) {
  const data = []
  for (let y = 0; y < sampleNames.length; y++) {
    for (let x = 0; x < analyteNames.length; x++) {
      data.push([x, y, toNum(dataset[y][x])])
    }
  }
  return {
    tooltip: { position: 'top' },
    grid: { top: 40, right: 16, bottom: 72, left: 120 },
    xAxis: { type: 'category', data: analyteNames, splitArea: { show: true } },
    yAxis: { type: 'category', data: sampleNames, splitArea: { show: true } },
    visualMap: {
      min: 0,
      max: Math.max(1, ...data.map(d => d[2] || 0)),
      calculable: true,
      orient: 'horizontal',
      left: 'center',
      bottom: 10
    },
    series: [{ name: `含量(${unit})`, type: 'heatmap', data, label: { show: true } }]
  }
}

function optionStacked100(analyteNames, sampleNames, dataset, unit) {
  // 轉百分比
  const cols = analyteNames.length
  const rows = sampleNames.length
  const sums = Array(cols).fill(0)
  for (let c = 0; c < cols; c++) {
    for (let r = 0; r < rows; r++) sums[c] += toNum(dataset[r][c])
  }
  const percent = dataset.map(row =>
    row.map((v, c) => (sums[c] ? (toNum(v) / sums[c]) * 100 : 0))
  )

  return {
    tooltip: { trigger: 'axis', valueFormatter: v => `${v?.toFixed?.(1) ?? v}%` },
    legend: { data: sampleNames },
    grid: { top: 40, right: 16, bottom: 72, left: 56 },
    xAxis: { type: 'category', data: analyteNames },
    yAxis: { type: 'value', name: '%', max: 100, axisLabel: { formatter: '{value}%' } },
    series: sampleNames.map((s, i) => ({
      name: s,
      type: 'bar',
      stack: 'total',
      emphasis: { focus: 'series' },
      data: percent[i].map(v => Number.isFinite(v) ? Number(v.toFixed(2)) : 0)
    }))
  }
}

function optionBoxplot(analyteNames, dataset, unit) {
  // 對每個 analyte 計算五數：min, Q1, median, Q3, max
  const data = analyteNames.map((_, c) => {
    const col = dataset.map(row => toNum(row[c])).filter(n => Number.isFinite(n)).sort((a,b) => a-b)
    if (!col.length) return [0,0,0,0,0]
    const q1 = quantile(col, 0.25)
    const q2 = quantile(col, 0.5)
    const q3 = quantile(col, 0.75)
    return [col[0], q1, q2, q3, col[col.length - 1]]
  })

  return {
    tooltip: { trigger: 'item' },
    grid: { top: 40, right: 16, bottom: 72, left: 56 },
    xAxis: { type: 'category', data: analyteNames, boundaryGap: true, axisTick: { alignWithLabel: true } },
    yAxis: { type: 'value', name: unit, splitArea: { show: false } },
    series: [{ name: '分佈', type: 'boxplot', data }]
  }
}

/* ----------------------- utils ----------------------- */
function toNum(v) { const n = Number(v); return Number.isFinite(n) ? n : 0 }
function quantile(arr, p) {
  if (!arr.length) return 0
  const pos = (arr.length - 1) * p
  const base = Math.floor(pos)
  const rest = pos - base
  return arr[base + 1] !== undefined ? arr[base] + rest * (arr[base + 1] - arr[base]) : arr[base]
}
function debounce(fn, t = 200) {
  let tid; return (...args) => { clearTimeout(tid); tid = setTimeout(() => fn(...args), t) }
}

/* ----------------------- group tag styles ----------------------- */
function getGroupStyle(category, index) {
  // 固定幾個常見群組色，其他輪替柔色
  const map = {
    '一般成分': { bg: '#e9f6f6', border: '#007083', text: '#004b4b' },
    '礦物質':   { bg: '#e6f0fa', border: '#005bbb', text: '#0c2f6b' },
    '維生素B群 & C': { bg: '#fff4e5', border: '#f7931e', text: '#8a4b00' },
    '維生素E':  { bg: '#f8e9f6', border: '#b76ac4', text: '#6d2b7a' },
    '脂肪酸組成': { bg: '#f0f0f0', border: '#7a7a7a', text: '#3a3a3a' }
  }
  const palette = [
    { bg: '#e9f6f6', border: '#007083', text: '#004b4b' },
    { bg: '#e6f0fa', border: '#005bbb', text: '#0c2f6b' },
    { bg: '#fff4e5', border: '#f7931e', text: '#8a4b00' },
    { bg: '#f8e9f6', border: '#b76ac4', text: '#6d2b7a' },
    { bg: '#f0f0f0', border: '#7a7a7a', text: '#3a3a3a' }
  ]
  const c = map[category] || palette[index % palette.length]
  return { color: c.text, backgroundColor: c.bg, borderLeft: `6px solid ${c.border}` }
}
</script>

<style scoped>
.container { max-width: 1080px; }
/* 🌿 比較面板外框：略深、微陰影、hover時更清楚 */
.compare-step {
  border: 1px solid #b0d5d5;          /* 🔹 比原本 e9f6f6 稍深一階，框線更明顯 */
  border-radius: 10px;
  background-color: #ffffff;
  transition: all 0.25s ease-in-out;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.03);
}
.compare-step:hover {
  border-color: #b7dede;               /* 🔹 滑入時稍再深一階，增加層次感 */
  box-shadow: 0 3px 6px rgba(0, 0, 0, 0.06);
}
.border-main-color-green { border-color: rgb(0,112,131) !important; }
.chart-box { width: 100%; }

/* 群組標籤 */
.group-header { color: #004b4b; }
.group-caret { width: 1em; display: inline-block; }

/* analyte checkbox hover 效果 */
.analyte-item { transition: all 0.15s ease-in-out; }
.analyte-item:hover {
  background-color: #f2fbfb;
  box-shadow: 0 0 0 2px rgba(0,112,131,0.2);
}
/* 🔹 收合時滑順淡出 */
.fade-collapse-enter-active,
.fade-collapse-leave-active {
  transition: all 0.25s ease;
  overflow: hidden;
}
.fade-collapse-enter-from,
.fade-collapse-leave-to {
  opacity: 0;
  max-height: 0;
}
.fade-collapse-enter-to,
.fade-collapse-leave-from {
  opacity: 1;
  max-height: 500px; /* 足夠顯示整個群組 */
}

/* 🔹 小箭頭旋轉動畫 */
.bi.rotate-90 {
  transform: rotate(90deg);
  transition: transform 0.2s ease;
}
.bi.rotate-0 {
  transform: rotate(0deg);
  transition: transform 0.2s ease;
}
.silver-reflect-button {
  background: linear-gradient(180deg, #f8f8f8 0%, #e6e6e6 100%);
  border: 1px solid #bdbdbd;
  box-shadow: inset 0 1px 0 rgba(255,255,255,0.5), 0 1px 3px rgba(0,0,0,0.1);
  border-radius: 50px;
  transition: all 0.2s ease;
}
.silver-reflect-button:hover {
  background: linear-gradient(180deg, #ffffff 0%, #dcdcdc 100%);
  box-shadow: 0 0 8px rgba(180,180,180,0.6);
}

</style>
