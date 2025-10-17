<template>
  <div class="container py-4">
    <!-- 返回 -->
    <router-link to="/cnt/nutrition" class="btn btn-outline-secondary mb-3">
      ← 返回營養資料庫
    </router-link>

    <!-- 食材基本資訊 -->
    <h2 class="fw-bold main-color-green-text mb-1">{{ sample.sampleName }}</h2>
    <p class="text-muted mb-4" v-if="sample.contentDesc">{{ sample.contentDesc }}</p>

    <!-- 營養成分表（每100g） -->
    <div class="d-flex align-items-baseline gap-2 mb-2">
      <h5 class="mb-0">營養成分（每 100g）</h5>
      <small class="text-muted" v-if="normalizedNutrients.length">共 {{ normalizedNutrients.length }} 項</small>
    </div>

    <div class="table-responsive">
      <table class="table align-middle">
        <thead class="table-light">
          <tr>
            <th style="min-width: 220px;">營養素</th>
            <th class="text-end" style="width: 140px;">含量</th>
            <th>視覺化</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(n, idx) in normalizedNutrients" :key="idx" :title="n.tooltip">
            <td>
              <div class="fw-semibold">{{ n.name }}</div>
              <small class="text-muted" v-if="n.category">{{ n.category }}</small>
            </td>
            <td class="text-end">
              <span class="fw-semibold">{{ formatNumber(n.value) }}</span>
              <span class="text-muted">{{ ' ' + (n.unit || '') }}</span>
            </td>
            <td style="min-width: 240px;">
              <div class="progress" role="progressbar" :aria-valuenow="Math.round(n.percent)" aria-valuemin="0" aria-valuemax="100">
                <div class="progress-bar" :style="{ width: n.percent + '%' }">
                  {{ Math.round(n.percent) }}%
                </div>
              </div>
            </td>
          </tr>
          <tr v-if="normalizedNutrients.length === 0">
            <td colspan="3" class="text-center text-muted py-5">暫無營養資料</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- 動作 -->
    <div class="mt-4 d-flex gap-2">
      <button class="btn btn-outline-success" @click="addToCompare()">
        加入比較
      </button>
      <router-link to="/cnt/nutrition" class="btn btn-outline-primary">
        回清單
      </router-link>
    </div>
  </div>
</template>

<script>
// ⚠ 若你的專案未設定 @ 別名，請改成相對路徑：'../../api/cntApi'
import { getNutritionById } from '../../api/cntApi'

