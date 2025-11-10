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
        <button
          class="btn silver-reflect-button px-3 py-1 rounded-pill ms-2 btn-sm"
          @click="clearSamples"
        >
          清空
        </button>
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
          <button
            type="button"
            class="btn silver-reflect-button btn-sm text-dark px-3"
            @click.stop.prevent="toggleAllGroups"
          >
            {{ allCollapsed ? '全部展開' : '全部收合' }}
          </button>
        </div>
      </div>

      <!-- 快搜 + 全選/清空 -->
      <!-- <div class="row g-2 align-items-center mb-2">
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
      </div> -->
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
          <!-- 👇 DEMO 一鍵帶入：放在最左邊 -->
          <button
            class="btn teal-reflect-button btn-sm text-white px-3 me-2"
            @click="applyDemo"
          >
            DEMO 一鍵帶入
          </button>

          <button
            class="btn silver-reflect-button btn-sm text-dark px-3 me-2"
            @click="selectAllAnalytes"
          >
            全選目前篩選
          </button>
          <button
            class="btn silver-reflect-button btn-sm text-dark px-3"
            @click="ui.selectedAnalyteIds = []"
          >
            清空
          </button>
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
            <!-- 由這層 v-show 控制顯示；不要放任何 d-* 類別 -->
            <div v-show="!isGroupCollapsed(group.category)" class="mt-2">
              <!-- 這層才放 d-flex 等排版類別 -->
              <div class="analyte-group-content d-flex flex-wrap gap-2">
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
                    :disabled="isPMS(a)"            
                    :title="isPMS(a) ? '此欄為彙總指標，無法比較' : ''"  
                  />
                  <!-- :title="isPMS(a) ? '此欄為彙總指標，無法比較' : ''" =>小提示，可留可拔 -->
                   <!-- :disabled="isPMS(a)"  =>只有 PMS 不能勾 -->
                  {{ a.analyteName }}
                </label>
              </div>
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
      <div class="d-flex justify-content-end gap-2 mb-3">
        <button class="btn teal-reflect-button text-white btn-sm" @click="exportPng">
          💾 匯出圖表（PNG）
        </button>
        <button class="btn silver-reflect-button btn-sm" @click="exportCsv">
          📑 匯出數據（CSV）
        </button>
      </div>
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
import { reactive, ref, computed, watch, onMounted, onBeforeUnmount, nextTick } from 'vue'
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
function showSuccess(msg) {
  Swal.fire({
    text: msg,
    icon: 'success',
    timer: 1400,
    showConfirmButton: false
  })
}
// 全程正規化分類字串。加入這個 helper
const norm = s => {
  const t = String(s || '').trim()
  return t === '' ? '未分類' : t
}
/* ---------- 名稱解析與寬度估算 helper ---------- */
// 取中文顯示名 + 英文尾註（允許括號裡再有括號）
function parseZhEn(name) {
  const s = String(name || '').trim();
  const open = s.lastIndexOf('(');
  const close = s.endsWith(')');
  if (open > -1 && close) {
    const zhPart = s.slice(0, open).trim();
    const tail = s.slice(open + 1, -1).trim(); // 括號內完整字串，可含 (Chinese) 之類
    const hasCJK = /[\u4e00-\u9fff]/.test(tail);
    const hasLat = /[A-Za-z]/.test(tail);
    if (hasLat && !hasCJK) {
      return { zh: zhPart || s, en: tail };
    }
  }
  return { zh: s, en: '' };
}

// 用 canvas 實測字寬，避免估太寬把圖擠到右邊
const measureTextWidth = (() => {
  const canvas = document.createElement('canvas');
  const ctx = canvas.getContext('2d');
  // 依你頁面實際字體微調；12px 是 ECharts 預設刻度字
  ctx.font = '12px "Noto Sans TC", "Microsoft JhengHei", system-ui, -apple-system, "Segoe UI", Roboto, Arial, sans-serif';
  return (text) => ctx.measureText(String(text)).width;
})();

