<template>
  <div class="container py-4">
    <!-- 導覽 -->
    <div class="d-flex flex-wrap gap-2 align-items-center mb-3">
      <router-link to="/cnt/nutrition" class="btn btn-outline-secondary">
        ← 返回營養資料庫
      </router-link>
      <button class="btn btn-outline-danger" @click="clearCompare">清空比較清單</button>
      <div class="ms-auto d-flex gap-2">
        <!-- 圖表切換 -->
        <div class="btn-group" role="group">
          <button class="btn btn-outline-primary" :class="{active: chartType==='radar'}" @click="chartType='radar'; updateCharts()">雷達圖</button>
          <button class="btn btn-outline-primary" :class="{active: chartType==='bar'}" @click="chartType='bar'; updateCharts()">條狀圖</button>
        </div>
        <!-- 指標模式 -->
        <select class="form-select" style="width: 220px" v-model="mode" @change="rebuildData">
          <option value="pms">脂肪酸組成（P/M/S）</option>
          <option value="major">主要營養素（能量/三大營養）</option>
          <option value="popular">前10熱門（IsPopular）</option>
          <option value="all">全部可用營養素（可能很多）</option>
        </select>
      </div>
    </div>

    <!-- 已選食材 -->
    <div class="mb-3">
      <div class="d-flex flex-wrap gap-2 align-items-center">
        <strong class="me-2">已選食材（{{ compareList.length }}）</strong>
        <span
          v-for="item in compareList"
          :key="item.sampleId"
          class="badge bg-light text-dark border d-flex align-items-center"
        >
          <span class="px-2">{{ item.sampleName || ('#'+item.sampleId) }}</span>
          <button class="btn btn-sm btn-link text-danger" @click="removeOne(item.sampleId)" title="移除">✕</button>
        </span>
      </div>
      <small class="text-muted d-block mt-1">建議 2–10 個，支援 5 個以上比較。</small>
    </div>

    <!-- 資料載入狀態 -->
    <div v-if="loading" class="text-center text-muted py-5">載入中…</div>
    <div v-else-if="!canCompare" class="text-center text-muted py-5">
      請先在食材頁按「加入比較」，至少需要 2 個食材。
    </div>

    <!-- 圖表 -->
    <div v-else>
      <div v-show="chartType==='radar'" ref="radarRef" style="width:100%; height: 460px;"></div>
      <div v-show="chartType==='bar'" ref="barRef" style="width:100%; height: 520px;" class="mt-3"></div>
    </div>
  </div>
</template>

<script>
import * as echarts from 'echarts'
// 若未設置 @ 別名，請改相對路徑 ../../api/cntApi
import { getNutritionById } from '../../api/cntApi'

