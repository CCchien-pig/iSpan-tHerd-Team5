<!--
  ProductInfo.vue - 商品資訊組件
  功能：商品標題、評分、規格選擇、價格、加入購物車
-->
<template>
  <div class="product-info">
    <!-- 商品標籤 -->
    <div class="brand-badge mb-3">
      <span class="badge bg-success">{{ product.badge }}</span>
    </div>

    <!-- 商品標題 -->
    <h1 class="product-title">{{ product.productName }}</h1>

    <!-- 品牌名稱 -->
    <p class="brand-name text-muted mb-2">由 {{ product.brandName }}</p>

    <!-- 評分與評價 -->
    <div class="rating-section mb-3">
      <span class="rating-value">{{ product.avgRating || 0 }}</span>
      <div class="stars">
        <span v-for="i in 5" :key="i" class="star">
          <i
            class="bi"
            :class="
              i <= Math.floor(product.avgRating || 0)
                ? 'bi-star-fill text-warning'
                : 'bi-star text-warning'
            "
          ></i>
        </span>
      </div>
      <a href="#reviews" class="reviews-link"
        >{{ product.reviewCount || 0 }} 則評價</a
      >
    </div>

    <!-- 有庫存標籤 -->
    <div class="stock-status mb-3">
      <span class="badge bg-success">有庫存</span>
    </div>

    <!-- 促銷訊息 -->
    <div class="promo-message mb-3 p-3 bg-light border-start border-warning border-4">
      <i class="bi bi-gift-fill text-warning me-2"></i>
      <strong>「定期自動送貨優惠」特價品</strong>
      <p class="mb-0 mt-1 small">首次訂購可享 30% 優惠至多批！後續訂購享 15% 折扣！</p>
    </div>

    <!-- 規格選擇 -->
    <div class="spec-section mb-4">
      <label class="form-label fw-bold">包裝數量: {{ selectedSpec?.OptionName || '請選擇' }}</label>
      <div class="spec-options">
        <button
          v-for="spec in product.Specs"
          :key="spec.SkuId"
          class="spec-button"
          :class="{ active: selectedSpec?.SkuId === spec.SkuId, disabled: !spec.IsActive }"
          :disabled="!spec.IsActive"
          @click="selectSpec(spec)"
        >
          <div class="spec-name">{{ spec.OptionName }}</div>
          <div class="spec-price">NT${{ spec.SalePrice || spec.UnitPrice }}</div>
        </button>
      </div>
    </div>

    <!-- 商品基本資訊 -->
    <div class="product-meta mb-4">
      <ul class="list-unstyled small">
        <li><strong>包裝規格：</strong>{{ selectedSpec?.OptionName || product.PackageType }}</li>
        <li><strong>效期：</strong>{{ formatDate(product.expiryDate) }}</li>
        <li v-if="product.dimensions">
          <strong>約尺寸：</strong>{{ product.weight }}公斤，{{ product.dimensions }}
        </li>
        <li><strong>商品編號：</strong>{{ product.productId }}</li>
        <li><strong>產品代碼：</strong>{{ product.productCode }}</li>
        <li><strong>UPC 代碼：</strong>{{ product.upcCode }}</li>
      </ul>
    </div>

    <!-- 包裝描述 -->
    <div class="package-info mb-4 p-3 bg-light rounded">
      <p class="mb-0 small">
        包裝使用可全面回收的瓶罐，無 BPA 成分，無 PVC 塑膠材質，按此
        <a href="#" class="text-primary">比較</a>
      </p>
    </div>

    <!-- 警語說明 -->
    <div class="warning-info mb-4">
      <p class="small mb-1">
        <i class="bi bi-shield-check text-success me-1"></i>
        無添加製造程序所需之外的成分
      </p>
      <p class="small mb-0">
        嗜睡藥物或酒精飲料與本產品一起服用時，會增強嗜睡的效果。
        <a href="#" class="text-primary">比較</a>
      </p>
    </div>
  </div>
</template>

<script setup>
defineProps({
  product: {
    type: Object,
    required: true,
  },
  selectedSpec: {
    type: Object,
    default: null,
  },
})

const emit = defineEmits(['spec-selected'])

/**
 * 選擇規格
 */
const selectSpec = (spec) => {
  emit('spec-selected', spec)
}

/**
 * 格式化日期
 */
const formatDate = (dateString) => {
  if (!dateString) return 'N/A'
  const date = new Date(dateString)
  return `${date.getFullYear()}年${date.getMonth() + 1}月${date.getDate()}日`
}
</script>

<style scoped>
.product-info {
  background: #fff;
  padding: 30px;
  border-radius: 8px;
}

/* 品牌標籤 */
.brand-badge .badge {
  font-size: 0.75rem;
  padding: 0.35rem 0.65rem;
}

/* 商品標題 */
.product-title {
  font-size: 1.5rem;
  font-weight: 600;
  color: #333;
  line-height: 1.4;
  margin-bottom: 0.5rem;
}

.brand-name {
  font-size: 0.9rem;
}

/* 評分區域 */
.rating-section {
  display: flex;
  align-items: center;
  gap: 10px;
}

.stars {
  display: flex;
  gap: 2px;
}

.star i {
  font-size: 1rem;
}

.rating-value {
  font-weight: 600;
  color: #333;
}

.reviews-link {
  color: #0066c0;
  text-decoration: none;
}

.reviews-link:hover {
  text-decoration: underline;
}

/* 規格選擇 */
.spec-options {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.spec-button {
  flex: 1;
  min-width: 120px;
  padding: 15px;
  border: 2px solid #d0d0d0;
  border-radius: 8px;
  background: #fff;
  cursor: pointer;
  transition: all 0.3s ease;
  text-align: center;
}

.spec-button:hover:not(.disabled) {
  border-color: #f68b1e;
  box-shadow: 0 2px 8px rgba(246, 139, 30, 0.2);
}

.spec-button.active {
  border-color: #f68b1e;
  background-color: #fff7f0;
  box-shadow: 0 2px 8px rgba(246, 139, 30, 0.3);
}

.spec-button.disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.spec-name {
  font-weight: 600;
  color: #333;
  margin-bottom: 5px;
}

.spec-price {
  font-size: 1.1rem;
  color: #d32f2f;
  font-weight: 700;
}

/* 響應式設計 */
@media (max-width: 768px) {
  .product-info {
    padding: 20px;
  }

  .product-title {
    font-size: 1.25rem;
  }

  .spec-button {
    min-width: 100px;
    padding: 10px;
  }
}
</style>