// 依最長的 Y 標籤算出左邊距
function leftPadForY(labels, cw) {
  const maxW = Math.max(0, ...labels.map(measureTextWidth));
  const estimated = Math.ceil(maxW) + 16;       // 文字 + 內距
  const cap = Math.floor(cw * 0.34);            // 左邊距最多占容器 34%（更嚴格）
  return Math.max(64, Math.min(estimated, cap)); 
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
// 🛡️ 批次切換期間的全域旗標（放最上面，任何函式執行時都能讀到正確值）
let isBulkToggling = false
const allCollapsed = ref(false)   // ← 新增：全局唯一「全部收合」狀態
let togglingNow = false

/* ---------- 單位正規化 ---------- */
// 單位正規化與顯示
const normUnit = (u) =>
  String(u || '').trim().toLowerCase().replace('μg', 'µg').replace('mcg', 'µg')
const displayUnit = (u) => {
  const k = normUnit(u)
  if (!k) return '-'         // 沒單位時顯示「-」
  if (k === 'kcal') return 'kcal'
  if (k === 'g')    return 'g'
  if (k === 'mg')   return 'mg'
  if (k === 'µg')   return 'µg'
  return k                   // 其他單位原樣顯示
}

// 數字格式：kcal 或 >=100 取 0 位；>=10 取 1 位；其他 2 位
const fmtNumber = (n, unit) => {
  const v = Number(n ?? 0)
  const abs = Math.abs(v)
  const k = normUnit(unit)
  const digits = (k === 'kcal' || abs >= 100) ? 0 : (abs >= 10 ? 1 : 2)
  return v.toLocaleString(undefined, {
    minimumFractionDigits: digits,
    maximumFractionDigits: digits
  })
}

// 判斷名稱是否就是「P/M/S」（容忍空白/大小寫）
  const isPMS = (a) => {
    const name = String(a?.analyteName || a?.name || '').trim().toUpperCase()
    return name === 'P/M/S'
  }

  // ⭐ DEMO 用預設選項（名字請換成你實際資料庫的名稱）
// 這些是「已選食材」那一排要出現的
const DEMO_SAMPLE_NAMES = [
  '三節翅(土雞) (Chicken: whole wings, wild; Chicken: three joint wings, wild)',
  '三節翅(肉雞) (Chicken: whole wings, feed; Chicken: three joint wings, feed)',
  '三節翅平均值',
  '中脂調味乳(多穀類) (Reduced fat flavored composite and recombined milk: cereal flavor)',
  '中脂調味乳(巧克力) (Reduced fat flavored composite and recombined milk: chocolate flavor)',
  '中脂調味乳(果汁) (Low-fat flavored composite and recombined milk: low fat, fruit flavor)',
  // ...看你要幾個，最多 6 個
]

// 這些是「要勾選的營養素」按鈕
const DEMO_ANALYTE_NAMES = [
  // 一般成分
  '水分',
  '熱量',
  '粗脂肪',
  '粗蛋白',
  '總碳水化合物',
  // 礦物質
  '磷',
  '鈉',
  '鉀',
  '鈣',
  '鐵',
  '鋅',
  '鎂',
]

// 如果之前已選過 P/M/S，自動剔除
  watch(() => ui.selectedAnalyteIds.slice(), (ids) => {
    const getAnalyteById = (id) => {
      for (const g of ui.filteredAnalytesByCat || []) {
        const hit = g.items?.find(x => x.analyteId === id)
        if (hit) return hit
      }
      return null
    }
    const cleaned = ids.filter(id => !isPMS(getAnalyteById(id)))
    if (cleaned.length !== ids.length) ui.selectedAnalyteIds = cleaned
  })

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
  console.log('[LOAD] showAll=', state.showAllAnalytes, 'bulk=', isBulkToggling)
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
    if (state.analyteKeyword.trim() !== '') {
      filterAnalytes() // 只有真的在搜尋時才觸發
    }
    console.log('[LOAD] done. groups=', ui.filteredAnalytesByCat.length)
  } catch (e) {
    console.error('載入營養素失敗', e)
  }
}
// DEMO用
/* ---------- DEMO 一鍵帶入 ---------- */
async function applyDemo() {
  if (!ui.allSamples.length || !ui.analyteOptions.length) {
    // 保險一點，確保資料都有載好
    await Promise.all([loadSamples(), loadAnalytes()])
  }

  // 1) 食材：從全部食材中找出 DEMO 想用的那幾個
  ui.compareList = ui.allSamples.filter(s =>
    DEMO_SAMPLE_NAMES.includes(s.sampleName)
  ).slice(0, 6) // 最多 6 種

  // 2) 營養素：從全部營養素中找出 DEMO 想勾的那些
  ui.selectedAnalyteIds = ui.analyteOptions
    .filter(a => DEMO_ANALYTE_NAMES.includes(a.analyteName))
    .map(a => a.analyteId)

  // 3) 清掉關鍵字，讓群組恢復正常顯示
  state.analyteKeyword = ''
  filterAnalytes()

  // 4) 展開預設群組，確保「一般成分 / 礦物質」有打開
  ui.collapsedGroups.clear()
  expandDefaults()

  // 5) 直接幫你按「開始比較」（如果你想要需要再按一次，就把這行註解掉）
  fetchCompare()
}

