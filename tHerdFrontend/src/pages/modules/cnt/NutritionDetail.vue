<template>
  <div class="page-wrap py-6">
    <!-- 🧾 Basic Info Card -->
    <el-card shadow="hover" class="info-card">
      <div class="flex flex-col gap-5">
        <!-- 標題列 -->
        <div class="flex flex-col md:flex-row md:items-start md:justify-between gap-4">
          <div class="flex items-start gap-3">
            <div class="title-bar"></div>
            <div>
              <h1 class="page-title m-0 main-color-green-text">
                {{ sample.sampleName }}
                <span v-if="sampleNameEnAuto" class="en-sub">（{{ sampleNameEnAuto }}）</span>
              </h1>

              <!-- 🏷 分類徽章（與搜尋保留呼吸間距） -->
              <div class="mt-3">
                <div class="category-badge">
                  <span class="badge-icon">🔰</span>
                  <span class="badge-text">{{ sample.categoryName }}</span>
                </div>
              </div>

              <!-- 🔍 搜尋營養素 -->
              <div class="mt-3 search-wrap">
                <span class="search-icon">🔍</span>
                <input
                  v-model="q"
                  type="search"
                  class="search-input"
                  placeholder="搜尋營養素（例：鈣、維生素B12、鐵）"
                />
              </div>

              <!-- 描述 -->
              <p class="mt-3 desc-text">
                {{ sample.description || '（無描述）' }}
              </p>
            </div>
          </div>
        </div>

        <!-- 次資訊列 -->
        <div class="sub-meta-row">
          <span class="sub-dot"></span>
          <span class="sub-meta">每 100 g 基準 · 已隱藏 0 值營養素 · 資料來自營養資料庫</span>
        </div>

        <!-- 🎛 工具列：排序 / Top5-10 / 匯出 -->
        <div class="tool-row">
          <div class="tool-left">
            <label class="tool-label main-color-white-text">排序：</label>
            <!-- 固定寬度 224px，避免中文被擠壓 -->
            <el-select v-model="sortOrder" class="w-56" placeholder="排序方式">
              <el-option label="原始順序" value="none" />
              <el-option label="由大到小" value="desc" />
              <el-option label="由小到大" value="asc" />
            </el-select>
          </div>

          <div class="tool-right">
            <!-- 📊 最小負擔 Switch（密合膠囊） -->
            <div class="topswitch" @click="top10 = !top10" role="button" aria-label="切換Top5/Top10">
              <span class="switch-icon">📊</span>
              <div :class="['switch-capsule', { on: top10 }]">
                <div class="knob"></div>
              </div>
              <span class="switch-label">{{ top10 ? 'Top10' : 'Top5' }}</span>
            </div>

            <!-- 匯出 PNG（內嵌中英標題/資訊） -->
            <el-button class="silver-reflect-button tool-btn" @click="exportPng" :loading="exporting">
              💾 匯出 PNG
            </el-button>
          </div>
        </div>
      </div>
    </el-card>

    <!-- 📊 Charts -->
    <el-card shadow="hover" class="mt-4 chart-card">
      <el-tabs v-model="activeChart" @tab-change="renderCharts">
        <el-tab-pane label="柱狀圖" name="bar">
          <div v-if="hasData" ref="barRef" class="chart-box"></div>
          <div v-else class="nodata">此項目無主要營養數據（0 值已隱藏）</div>
        </el-tab-pane>
        <el-tab-pane label="雷達圖" name="radar">
          <div v-if="hasData" ref="radarRef" class="chart-box"></div>
          <div v-else class="nodata">此項目無主要營養數據（0 值已隱藏）</div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 🗂 分類卡片（卡片樣式 + 搜尋過濾） -->
    <section class="mt-4">
      <h2 class="section-title main-color-green-text">營養成分（依分類）</h2>

      <div class="grid gap-4 md:grid-cols-2">
        <el-card
          v-for="(rows, cat) in filteredGrouped"
          :key="cat"
          shadow="never"
          class="group-card"
          body-class="group-card-body"
        >
          <header class="group-card-header">
            <div class="flex items-center gap-2">
              <span class="group-dot"></span>
              <span class="group-name">{{ cat }}</span>
            </div>
            <el-tag size="small" round type="info" effect="plain">{{ rows.length }} 項</el-tag>
          </header>

          <!-- 列表膠囊 -->
          <ul class="nutri-pill-list">
            <li v-for="row in rows" :key="row.name" class="nutri-pill">
              <div class="pill-left">
                <span class="pill-name">{{ row.name }}</span>
                <span class="pill-unit" v-if="row.unit">（{{ row.unit }}）</span>
              </div>
              <div class="pill-value">
                {{ displayVal(row.valuePer100g) }}
              </div>
            </li>
          </ul>
        </el-card>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, nextTick, watch, onBeforeUnmount } from 'vue'
