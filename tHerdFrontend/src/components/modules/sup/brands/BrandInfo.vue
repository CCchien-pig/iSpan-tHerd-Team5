<template>
  <div v-if="brandOverview" class="brand-info-container">
    <div class="brand-overview-card shadow-sm">
      <h5 class="card-title text-teal mb-4">
        <i class="bi bi-bar-chart-line-fill me-2"></i>品牌概況
      </h5>

      <div class="info-layout">
        <div class="top-row mb-4">
          <div class="info-item">
            <div class="icon-wrapper">
              <i class="bi bi-box-seam"></i>
            </div>
            <div class="info-content">
              <span class="label">商品總數</span>
              <span class="value"
                >{{ brandOverview.productCount }} <small class="unit">個</small></span
              >
            </div>
          </div>

          <div class="info-item">
            <div class="icon-wrapper">
              <i class="bi bi-heart-fill"></i>
            </div>
            <div class="info-content">
              <span class="label">獲收藏數</span>
              <span class="value">{{ brandOverview.favoriteCount }}</span>
            </div>
          </div>
        </div>

        <div class="bottom-row">
          <div class="info-item justify-self-start">
            <div class="icon-wrapper">
              <i class="bi bi-building"></i>
            </div>
            <div class="info-content">
              <span class="label">供應商名稱</span>
              <span class="value supplier-name" :title="brandOverview.supplierName">
                {{ brandOverview.supplierName }}
              </span>
            </div>
          </div>

          <div class="info-item justify-self-center">
            <div class="icon-wrapper">
              <i class="bi bi-receipt"></i>
            </div>
            <div class="info-content">
              <span class="label">公司統編</span>
              <span class="value">{{ currentTaxId }}</span>
            </div>
          </div>

          <div class="info-item justify-self-end">
            <div class="icon-wrapper">
              <i class="bi bi-clock-history"></i>
            </div>
            <div class="info-content">
              <span class="label">成立天數</span>
              <span class="value">
                {{ brandOverview.createdDaysAgo }} <small class="unit">天</small>
              </span>
            </div>
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
  props: {
    brandId: {
      type: Number,
      required: true,
    },
  },
  emits: ['update:brandInfoAvailable'],
  data() {
    return {
      brandOverview: null,
      errorMessage: '',
      // 隨機統編池
      taxIds: ['50784323', '29079069', '05714195', '54357012', '89589328'],
      currentTaxId: '',
    }
  },
  async created() {
    // 元件建立時，先隨機選一個統編
    this.currentTaxId = this.taxIds[Math.floor(Math.random() * this.taxIds.length)]

    try {
      const response = await axios.get(`/api/sup/Brands/${this.brandId}/overview`)
      if (response.data.success) {
        this.brandOverview = response.data.data
        this.$emit('update:brandInfoAvailable', true)
      } else {
        this.$emit('update:brandInfoAvailable', false)
      }
    } catch (err) {
      console.error(err)
      this.$emit('update:brandInfoAvailable', false)
    }
  },
}
</script>

<style scoped>
.brand-info-container {
  width: 100%; /* 區塊橫向佔滿父層 */
  display: flex;
  justify-content: center; /* 內部卡片水平置中 */
  align-items: center;
  margin-top: 2rem;
  margin-bottom: 2rem;
}

.brand-overview-card {
  background-color: #fff;
  border-radius: 12px;
  padding: 1.5rem 2.5rem;
  border: 1px solid #e9ecef;
  border-top: 4px solid #007083;
  min-width: 320px;
  /* max-width: 560px; */
  width: 100%;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  align-items: center;
  box-sizing: border-box;
}

.top-row {
  width: 100%;
  display: flex;
  justify-content: center;
  gap: 10rem;
  align-items: center;
  padding-bottom: 1.5rem;
  border-bottom: 1px dashed #e9ecef;
}

.bottom-row {
  width: 100%;
  display: grid;
  grid-template-columns: 1fr 1fr 1fr;
  gap: 2rem;
  padding-top: 1.5rem;
  justify-items: center;
}

/* Grid 子項目的自身對齊補充 */
.justify-self-start {
  justify-self: start;
}
.justify-self-center {
  justify-self: center;
}
.justify-self-end {
  justify-self: end;
}

/* info-item 樣式 */
.info-item {
  display: flex;
  align-items: center;
  gap: 12px;
}

/* icon 和 content 樣式 */
.icon-wrapper {
  width: 46px;
  height: 46px;
  background-color: #f2f9fa;
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
  font-weight: normal;
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

/* --- RWD 響應式 --- */
@media (max-width: 768px) {
  .brand-info-container {
    padding-left: 8px;
    padding-right: 8px;
    margin-top: 1rem;
    margin-bottom: 1rem;
  }
  .brand-overview-card {
    padding: 1rem 0.5rem;
    min-width: 0;
    max-width: 100%;
  }
  .top-row,
  .bottom-row {
    width: 100%;
  }
  .bottom-row {
    grid-template-columns: 1fr;
    gap: 1.5rem;
    justify-items: center;
  }
  .justify-self-center,
  .justify-self-end {
    justify-self: start;
  }
}
</style>