/* ----------------------- analyte 群組處理 ----------------------- */
// 3) groupAnalytes：保護期後依 allCollapsed 同步 collapsedGroups
function groupAnalytes(items) {
  const map = new Map()
  for (const a of items) {
    const cat = norm(a.category) || '未分類'
    if (!map.has(cat)) map.set(cat, [])
    map.get(cat).push({ analyteId: a.analyteId, analyteName: a.analyteName })
  }

  const newGroups = Array.from(map, ([category, items]) => ({ category, items }))
  ui.filteredAnalytesByCat = newGroups

  // 批次切換期間：完全不要動 collapsedGroups（避免「收了又展開」）
  if (isBulkToggling) return

  if (allCollapsed.value) {
    // 目前是「全部收合」模式 → 重建後維持全部收合
    collapsedGroups.value = newGroups.map(g => norm(g.category))
  } else {
    // 一般模式 → 僅清理不存在的分類
  collapsedGroups.value = collapsedGroups.value.filter(cat =>
    newGroups.some(g => norm(g.category) === cat))
  }
  console.log('[GROUP] exit:  collapsed=', collapsedGroups.value.length)
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
  console.log('[FILTER] kw=', kw, 'bulk=', isBulkToggling)

  if (!kw) {
    // ✅ 若清空搜尋 → 顯示全部 analyte
    // 🛡️ 批次切換期間不要重建，避免洗掉剛設定的 collapsedGroups
    if (!isBulkToggling) {
      groupAnalytes(ui.analyteOptions)
    }
    return
  }

  // ✅ 即時搜尋（可跨群組）
  const map = new Map()
  for (const a of ui.analyteOptions) {
    if ((a.analyteName || '').toLowerCase().includes(kw)) {
      const cat = norm(a.category) || '未分類'
      if (!map.has(cat)) map.set(cat, [])
      map.get(cat).push({ analyteId: a.analyteId, analyteName: a.analyteName })
    }
  }

  ui.filteredAnalytesByCat = Array.from(map, ([category, items]) => ({ category, items }))
  // ✅ 搜尋時保留收合狀態（避免閃爍）
}