import { useRoute } from 'vue-router'
import * as echarts from 'echarts'
import { getNutritionDetail } from './api/cntService'

const route = useRoute()
const id = Number(route.params.id)

// ===== State =====
const sample = ref({ sampleId: id, sampleName: '-', sampleNameEn: '', categoryName: '-', description: '-' })
const nutrients = ref([])

const sortOrder = ref('desc') // none/desc/asc
const activeChart = ref('bar')
const top10 = ref(true)       // true=Top10, false=Top5
const q = ref('')             // 搜尋
const exporting = ref(false)  // 匯出 loading

// ===== Helpers =====
const displayVal = (v) => (v === null || v === undefined ? '-' : v)

// 自動英文（無英文名則產生；這裡保守處理，有需要可接翻譯 API）
const sampleNameEnAuto = computed(() => {
  const en = (sample.value.sampleNameEn || '').trim()
  if (en) return en
  return 'Auto English'
})

// 依「隱藏 0 值」原則先過濾來源；再分群、排序
const grouped = computed(() => {
  const source = nutrients.value.filter(x => (x?.valuePer100g ?? null) > 0)

  const g = source.reduce((map, n) => {
    const key = n.category || '其他'
    if (!map[key]) map[key] = []
    map[key].push({ ...n })
    return map
  }, {})

  const sortFn = (a, b) => {
    if (sortOrder.value === 'asc')  return (a.valuePer100g ?? 0) - (b.valuePer100g ?? 0)
    if (sortOrder.value === 'desc') return (b.valuePer100g ?? 0) - (a.valuePer100g ?? 0)
    return 0 // none：維持原順序
  }
  Object.keys(g).forEach(k => g[k].sort(sortFn))
  return g
})

// 搜尋過濾（群組名稱/項目名/單位）
const filteredGrouped = computed(() => {
  const keyword = q.value.trim().toLowerCase()
  if (!keyword) return grouped.value
  const out = {}
  Object.entries(grouped.value).forEach(([cat, rows]) => {
    const m = rows.filter(r => (`${r.name} ${r.unit || ''} ${cat}`.toLowerCase()).includes(keyword))
    if (m.length) out[cat] = m
  })
  return out
})

/** 先挑 TopN（以值最大，排除 0）→ 再依 sortOrder 排序顯示 */
function selectTopThenSort(list, n) {
  const top = [...list]
    .filter(x => (x?.valuePer100g ?? 0) > 0)
    .sort((a, b) => (b.valuePer100g ?? 0) - (a.valuePer100g ?? 0))
    .slice(0, n)
  if (sortOrder.value === 'asc')  top.sort((a, b) => a.valuePer100g - b.valuePer100g)
  if (sortOrder.value === 'desc') top.sort((a, b) => b.valuePer100g - a.valuePer100g)
  return top
}

// 有無資料（避免空白圖表）
const hasData = computed(() => nutrients.value.some(n => (n?.valuePer100g ?? 0) > 0))

// ===== ECharts =====
const barRef = ref(null); let barChart = null
const radarRef = ref(null); let radarChart = null
let resizeObserver = null

function ceilNice(n) {
  if (!n || n <= 0) return 1
  const p = Math.pow(10, Math.floor(Math.log10(n)))
  const m = n / p
  const k = m <= 1 ? 1 : m <= 2 ? 2 : m <= 5 ? 5 : 10
  return k * p
}

