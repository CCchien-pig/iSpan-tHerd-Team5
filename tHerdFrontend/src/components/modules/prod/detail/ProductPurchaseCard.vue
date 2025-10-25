<!--
  ProductPurchaseCard.vue - 商品購買卡片組件
  功能：顯示價格、數量選擇、加入購物車等購買相關功能
-->
<template>
  <div class="product-purchase-card-container">
    <div class="product-purchase-card">
      <!-- 自動訂貨區域 -->
      <!-- 如果有自動訂貨需求 -->
      <!-- <div class="price-section p-3">
      <div class="d-flex align-items-baseline gap-3">
        <div class="current-price">NT${{ currentPrice }}</div>
        <div v-if="hasDiscount" class="discount-badge">
          <span class="badge bg-danger">省 {{ discountPercent }}%</span>
        </div>
      </div>
      <div v-if="hasDiscount" class="original-price">NT${{ originalPrice }}</div>
      <div class="price-note mt-2 text-muted small">NT${{ originalPrice }} /件單價</div>
    </div> -->

      <!-- 一次性購買區域 -->
      <div class="auto-delivery-section p-3">
        <!-- <div class="form-check">
          <input
            class="form-check-input"
            type="checkbox"
            id="autoDelivery"
            v-model="autoDelivery"
          />
          <label class="form-check-label" for="autoDelivery">
            <strong>一次性購買：</strong>
          </label>
        </div> -->
        <div class="price-display mt-2">
          <span class="fs-4 fw-bold text-danger">NT${{ currentPrice }}</span>
        </div>
        <span class="text-muted">NT${{ originalPrice }} /件單價</span>
      </div>

      <!-- 操作按鈕區 -->
      <div class="action-buttons p-3">
        <!-- 數量選擇 -->
        <div class="quantity-selector mb-3">
          <label class="form-label">數量</label>
          <div class="input-group" style="max-width: 150px">
            <button class="btn btn-outline-secondary" type="button" @click="decreaseQuantity">
              <i class="bi bi-dash"></i>
            </button>
            <input
              type="number"
              class="form-control text-center"
              v-model.number="internalQuantity"
              min="1"
              @change="updateQuantity"
            />
            <button class="btn btn-outline-secondary" type="button" @click="increaseQuantity">
              <i class="bi bi-plus"></i>
            </button>
          </div>
        </div>

        <!-- 加入購物車按鈕 -->
        <button class="btn btn-warning btn-lg w-100 mb-3 fw-bold" @click="handleAddToCart">
          <i class="bi bi-cart-plus me-2"></i>
          加入購物車
        </button>
      </div>
    </div>

    <!-- 收藏與分享 -->
    <button class="btn btn-outline-secondary mt-3 w-100" @click="$emit('toggle-favorite')">
      <i class="bi bi-heart"></i>
      加到願望清單
    </button>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'

const props = defineProps({
  currentPrice: {
    type: Number,
    required: true,
  },
  originalPrice: {
    type: Number,
    required: true,
  },
  hasDiscount: {
    type: Boolean,
    default: false,
  },
  discountPercent: {
    type: Number,
    default: 0,
  },
  quantity: {
    type: Number,
    default: 1,
  },
})

const emit = defineEmits(['add-to-cart', 'toggle-favorite', 'toggle-like', 'update:quantity'])

// 內部數量狀態
const internalQuantity = ref(props.quantity)
const autoDelivery = ref(false)

// 監聽外部數量變化
watch(
  () => props.quantity,
  (newVal) => {
    internalQuantity.value = newVal
  }
)

/**
 * 增加數量
 */
const increaseQuantity = () => {
  internalQuantity.value++
  updateQuantity()
}

/**
 * 減少數量
 */
const decreaseQuantity = () => {
  if (internalQuantity.value > 1) {
    internalQuantity.value--
    updateQuantity()
  }
}

/**
 * 更新數量
 */
const updateQuantity = () => {
  if (internalQuantity.value < 1) {
    internalQuantity.value = 1
  }
  emit('update:quantity', internalQuantity.value)
}

/**
 * 處理加入購物車
 */
const handleAddToCart = () => {
  emit('add-to-cart', {
    quantity: internalQuantity.value,
    autoDelivery: autoDelivery.value,
  })
}
</script>

<style scoped>
.product-purchase-card-container {
  position: sticky;
  top: 20px;
}

.product-purchase-card {
  background: #fff;
  border-radius: 8px;
  border: 1px solid #ccc;
}

/* 價格區域 */
.price-section {
  background-color: #f8f9fa;
  border: 1px solid #dee2e6;
}

.current-price {
  font-size: 1rem;
  font-weight: 700;
  color: #d32f2f;
}

.discount-badge .badge {
  font-size: 0.6rem;
}

.original-price {
  font-size: 0.8rem;
  color: #999;
  text-decoration: line-through;
}

.price-note {
  font-size: 0.6rem;
}

/* 自動補貨區域 */
.auto-delivery-section {
}

.auto-delivery-section .form-check-input:checked {
  background-color: #28a745;
  border-color: #28a745;
}

/* 數量選擇器 */
.quantity-selector {
  margin-bottom: 1rem;
}

.quantity-selector .form-label {
  font-weight: 600;
  margin-bottom: 0.5rem;
}

.quantity-selector .input-group {
  border: 1px solid #dee2e6;
  border-radius: 4px;
  overflow: hidden;
}

.quantity-selector .btn {
  border: none;
  background-color: #f8f9fa;
  color: #495057;
  padding: 0.5rem 0.75rem;
}

.quantity-selector .btn:hover {
  background-color: #e9ecef;
}

.quantity-selector .form-control {
  border: none;
  border-left: 1px solid #dee2e6;
  border-right: 1px solid #dee2e6;
  padding: 0.5rem;
}

/* 操作按鈕 */
.btn-warning {
  background-color: #f68b1e;
  border-color: #f68b1e;
  color: #fff;
  font-size: 1.1rem;
  padding: 0.75rem;
}

.btn-warning:hover {
  background-color: #e57a0d;
  border-color: #e57a0d;
}

.btn-outline-secondary {
  color: #6c757d;
  border-color: #6c757d;
}

.btn-outline-secondary:hover {
  background-color: #6c757d;
  color: #fff;
}

/* 響應式設計 */
@media (max-width: 768px) {
  .product-purchase-card {
    position: static;
    margin-top: 1rem;
  }

  .current-price {
    font-size: 1.5rem;
  }

  .btn-warning {
    font-size: 1rem;
  }
}
</style>