function selectAllAnalytes() {
  const ids = []
  ui.filteredAnalytesByCat.forEach(g =>
    g.items.forEach(a => { if (!isPMS(a)) ids.push(a.analyteId) })  // ✅ 過濾 P/M/S
  )
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

function clearSamples() {
  ui.compareList = []   // ← 清空已選食材
}

/* ---------- 群組收合（改為陣列可追蹤版） ---------- */

function isGroupCollapsed(cat) {
  return collapsedGroups.value.includes(norm(cat))
}

function toggleGroup(cat) {
  const c = norm(cat)
  const i = collapsedGroups.value.indexOf(c)
  if (i > -1) collapsedGroups.value.splice(i, 1)
  else collapsedGroups.value.push(c)
  // 🔹 強制 Vue 重新追蹤（避免 v-show 不更新）
  collapsedGroups.value = [...collapsedGroups.value]
}

// 2) 改寫 toggleAllGroups（改用 allCollapsed 當唯一事實來源）
async function toggleAllGroups() {
  if (togglingNow) return         // 🛡️ 防連點
  togglingNow = true
  try {
    isBulkToggling = true
    allCollapsed.value = !allCollapsed.value

    collapsedGroups.value = allCollapsed.value
      ? ui.filteredAnalytesByCat.map(g => norm(g.category)) // 全收
      : []                                                  // 全展

    collapsedGroups.value = [...collapsedGroups.value]
    await nextTick()
    await Promise.resolve() // 再等一拍避開同輪 regroup
  } finally {
    isBulkToggling = false
    togglingNow = false      // ✅ 關鍵：把點擊鎖放開
    console.log('[CLICK] after:', 'allCollapsed=', allCollapsed.value,
                'collapsed=', collapsedGroups.value.length)
  }
}

function expandDefaults() {
  const defaults = ['一般成分', '礦物質', '維生素B群 & C', '維生素E']
  // ✅ 只設定為「非預設群組收合」
  collapsedGroups.value = ui.filteredAnalytesByCat
    .filter(g => !defaults.includes(norm(g.category)))
    .map(g => norm(g.category))
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
// 加個小工具把食材陣列切塊
function wrapSamples(names, per = 3, sep = '、') {
  const rows = []
  for (let i = 0; i < names.length; i += per) {
    rows.push(names.slice(i, i + per).join(sep))
  }
  return rows.join('\n')  // ← 每列之間用 \n
}

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
    const rawSampleNames = analytes[0]?.values?.map(v => v.sampleName) || [];
    const parsed = rawSampleNames.map(parseZhEn);
    const zhNames = parsed.map(p => p.zh);   // 給圖例/系列/座標軸顯示
    const enTails = parsed.map(p => p.en);   // 只給 tooltip 括號顯示

    // dataset[y(sample)][x(analyte)] = value
    const dataset = zhNames.map(() => [])
    analytes.forEach((a, ai) => a.values.forEach((v, si) => { dataset[si][ai] = toNum(v.value) }))
   // ✅ 先算 names/rows，後面雷達圖要用
   const names       = zhNames
   const namesPerRow = 3
   const rows        = Math.ceil(names.length / namesPerRow)
   // 在 renderAll() 內、算出 rows 之後、決定 option 之前
   const extraTopByType = { bar: 0, stacked: 0, heatmap: 8, radar: 12, boxplot: 0 };
   const extraTop = extraTopByType[ui.chartType] || 0;


    let option
    switch (ui.chartType) {
     case 'bar':     option = optionBar(analyteNames, zhNames, dataset, grp.unit); break
     case 'radar':   option = optionRadar(analyteNames, zhNames, dataset, grp.unit, { rows, enTails }); break
     case 'heatmap': option = optionHeatmap(analyteNames, zhNames, dataset, grp.unit, (el?.clientWidth || 800)); break
     case 'stacked': option = optionStacked100(analyteNames, zhNames, dataset, grp.unit); break
     case 'boxplot': option = optionBoxplot(analyteNames, dataset, grp.unit); break
    }

    // === 標題/副標與版面 ===
    // 1) 顯示用「原始單位」
    const rawUnit  = (grp.unit ?? '').trim();
    const unitKey  = normUnit(rawUnit);
    const showUnit = rawUnit || '-';

    // 2) 食材名稱 → 多列換行（每列3個，可調 4/5）
    const namesMultiline  = wrapSamples(names, namesPerRow)  // <-- 真的用上它
    const titleText       = '食材比較'                        // 主標題就放簡短字
    const subZh           = `依單位分群（每100公克）· 單位：${showUnit}`
    const subEn           = `Per 100g · Unit: ${showUnit}`
    // 把多行食材清單放在副標的第一行
    // 組成多段文字（rich style）
    const subText = [
      `{foods|${namesMultiline}}`,
      `{info|${subZh} | ${subEn}}`,
      `{src|資料來源｜Source: tHerd Nutrition DB}`
    ].join('\n')
    
    // 3) 依食材列數拉開上邊距（避免壓到圖）
    const baseGrid = option.grid && !Array.isArray(option.grid) ? option.grid : {}
    option.grid = {
      ...baseGrid,
      top: Math.max(baseGrid.top ?? 0, 148 + (rows - 1) * 24 + extraTop),
      left:   Math.max(baseGrid.left   ?? 0, 64),
      right:  Math.max(baseGrid.right  ?? 0, 28),
      bottom: Math.max(baseGrid.bottom ?? 0, 108),
      containLabel: true
    }

    option.title = {
        left: 'center',
        top: 10,
        text: titleText,
        subtext: subText,
        subtextGap: 16,
        textStyle: {
          fontSize: 18, fontWeight: 500, color: '#1f2937'// 主標題「食材比較」深灰黑
        },
        subtextStyle: {
          rich: {
            // 🔹 第一行：食材清單（主視覺焦點）→ 灰黑、略粗
            foods: {
              fontSize: 15, lineHeight: 24, fontWeight: 600, color: '#374151'},
                        // ≈ Tailwind slate-700            
            // 🔹 第二行：單位資訊 → 中灰、略細一點
            info: {
              fontSize: 14, lineHeight: 22, fontWeight: 600, color: '#6b7280'},
                        // ≈ slate-500
            // 🔹 第三行：資料來源 → 比上面再淺一階，但不會太淡
            src: {
              fontSize: 13, lineHeight: 20, fontWeight: 600, color: '#4b5563'}
                        // ≈ slate-600，比 #9ca3af 深一點更穩重
          }
        }
      }


    // 4) 圖例：維持你原本單列/自動換寬的寫法（可保留或之後換成多列版本）
    const cw = el?.clientWidth || 800
    option.legend = {
      ...(option.legend || {}),
      type: 'plain',
      orient: 'horizontal',
      left: 'center',
      bottom: 2,
      width: Math.max(320, cw - 160),
      itemGap: 16,
      itemWidth: 10,
      itemHeight: 10,
      textStyle: { fontSize: 12, lineHeight: 16 }
    }
    if (zhNames.length > 14) {
      option.legend.type = 'scroll'
      option.legend.pageIconSize = 10
      option.legend.pageButtonItemGap = 6
      option.legend.pageFormatter = '{current}/{total}'
    }


    // 3) 軸線：只對 value 軸做數字格式（用 unitKey），類別軸不處理
    const isXValue = option.xAxis && option.xAxis.type === 'value'
    const isYValue = option.yAxis && option.yAxis.type === 'value'
    if (option.xAxis) option.xAxis = { ...option.xAxis, name: '' }
    if (option.yAxis) option.yAxis = { ...option.yAxis, name: '' }
    if (isXValue) {
      option.xAxis = {
        ...option.xAxis,
        axisLabel: { ...(option.xAxis.axisLabel || {}), formatter: v => fmtNumber(v, unitKey) }
      }
    }
    if (isYValue) {
      option.yAxis = {
        ...option.yAxis,
        axisLabel: { ...(option.yAxis.axisLabel || {}), formatter: v => fmtNumber(v, unitKey) }
      }
    }

    // 4) Tooltip / 資料標籤：數字用 unitKey，尾巴單位顯示 rawUnit（showUnit）
    // 條狀/堆疊：以「營養素 → 各食材：數字」的樣式
    if (ui.chartType === 'bar' || ui.chartType === 'stacked') {
      option.tooltip = {
        trigger: 'axis',
        confine: true,
        formatter: (params) => {
          const analyte = params?.[0]?.axisValueLabel ?? params?.[0]?.axisValue ?? ''
          let html = `<div style="margin-bottom:4px;"><strong>${analyte}</strong></div>`
          for (const p of params) {
            if (p.seriesName === '平均值') {
              html += `<div>${p.marker} 平均值：<b>${fmtNumber(p.value, unitKey)} ${showUnit}</b></div>`
            } else {
              const idx = zhNames.indexOf(p.seriesName)
              const en  = enTails[idx] ? `（${enTails[idx]}）` : ''
              html += `<div>${p.marker} ${p.seriesName}${en}：<b>${fmtNumber(p.value, unitKey)} ${showUnit}</b></div>`
            }
          }
          return html
        }
      }
    }
    else if (ui.chartType === 'heatmap') {
      option.tooltip = {
        position: 'top',
        confine: true,
        formatter: (p) => {
          const aIdx = p.data[0], sIdx = p.data[1]
          const analyte = analyteNames[aIdx]
          const en = enTails[sIdx] ? `（${enTails[sIdx]}）` : ''
          return `<div style="margin-bottom:4px;"><strong>${analyte}</strong></div>
                  <div>${p.marker} ${zhNames[sIdx]}${en}：<b>${fmtNumber(p.data[2], unitKey)} ${showUnit}</b></div>`
        }
      }
    }
    // 雷達圖在 optionRadar 自帶客製 formatter（已用 enTails 了）
    else if (ui.chartType !== 'radar') {
      option.tooltip = {
        ...(option.tooltip || {}),
        trigger: option.tooltip?.trigger || 'axis',
        valueFormatter: v => `${fmtNumber(v, unitKey)} ${showUnit}`.trim()
      }
    } // radar 維持 optionRadar 內建的 trigger: 'item'

    if (Array.isArray(option.series) && option.series.length) {
      const isHorizontal = option.yAxis && option.yAxis.type === 'category'
      option.series = option.series.map(s => {
        if (s.type !== 'bar') return s

        // ✅ 如果是堆疊百分比圖，強制顯示為百分比
        const isPercent = ui.chartType === 'stacked'

        return {
          ...s,
          barMaxWidth: 26,
          label: {
            ...(s.label || {}),
            show: true,
            position: isHorizontal ? 'right' : 'top',
            formatter: p =>
              isPercent
                ? `${p.value?.toFixed?.(1) ?? p.value}%`
                : `${fmtNumber(p.value, unitKey)} ${showUnit}`.trim()
          }
        }
      })
    }
    // 讓不同視圖做一點小微調
    if (ui.chartType === 'boxplot') {
      option.legend = { ...(option.legend||{}), show: false }       // 箱型圖通常不需要圖例
      option.grid   = { ...(option.grid||{}), left: '12%', right: '12%', containLabel: true }
      if (Array.isArray(option.series) && option.series[0]?.type === 'boxplot') {
        option.series[0] = { ...option.series[0], boxWidth: [14, 28] } // px 範圍，讓箱寬穩定
      }
    }
    chart.setOption(option)
  })
}