export default {
  name: 'NutritionDetail',
  props: ['id', 'slug'],
  data() {
    return {
      loading: false,
      sample: {
        sampleId: null,
        sampleName: '',
        contentDesc: ''
      },
      // 原始營養陣列（來自 API 的「分析項目 + 含量」）
      // 期望每筆至少帶：{ analyteName, unit, valuePer100g } 或 { analyteName: 'P/M/S', per100gRaw: '1.52/1.89/1.00', unit, category }
      rawNutrients: []
    }
  },
  computed: {
    // 轉換成前端可用的顯示資料（含：拆解 P/M/S、計算 percent）
    normalizedNutrients() {
      // 1) 先把所有營養項目攤平
      const rows = []
      for (const it of this.rawNutrients) {
        if (this.isPMSRow(it)) {
          const trio = this.parsePMS(it.per100gRaw || it.valuePer100g || it.Per100g)
          const unit = it.unit || it.DefaultUnit || it.Unit || 'g'
          const category = it.category || it.AnalyteCategory || '脂肪酸組成'
          // P/M/S 對應中文
          const map = [
            { key: 'P', label: '多元不飽和脂肪 (P)', value: trio.P },
            { key: 'M', label: '單元不飽和脂肪 (M)', value: trio.M },
            { key: 'S', label: '飽和脂肪 (S)', value: trio.S }
          ]
          map.forEach(m => {
            rows.push({
              name: m.label,
              unit,
              value: this.toNumber(m.value),
              category,
              tooltip: `P/M/S：${trio.P ?? '-'} / ${trio.M ?? '-'} / ${trio.S ?? '-'} ${unit}`
            })
          })
        } else {
          // 一般營養素
          const unit = it.unit || it.DefaultUnit || it.Unit || ''
          const value = this.toNumber(it.valuePer100g ?? it.Per100g)
          rows.push({
            name: it.analyteName || it.AnalyteName || '-',
            unit,
            value,
            category: it.category || it.AnalyteCategory || '',
            tooltip: value != null ? `${value} ${unit}` : ''
          })
        }
      }

      // 2) 計算進度條百分比（以同頁面中的最大值為 100%）
      const max = rows.reduce((m, r) => (r.value != null && r.value > m ? r.value : m), 0) || 1
      return rows.map(r => ({
        ...r,
        percent: Math.max(0, Math.min(100, (r.value / max) * 100))
      }))
    }
  },
  methods: {
    async fetchNutritionDetail(sampleId) {
      this.loading = true
      try {
        // 🔌 這裡串你的後端：請回傳 sample 與 nutrients
        // 期望回傳格式：
        // {
        //   sample: { sampleId, sampleName, contentDesc },
        //   nutrients: [
        //     { analyteName: '蛋白質', unit: 'g', valuePer100g: 22.5 },
        //     { analyteName: 'P/M/S', unit: 'g', per100gRaw: '1.52/1.89/1.00', category: '脂肪酸組成' },
        //     ...
        //   ]
        // }
        const resp = await getNutritionById(sampleId)
        // 驗證並套入
        this.sample = {
          sampleId: resp?.sample?.sampleId ?? sampleId,
          sampleName: resp?.sample?.sampleName ?? (this.slug || '未命名食材'),
          contentDesc: resp?.sample?.contentDesc ?? ''
        }
        this.rawNutrients = Array.isArray(resp?.nutrients) ? resp.nutrients : []
      } catch (err) {
        console.warn('[NutritionDetail] API 失敗，使用 mock 資料。', err)

        // 🧪 MOCK：依你提供的資料形態建立示例（可刪）
        this.sample = {
          sampleId,
          sampleName: '鯖魚（示例）',
          contentDesc: '富含 Omega-3 的高營養食材；示例資料（無 API）。'
        }
        this.rawNutrients = [
          // 脂肪酸組成：P/M/S 三值
          { analyteName: 'P/M/S', unit: 'g', per100gRaw: '1.52/1.89/1.00', category: '脂肪酸組成' },
          // 一般營養素
          { analyteName: '蛋白質', unit: 'g', valuePer100g: 22.5 },
          { analyteName: '脂肪', unit: 'g', valuePer100g: 12.3 },
          { analyteName: '維生素B12', unit: 'µg', valuePer100g: 4.2 }
        ]
      } finally {
        this.loading = false
      }
    },

    addToCompare() {
      try {
        const key = 'nutrition_compare_list'
        const list = JSON.parse(localStorage.getItem(key) || '[]')
        if (!list.find(x => x.sampleId === this.sample.sampleId)) {
          list.push({
            sampleId: this.sample.sampleId,
            sampleName: this.sample.sampleName,
            slug: this.slug
          })
          localStorage.setItem(key, JSON.stringify(list))
          alert(`已加入比較：${this.sample.sampleName}`)
        } else {
          alert('此食材已在比較清單中')
        }
      } catch {
        alert('加入比較失敗，請稍後再試')
      }
    },

    isPMSRow(item) {
      const name = (item.analyteName || item.AnalyteName || '').toUpperCase().trim()
      return name === 'P/M/S' || name === 'PMS' || (item.per100gRaw && item.per100gRaw.includes('/')) || (item.Per100g && String(item.Per100g).includes('/'))
    },

    parsePMS(raw) {
      if (!raw) return { P: null, M: null, S: null }
      const parts = String(raw).split('/').map(x => this.toNumber(x))
      return { P: parts[0] ?? null, M: parts[1] ?? null, S: parts[2] ?? null }
    },

    toNumber(x) {
      const n = Number(x)
      return isNaN(n) ? null : n
    },

    formatNumber(n) {
      if (n == null) return '-'
      // 小數位數自動：>=1 顯示到 2 位；<1 顯示到 3 位
      return n >= 1 ? n.toFixed(2) : n.toFixed(3)
    }
  },

  mounted() {
    // 從路由參數取得 id（/cnt/nutrition/:slug-:id）
    const sid = this.id || (this.$route?.params?.id)
    this.fetchNutritionDetail(sid)
  }
}
</script>

<style scoped>
/* 可依 main.css 覆寫 progress-bar 顏色 */
/* 例如：.progress-bar { background-color: var(--main-teal); } */
</style>
