<template>
  <div class="p-4 space-y-4">
    <!-- 🧾 基本資料區 -->
    <el-card>
      <div class="flex flex-col md:flex-row md:items-start gap-4">
        <div class="flex-1">
          <h2 class="text-xl font-bold">{{ sample.sampleName }}</h2>
          <div class="text-gray-600 mt-1">分類：{{ sample.categoryName }}</div>
          <div class="text-gray-700 mt-2 whitespace-pre-wrap">
            描述：{{ sample.description }}
          </div>
        </div>

        <!-- 🔧 排序控制與圖表刷新 -->
        <div class="flex items-center gap-2">
          <el-select v-model="sortOrder" class="w-44" placeholder="排序方式">
            <el-option label="原始順序" value="none" />
            <el-option label="由大到小" value="desc" />
            <el-option label="由小到大" value="asc" />
          </el-select>
          <el-button @click="renderCharts" type="primary" plain>
            更新圖表
          </el-button>
        </div>
      </div>
    </el-card>

    <!-- 📊 圖表展示 -->
    <el-card>
      <el-tabs v-model="activeChart" @tab-change="renderCharts">
        <el-tab-pane label="柱狀圖（Top 10）" name="bar">
          <div ref="barRef" style="width: 100%; height: 380px;"></div>
        </el-tab-pane>
        <el-tab-pane label="雷達圖（Top 6）" name="radar">
          <div ref="radarRef" style="width: 100%; height: 380px;"></div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 🗂 營養成分表（依分類群組） -->
    <el-card>
      <el-collapse v-model="openGroups" accordion>
        <el-collapse-item
          v-for="(rows, cat) in grouped"
          :key="cat"
          :name="cat"
        >
          <template #title>
            <span class="font-semibold">{{ cat }}</span>
            <span class="text-gray-500 ml-2">（{{ rows.length }} 項）</span>
          </template>

          <el-table :data="rows" border>
            <el-table-column prop="name" label="營養素" min-width="220" />
            <el-table-column label="數值 (每100g)">
              <template #default="{ row }">
                {{ displayVal(row.valuePer100g) }}
              </template>
            </el-table-column>
            <el-table-column prop="unit" label="單位" width="120" />
          </el-table-column>
          </el-table>
        </el-collapse-item>
      </el-collapse>
    </el-card>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, nextTick, watch } from 'vue'
import { useRoute } from 'vue-router'
import * as echarts from 'echarts'
import { getNutritionDetail } from './api/cntService'

// 路由ID
const route = useRoute()
const id = Number(route.params.id)

// 🧾 Basic Info
const sample = ref({ sampleId: id, sampleName: '-', categoryName: '-', description: '-' })
const nutrients = ref([])

// 排序控制
const sortOrder = ref('none') // none/desc/asc
const activeChart = ref('bar')
const openGroups = ref([])

// 顯示Null為 "-"
const displayVal = (v) => (v === null || v === undefined ? '-' : v)

// 分類群組 + 排序
const grouped = computed(() => {
  const g = nutrients.value.reduce((map, n) => {
    const key = n.category || '其他'
    if (!map[key]) map[key] = []
    map[key].push({ ...n })
    return map
  }, {})

  const sortFn = (a, b) => {
    const va = a.valuePer100g ?? null
    const vb = b.valuePer100g ?? null
    if (va === null && vb === null) return 0
    if (va === null) return 1
    if (vb === null) return -1
    return sortOrder.value === 'asc' ? va - vb
      : sortOrder.value === 'desc' ? vb - va
      : 0
  }

  Object.keys(g).forEach(k => g[k].sort(sortFn))
  return g
})

// 🔬 ECharts
const barRef = ref(null)
let barChart = null
const radarRef = ref(null)
let radarChart = null

function topByValue(list, topN) {
  const ok = list
    .filter(x => x.valuePer100g !== null && x.valuePer100g !== undefined)
    .sort((a, b) => b.valuePer100g - a.valuePer100g)
    .slice(0, topN)
  return ok
}

async function renderCharts() {
  await nextTick()

  if (barChart) barChart.dispose()
  if (radarChart) radarChart.dispose()

  // Bar
  const top10 = topByValue(nutrients.value, 10)
  if (barRef.value) {
    barChart = echarts.init(barRef.value)
    barChart.setOption({
      tooltip: { trigger: 'axis' },
      grid: { left: 10, right: 10, top: 20, bottom: 40, containLabel: true },
      xAxis: { type: 'value' },
      yAxis: { type: 'category', data: top10.map(x => x.name + (x.unit ? ` (${x.unit})` : '')) },
      series: [{ type: 'bar', data: top10.map(x => x.valuePer100g) }]
    })
  }

  // Radar
  const top6 = topByValue(nutrients.value, 6)
  if (radarRef.value) {
    radarChart = echarts.init(radarRef.value)
    radarChart.setOption({
      tooltip: {},
      radar: {
        indicator: top6.map(x => ({ name: x.name, max: Math.max(...top6.map(y => y.valuePer100g)) || 1 }))
      },
      series: [
        {
          type: 'radar',
          data: [
            { value: top6.map(x => x.valuePer100g), name: '每100g含量' }
          ]
        }
      ]
    })
  }
}

async function fetchDetail() {
  const { sample: s, nutrients: n } = await getNutritionDetail(id)
  sample.value = s || sample.value
  nutrients.value = (n || []).map(x => ({
    category: x.category || '其他',
    name: x.name,
    unit: x.unit || '-',
    valuePer100g: x.valuePer100g === 0 ? 0 : (x.valuePer100g ?? null)
  }))
  openGroups.value = Object.keys(grouped.value)
  renderCharts()
}

onMounted(fetchDetail)
watch(sortOrder, () => nextTick(renderCharts))
</script>

<style scoped>
.p-4{padding:1rem}
.space-y-4 > * + *{margin-top:1rem}
.text-xl{font-size:1.25rem}
.font-bold{font-weight:700}
.text-gray-600{color:#6b7280}
.text-gray-700{color:#374151}
.whitespace-pre-wrap{white-space:pre-wrap}
</style>
