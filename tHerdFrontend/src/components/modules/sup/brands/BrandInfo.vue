<template>
  <div v-if="brandOverview" class="brand-info-container">
    <div class="brand-overview-card shadow-sm">
      <h5 class="card-title text-teal mb-4">
        <i class="bi bi-bar-chart-line-fill me-2"></i>品牌概況
      </h5>

      <!-- 上下都用相同 three-cols -->
      <div class="grid-6 mb-3">
        <!-- 上三：商品總數 / 總銷售量 / 獲收藏數 -->
        <div class="info-item">
          <div class="icon-wrapper"><i class="bi bi-box-seam"></i></div>
          <div class="info-content">
            <span class="label">商品總數</span>
            <span class="value"
              >{{ brandOverview.productCount }} <small class="unit">個</small></span
            >
          </div>
        </div>

        <div class="info-item">
          <div class="icon-wrapper"><i class="bi bi-bag-check-fill"></i></div>
          <div class="info-content">
            <span class="label">該品牌總銷售量</span>
            <span class="value">{{ brandOverview.totalSales }} <small class="unit">件</small></span>
          </div>
        </div>

        <div class="info-item">
          <div class="icon-wrapper"><i class="bi bi-heart-fill"></i></div>
          <div class="info-content">
            <span class="label">獲收藏數</span>
            <span class="value">{{ brandOverview.favoriteCount }}</span>
          </div>
        </div>

        <!-- 下三：供應商 / 統編 / 成立天數 -->
        <div class="info-item">
          <div class="icon-wrapper"><i class="bi bi-building"></i></div>
          <div class="info-content">
            <span class="label">供應商名稱</span>
            <span class="value supplier-name" :title="brandOverview.supplierName">
              {{ brandOverview.supplierName }}
            </span>
          </div>
        </div>

        <div class="info-item">
          <div class="icon-wrapper"><i class="bi bi-receipt"></i></div>
          <div class="info-content">
            <span class="label">公司統編</span>
            <span class="value">{{ currentTaxId }}</span>
          </div>
        </div>

        <div class="info-item">
          <div class="icon-wrapper"><i class="bi bi-clock-history"></i></div>
          <div class="info-content">
            <span class="label">成立天數</span>
            <span class="value"
              >{{ brandOverview.createdDaysAgo }} <small class="unit">天</small></span
            >
          </div>
        </div>
      </div>
    </div>
  </div>

  <div v-else-if="errorMessage" class="error-message">{{ errorMessage }}</div>
  <div v-else class="text-muted py-3"><i class="bi bi-hourglass-split me-1"></i>讀取資訊中...</div>
</template>

<script>
import axios from 'axios'

export default {
  props: { brandId: { type: Number, required: true } },
  emits: ['update:brandInfoAvailable'],
  data() {
    return {
      brandOverview: null,
      errorMessage: '',
      taxIds: [
        '50784323',
        '29079069',
        '05714195',
        '54357012',
        '89589328',
        '12845673',
        '67483920',
        '34012987',
      ],
      currentTaxId: '',
    }
  },
  async created() {
    this.currentTaxId = this.taxIds[Math.floor(Math.random() * this.taxIds.length)]
    try {
      const { data } = await axios.get(`/api/sup/Brands/${this.brandId}/overview`)
      if (data?.success) {
        this.brandOverview = data.data
        this.$emit('update:brandInfoAvailable', true)
      } else {
        this.errorMessage = data?.message || '查無品牌資料'
        this.$emit('update:brandInfoAvailable', false)
      }
    } catch (err) {
      this.errorMessage = err?.response?.data?.message || '讀取失敗'
      this.$emit('update:brandInfoAvailable', false)
    }
  },
}
</script>

<style scoped>
.brand-info-container {
  width: 100%;
  display: flex;
  justify-content: center;
  align-items: center;
  margin: 2rem 0;
}
.brand-overview-card {
  background: #fff;
  border-radius: 12px;
  padding: 1.5rem 2.5rem;
  border: 1px solid #e9ecef;
  border-top: 4px solid #007083;
  width: 100%;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  align-items: center;
  box-sizing: border-box;
}
/* 六格統一網格 */
.grid-6 {
  width: 100%;
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1.25rem; /* 縮小格與格之間距 */
  align-items: center;
  padding: 0.5rem 0; /* 上下縮小 */
  border-top: 1px dashed #e9ecef; /* 可選：僅保留上邊分隔線 */
}
.three-cols {
  width: 100%;
  display: grid;
  grid-template-columns: 1fr 1fr 1fr;
  gap: 2rem;
  align-items: center;
}
.justify-self-start {
  justify-self: start;
}
.justify-self-center {
  justify-self: center;
}
.justify-self-end {
  justify-self: end;
}
.info-item {
  display: flex;
  align-items: center;
  gap: 12px;
}
.icon-wrapper {
  width: 46px;
  height: 46px;
  background: #f2f9fa;
  color: #007083;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.3rem;
  flex-shrink: 0;
}
.info-content {
  display: flex;
  flex-direction: column;
}
.label {
  font-size: 0.9rem;
  color: #6c757d;
  margin-bottom: 2px;
}
.value {
  font-size: 1.25rem;
  font-weight: 700;
  color: #2c3e50;
  line-height: 1.2;
  white-space: nowrap;
}
.unit {
  font-size: 0.9rem;
  color: #6c757d;
  margin-left: 2px;
}
.supplier-name {
  font-size: 1.1rem;
  max-width: 180px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  display: block;
}
.error-message {
  color: #dc3545;
  padding: 1rem;
  text-align: center;
}
.text-teal {
  color: #007083;
}
.card-title {
  font-weight: 700;
  font-size: 1.2rem;
}
.supplier-name {
  display: block;
  max-width: 220px; /* 視版面調整 */
  white-space: normal; /* 允許換行 */
  word-break: break-word; /* 長字斷行 */
  overflow-wrap: anywhere;
  line-height: 1.3;
}

/* 響應式：手機改成一欄 */
@media (max-width: 768px) {
  .grid-6 {
    grid-template-columns: 1fr;
    gap: 1rem;
    border-top: 1px dashed #e9ecef;
  }
}
</style>