async function renderCharts() {
  await nextTick()

  // 若沒有資料，銷毀舊圖並直接返回
  if (!hasData.value) {
    if (barChart) { barChart.dispose(); barChart = null }
    if (radarChart) { radarChart.dispose(); radarChart = null }
    return
  }

  if (barChart) barChart.dispose()
  if (radarChart) radarChart.dispose()

  const N = top10.value ? 10 : 5
  const topData = selectTopThenSort(nutrients.value, N)

  // 主要單位（取 TopN 內出現最多的單位）
  const unitCounter = topData.reduce((m, x) => {
    const u = (x.unit || '').trim()
    if (!u) return m
    m[u] = (m[u] || 0) + 1
    return m
  }, {})
  const mainUnit = Object.entries(unitCounter).sort((a,b)=>b[1]-a[1])[0]?.[0] || ''

  // ---- Bar ----
  if (barRef.value) {
    barChart = echarts.init(barRef.value)
    barChart.setOption({
      tooltip: {
        trigger: 'axis',
        valueFormatter: v => v,
        className: 'nutri-tooltip'
      },
      // 根據排序調整 grid，避免左側文字擠壓
      grid: sortOrder.value === 'none'
        ? { left: 16, right: 24, top: 18, bottom: 46, containLabel: true }
        : { left: 12, right: 24, top: 18, bottom: 46, containLabel: true },
      xAxis: { type: 'value' },
      yAxis: {
        type: 'category',
        axisLabel: { width: 340, overflow: 'truncate', interval: 0 },
        data: topData.map(x => x.name + (x.unit ? `（${x.unit}）` : ''))
      },
      series: [{
        type: 'bar',
        barMaxWidth: 20,
        barCategoryGap: '26%',
        itemStyle: { borderRadius: [6, 6, 6, 6] },
        label: {
          show: true,
          position: 'right',
          formatter: p => `${p.data.value} ${p.data.unit || ''}`.trim()
        },
        data: topData.map(x => ({ value: x.valuePer100g, unit: x.unit }))
      }]
    })
  }

  // ---- Radar ----
  if (radarRef.value) {
    const vmax = ceilNice(Math.max(...topData.map(y => y.valuePer100g)) || 1)
    radarChart = echarts.init(radarRef.value)
    radarChart.setOption({
      tooltip: {},
      radar: {
        radius: '62%',
        splitNumber: 5,
        axisName: { color: '#444' },
        indicator: topData.map(x => ({ name: x.name, max: vmax }))
      },
      series: [{
        type: 'radar',
        areaStyle: { opacity: 0.16 },
        lineStyle: { width: 2 },
        symbol: 'circle',
        symbolSize: 3,
        data: [{ value: topData.map(x => x.valuePer100g), name: `Per 100g (Top${N})${mainUnit ? ' · ' + mainUnit : ''}` }]
      }]
    })
  }

  // Resize 綁定
  if (!resizeObserver) {
    resizeObserver = new ResizeObserver(() => {
      barChart && barChart.resize()
      radarChart && radarChart.resize()
    })
    barRef.value && resizeObserver.observe(barRef.value)
    radarRef.value && resizeObserver.observe(radarRef.value)
  }
}