function resizeAll() {
  Object.values(chartRefs).forEach(el => el?.__chartInstance?.resize?.())
}

/* ----------------------- chart options ----------------------- */
function optionBar(analyteNames, zhNames, dataset, unit) {
  const averages = analyteNames.map((_, i) =>
    dataset.reduce((sum, arr) => sum + (arr[i] || 0), 0) / Math.max(1, dataset.length)
  );
  return {
    tooltip: { trigger: 'axis' },
    legend: { data: zhNames },
    grid: { top: 40, right: 16, bottom: 72, left: 56 },
    xAxis: { type: 'category', data: analyteNames },
    yAxis: { type: 'value', name: unit },
    series: [
      ...zhNames.map((name, i) => ({ name, type: 'bar', data: dataset[i], label: { show: true, position: 'top', fontSize: 12 } })),
      { name: '平均值', type: 'line', data: averages, lineStyle: { type: 'dashed' }, symbol: 'none' }
    ]
  };
}

function optionRadar(analyteNames, zhNames, dataset, unit, cfg = {}) {
  const enTails = cfg.enTails || [];
  const maxVal = Math.max(1, ...dataset.flat().map(n => Number(n) || 0)) * 1.2;
  const rows = cfg.rows ?? 1;
  // 副標越高，中心越往下、半徑越小一點
  const centerY = `${Math.min(70, 52 + rows * 4)}%`;
  const radius  = `${Math.max(46, 66 - rows * 3)}%`;

  return {
    tooltip: {
      trigger: 'item',
      confine: true,
      backgroundColor: 'rgba(255,255,255,0.95)',
      borderColor: '#007083', borderWidth: 1,
      textStyle: { color: '#333', fontSize: 12 },
      formatter: (p) => {
        const i = zhNames.indexOf(p.name)
        const name = (i > -1 && enTails[i]) ? `${p.name}（${enTails[i]}）` : p.name;
        let html = `<div style="margin-bottom:4px;"><strong>${name}</strong></div>`;
        for (let k = 0; k < analyteNames.length; k++) {
          const v = p.value?.[k];
          html += `<div>• ${analyteNames[k]}：<b>${fmtNumber(v, unit)}</b></div>`;
        }
        return html;
      }
    },
    legend: {
      data: zhNames, bottom: 8, icon: 'circle',
      itemWidth: 10, itemHeight: 10, textStyle: { fontSize: 12 }
    },
    radar: {
      center: ['50%', centerY],
      radius,
      splitNumber: 5,
      splitArea: { areaStyle: { color: ['#f9f9f9', '#fff'] } },
      axisLine:  { lineStyle: { color: '#ccc' } },
      splitLine: { lineStyle: { color: '#ddd' } },
      indicator: analyteNames.map(n => ({ name: n, max: maxVal }))
    },
    series: [{
      type: 'radar',
      symbol: 'circle', symbolSize: 4,
      lineStyle: { width: 2 },
      areaStyle: { opacity: 0.1 },
      data: zhNames.map((name, i) => ({ name, value: dataset[i] }))
    }]
  };
}