export default {
  name: 'NutritionCompare',
  data() {
    return {
      loading: false,
      chartType: 'radar',           // 'radar' | 'bar'
      mode: 'popular',              // 'pms' | 'major' | 'popular' | 'all'
      compareList: [],              // [{ sampleId, sampleName, slug }]
      rawBySample: {},              // sampleId -> [{ analyteName, unit, valuePer100g | per100gRaw, IsPopular?, category }]
      // 規範化後：nutrientKeys 與每個食材的值
      nutrientKeys: [],             // 指標名陣列（如 ['蛋白質','脂肪','碳水', ...] 或 ['多元不飽和脂肪 (P)', ...]）
      dataset: {},                  // nutrientKey -> { unit, values: { sampleId: number|null } }
      radarChart: null,
      barChart: null,
    }
  },
  computed: {
    canCompare() {
      return this.compareList.length >= 2
    }
  },
  methods: {
    // 讀取 localStorage 清單（由 Detail 頁加入）
    loadCompareList() {
      try {
        const key = 'nutrition_compare_list'
        const list = JSON.parse(localStorage.getItem(key) || '[]')
        // 限制最多 10 個以避免圖表擁擠
        this.compareList = list.slice(0, 10)
      } catch {
        this.compareList = []
      }
    },
    clearCompare() {
      localStorage.setItem('nutrition_compare_list', '[]')
      this.compareList = []
      this.disposeCharts()
    },
    removeOne(sampleId) {
      const key = 'nutrition_compare_list'
      const list = (JSON.parse(localStorage.getItem(key) || '[]') || []).filter(x => x.sampleId !== sampleId)
      localStorage.setItem(key, JSON.stringify(list))
      this.loadCompareList()
      this.rebuildAll()
    },

    // 主流程：抓全部資料 → 規範化 → 建圖
    async rebuildAll() {
      if (!this.canCompare) return
      this.loading = true
      try {
        // 1) 取每個 sample 的 nutrients
        const all = {}
        for (const item of this.compareList) {
          try {
            const resp = await getNutritionById(item.sampleId)
            all[item.sampleId] = Array.isArray(resp?.nutrients) ? resp.nutrients : []
            // 若沒有 API，fallback 簡單 mock（可刪）
            if (!all[item.sampleId].length) {
              all[item.sampleId] = this.mockNutrients(item.sampleId)
            }
          } catch {
            all[item.sampleId] = this.mockNutrients(item.sampleId)
          }
        }
        this.rawBySample = all

        // 2) 資料規範化（依 mode）
        this.normalizeByMode()

        // 3) 建立 / 更新圖
        this.$nextTick(() => {
          this.initOrUpdateCharts()
        })
      } finally {
        this.loading = false
      }
    },

    // 依模式規範化資料 -> this.nutrientKeys & this.dataset
    normalizeByMode() {
      // helpers 先把每個 sample 的 nutrients 轉為鍵值查找
      const mapBySample = {} // sampleId -> name->obj
      for (const [sid, arr] of Object.entries(this.rawBySample)) {
        mapBySample[sid] = {}
        for (const it of arr) {
          const name = (it.analyteName || it.AnalyteName || '').trim()
          if (!name) continue
          mapBySample[sid][name] = it
        }
      }

      // 依模式挑選指標
      let keys = []
      if (this.mode === 'pms') {
        keys = ['多元不飽和脂肪 (P)', '單元不飽和脂肪 (M)', '飽和脂肪 (S)']
      } else if (this.mode === 'major') {
        // 主要營養素（可按你的實際命名調整）
        keys = ['能量', '蛋白質', '脂肪', '碳水化合物', '膳食纖維']
      } else if (this.mode === 'popular') {
        // 取每個 sample 的熱門（IsPopular）營養素名稱，合併去重後取前 10
        const popularNames = new Set()
        for (const arr of Object.values(this.rawBySample)) {
          arr.forEach(it => {
            const popular = (it.IsPopular ?? it.isPopular) ? 1 : 0
            if (popular) {
              popularNames.add((it.analyteName || it.AnalyteName || '').trim())
            }
          })
        }
        keys = Array.from(popularNames).slice(0, 10)
        if (keys.length === 0) {
          // 若資料中沒有 popular 標記，退回 major
          keys = ['能量', '蛋白質', '脂肪', '碳水化合物', '膳食纖維']
        }
      } else {
        // all：將所有出現的營養素名統合（最多 16 個避免爆圖）
        const names = new Set()
        for (const arr of Object.values(this.rawBySample)) {
          arr.forEach(it => names.add((it.analyteName || it.AnalyteName || '').trim()))
        }
        keys = Array.from(names).filter(Boolean).slice(0, 16)
      }

      // PMS 特殊處理：從 P/M/S raw 拆
      const PMS_CHINESE = {
        P: '多元不飽和脂肪 (P)',
        M: '單元不飽和脂肪 (M)',
        S: '飽和脂肪 (S)'
      }

      const dataset = {} // nutrientKey -> { unit, values: { sampleId: number|null } }
      keys.forEach(k => (dataset[k] = { unit: '', values: {} }))

      // 為每個 sample 填值
      for (const item of this.compareList) {
        const sid = item.sampleId
        // 偵測 P/M/S
        const pmsRow = mapBySample[sid]['P/M/S'] || mapBySample[sid]['PMS']
        let pmsParsed = null
        if (pmsRow && (pmsRow.per100gRaw || pmsRow.Per100g)) {
          pmsParsed = this.parsePMS(pmsRow.per100gRaw || pmsRow.Per100g)
        }

        for (const key of keys) {
          if (key === PMS_CHINESE.P && pmsParsed) {
            dataset[key].values[sid] = this.toNumber(pmsParsed.P)
            dataset[key].unit = (pmsRow.unit || pmsRow.Unit || 'g')
            continue
          }
          if (key === PMS_CHINESE.M && pmsParsed) {
            dataset[key].values[sid] = this.toNumber(pmsParsed.M)
            dataset[key].unit = (pmsRow.unit || pmsRow.Unit || 'g')
            continue
          }
          if (key === PMS_CHINESE.S && pmsParsed) {
            dataset[key].values[sid] = this.toNumber(pmsParsed.S)
            dataset[key].unit = (pmsRow.unit || pmsRow.Unit || 'g')
            continue
          }
          // 一般營養素
          // 嘗試多種常見命名（能量/熱量…）
          const match = mapBySample[sid][key]
            || mapBySample[sid]['能量'] || mapBySample[sid]['熱量'] || mapBySample[sid]['Energy']
            || mapBySample[sid]['蛋白質'] || mapBySample[sid]['Protein']
            || mapBySample[sid]['脂肪'] || mapBySample[sid]['Fat']
            || mapBySample[sid]['碳水化合物'] || mapBySample[sid]['Carbohydrate']
            || mapBySample[sid]['膳食纖維'] || mapBySample[sid]['Fiber']
            || mapBySample[sid][key] // fallback 原名
          if (match) {
            const val = this.toNumber(match.valuePer100g ?? match.Per100g)
            dataset[key].values[sid] = val
            dataset[key].unit = (match.unit || match.DefaultUnit || match.Unit || '')
          } else {
            dataset[key].values[sid] = null
          }
        }
      }

      this.nutrientKeys = keys
      this.dataset = dataset
    },

    // 重新依 mode 規範化並更新圖
    rebuildData() {
      if (!this.canCompare) return
      this.normalizeByMode()
      this.updateCharts()
    },

    // ---- 圖表 ----
    initOrUpdateCharts() {
      // 初始化
      if (!this.radarChart) {
        this.radarChart = echarts.init(this.$refs.radarRef)
      }
      if (!this.barChart) {
        this.barChart = echarts.init(this.$refs.barRef)
      }
      this.updateCharts()
      window.addEventListener('resize', this.handleResize)
    },
    updateCharts() {
      if (!this.canCompare) return
      const { radarOpt, barOpt } = this.buildOptions()
      if (this.radarChart) this.radarChart.setOption(radarOpt, true)
      if (this.barChart) this.barChart.setOption(barOpt, true)
    },
    disposeCharts() {
      try {
        if (this.radarChart) { this.radarChart.dispose(); this.radarChart = null }
        if (this.barChart) { this.barChart.dispose(); this.barChart = null }
        window.removeEventListener('resize', this.handleResize)
      } catch {}
    },
    handleResize() {
      if (this.radarChart) this.radarChart.resize()
      if (this.barChart) this.barChart.resize()
    },

    // 建構 ECharts option
    buildOptions() {
      // X 軸（bar）= 食材；雷達指標 = nutrient keys
      const sampleNames = this.compareList.map(x => x.sampleName || ('#'+x.sampleId))
      // 取每個 nutrient 的 max（作雷達指標上限與 bar 堆疊尺度參考）
      const indicators = this.nutrientKeys.map(k => {
        const vals = this.compareList.map(s => this.dataset[k].values[s.sampleId]).filter(v => v != null)
        const max = vals.length ? Math.max(...vals) : 1
        return { name: k, max: max || 1 }
      })

      // 雷達：每個 sample 是一條 series
      const radarSeries = this.compareList.map(s => ({
        name: s.sampleName || ('#'+s.sampleId),
        type: 'radar',
        data: [{
          value: this.nutrientKeys.map(k => this.dataset[k].values[s.sampleId] ?? 0),
          name: s.sampleName || ('#'+s.sampleId)
        }]
      }))

      // 條圖：用「指標為分組、食材為系列」或反之；這裡採「指標為類別軸，食材為系列」
      const barSeries = this.compareList.map(s => ({
        name: s.sampleName || ('#'+s.sampleId),
        type: 'bar',
        emphasis: { focus: 'series' },
        data: this.nutrientKeys.map(k => this.dataset[k].values[s.sampleId] ?? 0)
      }))

      const radarOpt = {
        tooltip: { trigger: 'item' },
        legend: { top: 0, type: 'scroll' },
        radar: { indicator: indicators, radius: '62%' },
        series: radarSeries
      }

      const barOpt = {
        tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
        legend: { top: 0, type: 'scroll' },
        grid: { left: 10, right: 10, bottom: 20, containLabel: true },
        xAxis: { type: 'category', data: this.nutrientKeys, axisLabel: { interval: 0 } },
        yAxis: { type: 'value' },
        series: barSeries
      }

      return { radarOpt, barOpt }
    },

    // ---- utils ----
    parsePMS(raw) {
      const parts = String(raw || '').split('/').map(x => this.toNumber(x))
      return { P: parts[0] ?? null, M: parts[1] ?? null, S: parts[2] ?? null }
    },
    toNumber(x) {
      const n = Number(x)
      return isNaN(n) ? null : n
    },

    // 若 API 暫無資料 → 提供可視化 mock（依 sampleId 做變化避免完全一樣）
    mockNutrients(sampleId) {
      // 含 P/M/S 與主要營養素
      const seed = Number(String(sampleId).slice(-2)) || 1
      const r = (b) => Number((b * (1 + (seed % 7) / 20)).toFixed(2))
      return [
        { analyteName: 'P/M/S', unit: 'g', per100gRaw: `${r(1.2)}/${r(1.8)}/${r(0.9)}`, category: '脂肪酸組成' },
        { analyteName: '能量', unit: 'kcal', valuePer100g: r(180) },
        { analyteName: '蛋白質', unit: 'g', valuePer100g: r(20) },
        { analyteName: '脂肪', unit: 'g', valuePer100g: r(12) },
        { analyteName: '碳水化合物', unit: 'g', valuePer100g: r(5) },
        { analyteName: '膳食纖維', unit: 'g', valuePer100g: r(1.8) },
        // popular 標記示例
        { analyteName: '維生素B12', unit: 'µg', valuePer100g: r(4.1), IsPopular: 1 },
        { analyteName: '維生素D', unit: 'IU', valuePer100g: r(180), IsPopular: 1 },
        { analyteName: '鈣', unit: 'mg', valuePer100g: r(25), IsPopular: 1 },
        { analyteName: '鐵', unit: 'mg', valuePer100g: r(0.8), IsPopular: 1 },
      ]
    }
  },

  async mounted() {
    this.loadCompareList()
    if (this.canCompare) {
      await this.rebuildAll()
    }
  },

  beforeUnmount() {
    this.disposeCharts()
  }
}
</script>

<style scoped>
.badge button {
  line-height: 1;
  padding: 0 0.25rem;
}
</style>