/** 匯出 PNG（避免空白）：先 nextTick 確保渲染完成，再臨時加 Title，最後還原 */
async function exportPng() {
  const inst = activeChart.value === 'bar' ? barChart : radarChart
  if (!inst) return
  exporting.value = true
  await nextTick()

  const N = top10.value ? 10 : 5
  const cname = sample.value.sampleName || ''
  const ename = sampleNameEnAuto.value || ''
  const title = ename ? `${cname}（${ename}）` : cname

  // 副標：雙語（C2）
  const kindZh = activeChart.value === 'bar' ? `每100克主要營養成分圖表 (Top${N})` : `每100克主要營養成分圖表 (Top${N})`
  const kindEn = activeChart.value === 'bar' ? `Per 100g Nutrition Bar (Top${N})` : `Per 100g Nutrition Radar (Top${N})`

  // 從當前圖表推斷主單位（柱狀圖較精準）
  const opt = inst.getOption()
  let mainUnit = ''
  try {
    if (activeChart.value === 'bar') {
      const data = (opt?.series?.[0]?.data ?? []).map(d => (typeof d === 'object' ? d.unit : ''))
      const cnt = {}
      data.forEach(u => { if (u) cnt[u] = (cnt[u] || 0) + 1 })
      mainUnit = Object.entries(cnt).sort((a,b)=>b[1]-a[1])[0]?.[0] || ''
    }
  } catch {}

  const sub = `${kindZh} | ${kindEn}${mainUnit ? ` · ${mainUnit}` : ''} | 資料來源｜Source: tHerd Nutrition DB`

  // 暫存舊的 title 設定，避免還原出錯
  const prevTitle = opt.title ? JSON.parse(JSON.stringify(opt.title)) : null

  // 套上臨時 Title 後截圖
  inst.setOption({
    title: {
      left: 'center', top: 6,
      text: title,
      subtext: sub,
      textStyle: { fontSize: 14, fontWeight: 700, color: '#1f2937' },
      subtextStyle: { fontSize: 12, color: '#6b7280' }
    }
  })

  // 等待 ECharts layout 完成，避免空白
  await new Promise(r => setTimeout(r, 50))
  const url = inst.getDataURL({ type: 'png', pixelRatio: 2, backgroundColor: '#ffffff' })

  // 還原 Title
  if (prevTitle) {
    inst.setOption({ title: prevTitle })
  } else {
    inst.setOption({ title: { show: false } })
  }

  // 下載
  const a = document.createElement('a')
  a.href = url
  a.download = `${cname}_${activeChart.value}_Top${N}_Per100g${mainUnit ? '_' + mainUnit : ''}.png`
  document.body.appendChild(a); a.click(); a.remove()
  exporting.value = false
}

async function fetchDetail() {
  const { sample: s, nutrients: n } = await getNutritionDetail(id)
  sample.value = s || sample.value

  // 原始資料 → 標準化；保留 0 以便後續篩掉
  nutrients.value = (n || []).map(x => ({
    category: x.category || '其他',
    name: x.name,
    unit: x.unit || '',
    valuePer100g: x.valuePer100g === 0 ? 0 : (x.valuePer100g ?? null)
  }))

  await renderCharts()
}

onMounted(fetchDetail)
watch([sortOrder, top10], () => nextTick(renderCharts))
watch(q, () => nextTick(() => { /* 搜尋只影響下方列表；可視需求同步重繪 */ }))

onBeforeUnmount(() => {
  if (resizeObserver) {
    barRef.value && resizeObserver.unobserve(barRef.value)
    radarRef.value && resizeObserver.unobserve(radarRef.value)
    resizeObserver.disconnect()
    resizeObserver = null
  }
  barChart && barChart.dispose()
  radarChart && radarChart.dispose()
})
</script>

<style scoped>
/* ===== Layout ===== */
.page-wrap { max-width: 1120px; margin: 0 auto; padding: 0 1rem; }
.py-6 { padding-top: 1.5rem; padding-bottom: 1.5rem; }