function optionHeatmap(analyteNames, zhNames, dataset, unit) {
  const data = [];
  for (let x = 0; x < zhNames.length; x++) {
    for (let y = 0; y < analyteNames.length; y++) data.push([x, y, toNum(dataset[x][y])])
  }
  const safeLeft = 56;       // ← 更準的左邊距
  const rightPad = 28;  // 依左邊距做對稱微調

  return {
    grid: { top: 56, right: rightPad, bottom: 88, left: safeLeft, containLabel: true },
    xAxis: { type: 'category', data: zhNames, splitArea: { show: true } },
    yAxis: { type: 'category', data: analyteNames, splitArea: { show: true } },
    visualMap: {
      min: 0, max: Math.max(1, ...data.map(d => d[2] || 0)),
      calculable: true, orient: 'horizontal', left: 'center', bottom: 10
    },
    tooltip: { position: 'top' }, // 會被上面的 renderAll 再覆寫成客製 formatter
    series: [{ name: `含量(${unit})`, type: 'heatmap', data, label: { show: true } }]
  };
}


function optionStacked100(analyteNames, zhNames, dataset) {
  const cols = analyteNames.length, rows = zhNames.length;
  const sums = Array(cols).fill(0);
  for (let c = 0; c < cols; c++) for (let r = 0; r < rows; r++) sums[c] += toNum(dataset[r][c]);

  const percent = dataset.map(row => row.map((v, c) => (sums[c] ? (toNum(v) / sums[c]) * 100 : 0)));
  return {
    tooltip: { trigger: 'axis', valueFormatter: v => `${v?.toFixed?.(1) ?? v}%` },
    legend: { data: zhNames },
    grid: { top: 40, right: 16, bottom: 72, left: 56 },
    xAxis: { type: 'category', data: analyteNames },
    yAxis: { type: 'value', name: '%', max: 100, axisLabel: { formatter: '{value}%' } },
    series: zhNames.map((name, i) => ({
      name, type: 'bar', stack: 'total', emphasis: { focus: 'series' },
      data: percent[i].map(v => Number.isFinite(v) ? Number(v.toFixed(2)) : 0)
    }))
  };
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

/* ----------------------- 匯出檔案（修正版） ----------------------- */

// 取得所有已渲染的 ECharts 實例
function getAllChartInstances() {
  return Object.values(chartRefs)
    .map(el => el?.__chartInstance)
    .filter(Boolean)
}

async function exportPng() {
  await nextTick() // 確保圖已渲染
  const charts = getAllChartInstances()
  if (!charts.length) {
    return showWarn('目前沒有可匯出的圖表（請先執行「開始比較」）')
  }

  for (let i = 0; i < charts.length; i++) {
    const inst = charts[i]

    // 1) 取原設定並備份 grid
    const opt = inst.getOption()
    const prevGrid = opt.grid ? JSON.parse(JSON.stringify(opt.grid)) : null
    const baseGrid = Array.isArray(opt.grid) ? (opt.grid[0] || {}) : (opt.grid || {})

    // 2) 暫時拉高上/下邊界（只為了匯出好看）
    inst.setOption({
      grid: {
        ...baseGrid,
        containLabel: true,
        top: Math.max(baseGrid.top || 0, 140),   // ← 關鍵：上方距離
        bottom: Math.max(baseGrid.bottom || 0, 72),
        left: Math.max(baseGrid.left || 0, 64),
        right: Math.max(baseGrid.right || 0, 24),
      }
    })
    inst.resize()
    await new Promise(r => setTimeout(r, 80))   // 等版面重排

    // 3) 匯出圖片（檔名已做安全化）
    const rawUnit = ui.groups[i]?.unit || 'chart'
    const safeUnit = String(rawUnit).replace(/[\\/:*?"<>|]/g, '-').replace(/\s+/g, '_')
    const url = inst.getDataURL({ type: 'png', pixelRatio: 2, backgroundColor: '#ffffff' })
    const a = document.createElement('a')
    a.href = url
    a.download = `營養比較_${safeUnit}_${i + 1}.png`
    document.body.appendChild(a); a.click(); a.remove()

    // 4) 還原原本的 grid 設定（不影響畫面互動）
    inst.setOption({ grid: prevGrid ? prevGrid : {} })
  }
  showSuccess(`已匯出 ${charts.length} 張圖`)
}


function exportCsv() {
  // rows: 單位, 營養素, 食材, 數值
  const rows = [['單位', '營養素', '食材', '數值']]

  ;(ui.groups || []).forEach(grp => {
    const unit = grp.unit || ''
    ;(grp.analytes || []).filter(a => !isPMS(a)).forEach(a => {
      const name = a.analyteName || ''
      ;(a.values || []).forEach(v => {
        rows.push([unit, name, v?.sampleName ?? '', v?.value ?? ''])
      })
    })
  })

  if (rows.length === 1) {
    return showWarn('沒有可匯出的數據（請先執行「開始比較」）')
  }

  // CSV 轉字串（安全包雙引號）
  const csv = rows
    .map(r => r.map(x => `"${String(x).replace(/"/g, '""')}"`).join(','))
    .join('\n')

  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = '營養比較.csv'
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
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

/* 拿掉 Bootstrap 預設為了負 margin 留出的 padding */
.form-check.form-switch {
  padding-left: 0;
  display: inline-flex;
  align-items: center;
  gap: 0.6rem;
}

/* 不要負 margin，開關整條都看得到 */
.form-check.form-switch .form-check-input {
  margin-left: 0;
}

/* 確保打開時圓點真的跑到最右邊 */
.form-check.form-switch .form-check-input:checked {
  background-position: right center;
}

</style>
