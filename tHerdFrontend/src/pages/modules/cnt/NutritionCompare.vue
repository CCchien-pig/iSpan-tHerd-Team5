<template>
  <div class="container py-4">
    <!-- 頁面標題與控制列 -->
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h2 class="main-color-green-text">營養比較分析</h2>
      <div>
        <button class="btn btn-outline-success me-2" @click="toggleChartType">
          🔄 切換圖表：{{ chartTypeLabel }}
        </button>
        <button class="btn btn-outline-secondary" @click="reloadData">⟳ 重新載入</button>
      </div>
    </div>

    <!-- Tabs：依單位分組 -->
    <ul class="nav nav-tabs mb-3">
      <li class="nav-item" v-for="(g, i) in state.groups" :key="i">
        <button class="nav-link" :class="{ active: state.activeTab === i }" @click="state.activeTab = i">
          {{ g.unit }}
        </button>
      </li>
    </ul>

    <!-- 小 multiples (面板圖) -->
    <div class="row row-cols-1 row-cols-md-2 row-cols-lg-3 g-4">
      <div class="col" v-for="(a, idx) in activeAnalytes" :key="idx">
        <div class="card shadow-sm border-0 h-100">
          <div class="card-body">
            <h6 class="card-title text-center fw-bold mb-3">{{ a.analyteName }}</h6>
            <canvas :id="'chart-' + idx" height="200"></canvas>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { reactive, computed, watch, onMounted } from "vue";
import Chart from "chart.js/auto";
import annotationPlugin from "chartjs-plugin-annotation";
import axios from "axios";
Chart.register(annotationPlugin);

const state = reactive({
  groups: [],
  activeTab: 0,
  charts: [],
  chartType: "bar" // bar | radar
});

const chartTypeLabel = computed(() => (state.chartType === "bar" ? "長條圖" : "雷達圖"));
const activeAnalytes = computed(() => state.groups[state.activeTab]?.analytes || []);

function toggleChartType() {
  state.chartType = state.chartType === "bar" ? "radar" : "bar";
  drawCharts();
}

async function reloadData() {
  try {
    const { data } = await axios.get("/api/cnt/nutrition/compare", {
      params: {
        sampleIds: "3174,3175,3176",
        analyteIds: "1105,1107,1110,1112,1115"
      }
    });
    state.groups = data.groups || [];
    drawCharts();
  } catch (err) {
    alert(err.response?.data?.error || "載入資料失敗");
  }
}

function drawCharts() {
  // 清除舊的圖
  state.charts.forEach(c => c.destroy());
  state.charts = [];

  const analytes = activeAnalytes.value;
  analytes.forEach((a, idx) => {
    const ctx = document.getElementById("chart-" + idx);
    if (!ctx) return;

    const values = a.values.map(v => v.value);
    const avg = values.reduce((s, v) => s + v, 0) / values.length;

    const chart = new Chart(ctx, {
      type: state.chartType,
      data: {
        labels: a.values.map(v => v.sampleName),
        datasets: [
          {
            label: a.unit,
            data: values,
            borderWidth: 2,
            backgroundColor: "rgba(76,175,80,0.5)",
            borderColor: "rgba(56,142,60,0.9)",
            fill: state.chartType === "radar"
          }
        ]
      },
      options: {
        responsive: true,
        scales: {
          y: state.chartType === "bar" ? { beginAtZero: true } : undefined
        },
        plugins: {
          legend: { display: false },
          annotation: {
            annotations:
              state.chartType === "bar"
                ? {
                    avgLine: {
                      type: "line",
                      yMin: avg,
                      yMax: avg,
                      borderColor: "rgba(255,0,0,0.6)",
                      borderWidth: 1.5,
                      label: {
                        content: `平均 ${avg.toFixed(2)}`,
                        enabled: true,
                        position: "end"
                      }
                    }
                  }
                : {}
          }
        }
      }
    });

    state.charts.push(chart);
  });
}

watch(() => state.activeTab, () => drawCharts());
onMounted(reloadData);
</script>

<style scoped>
.nav-tabs .nav-link.active {
  background-color: var(--main-color-green);
  color: #fff;
}
.card {
  border-radius: 1rem;
}
</style>