/* ===== Info card ===== */
.info-card { border-radius: 18px; }
.title-bar { width: 6px; height: 40px; border-radius: 6px; background: rgb(0,112,131); margin-top: 2px; }
.page-title { font-size: 1.6rem; font-weight: 800; letter-spacing: .5px; }
.en-sub { font-weight: 600; color: #5b8290; margin-left: 4px; }
.desc-text { color: #374151; line-height: 1.6; }

/* 分類徽章（顯眼） */
.category-badge {
  display: inline-flex; align-items: center; gap: .5rem;
  padding: .35rem .75rem; border-radius: 999px;
  background: rgba(0,112,131,.12); border: 1px solid rgba(0,112,131,.3);
}
.badge-icon { font-size: .95rem; }
.badge-text { font-weight: 700; color: rgb(0,112,131); }

/* 搜尋欄（與分類拉開距離） */
.search-wrap { position: relative; width: min(520px, 100%); }
.search-icon { position: absolute; left: 10px; top: 50%; transform: translateY(-50%); }
.search-input {
  width: 100%; padding: .6rem .9rem .6rem 2rem; border-radius: 999px;
  border: 1px solid #e5e7eb; background: #fff;
}

/* 次資訊列 */
.sub-meta-row { display: flex; align-items: center; gap: .5rem; padding-left: .5rem; }
.sub-dot { width: 8px; height: 8px; border-radius: 999px; background: rgb(0,112,131); }
.sub-meta { color: #6b7280; font-size: .925rem; }

/* ===== Tools ===== */
.tool-row { display: flex; align-items: center; justify-content: space-between; gap: 1rem; margin-top: .5rem; }
.tool-left { display: flex; align-items: center; gap: .6rem; }
.tool-right { display: flex; align-items: center; gap: .6rem; }
.tool-label { font-weight: 700; }
/* 固定寬度 224px，避免中文被擠壓 */
.w-56 { width: 224px; }
.tool-btn { border-radius: 999px; }

/* 📊 最小負擔 Switch（膠囊完全密合） */
.topswitch { display: inline-flex; align-items: center; gap: .45rem; cursor: pointer; user-select: none; }
.switch-icon { font-size: .95rem; opacity: .85; }
.switch-capsule {
  width: 54px; height: 28px; border-radius: 999px; position: relative;
  background: #d1d5db; transition: background .18s ease, border-color .18s ease;
  border: 1px solid #cbd5e1;
  box-shadow: inset 0 1px 2px rgba(0,0,0,.06);
}
.switch-capsule.on { background: rgb(0,112,131); border-color: rgb(0,112,131); }
.knob {
  position: absolute; top: 1px; left: 1px; width: 26px; height: 26px; border-radius: 999px;
  background: #fff; transition: left .18s ease;
  box-shadow: 0 1px 2px rgba(0,0,0,.18);
}
.switch-capsule.on .knob { left: 27px; }
.switch-label { font-size: .92rem; color: #475569; }

/* ===== Charts ===== */
.chart-card { border-radius: 18px; }
.chart-box { width: 100%; height: 430px; }
.nodata {
  height: 430px; display: grid; place-items: center;
  color: #6b7280; border: 1px dashed #e5e7eb; border-radius: 12px;
}

/* 自訂 tooltip 字型可讀（可選） */
:global(.nutri-tooltip) { font-size: 12px; }

/* ===== Section ===== */
.section-title { font-size: 1.15rem; font-weight: 700; margin-bottom: .75rem; }

/* ===== Group Cards (B款) ===== */
.group-card { border: 1px solid #e9ecef; border-radius: 16px; background: #fff; }
.group-card-body { padding: 12px 14px 6px 14px; }
.group-card-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 4px 2px 10px 2px; border-bottom: 1px dashed #e9ecef;
}
.group-dot { width: 10px; height: 10px; border-radius: 50%; background: rgb(0,112,131); }
.group-name { font-weight: 700; color: #334155; }

/* 膠囊列 */
.nutri-pill-list { list-style: none; padding: 10px 0 2px 0; margin: 0; display: flex; flex-direction: column; gap: 8px; }
.nutri-pill {
  display: flex; align-items: center; justify-content: space-between;
  gap: 12px; padding: 10px 12px;
  background: #f8f9fa; border-radius: 999px;
  border: 1px solid #eef2f6;
}
.pill-left { display: flex; align-items: baseline; gap: 6px; overflow: hidden; }
.pill-name { font-weight: 600; color: #111827; white-space: nowrap; text-overflow: ellipsis; overflow: hidden; max-width: 56ch; }
.pill-unit { color: #64748b; font-size: .92rem; }
.pill-value { min-width: 88px; text-align: right; font-variant-numeric: tabular-nums; font-weight: 700; color: rgb(0,112,131); }

/* ===== Theme notes =====
  使用你的 main.css 主題 class：
  .main-color-green-text / .main-color-white-text / .teal-reflect-button / .silver-reflect-button ...
  色票來源：你的 PPT 主題
*/
</style>
