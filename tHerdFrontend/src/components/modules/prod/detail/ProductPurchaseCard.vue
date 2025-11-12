<!--
  ProductPurchaseCard.vue - 商品購買卡片組件
  功能：顯示價格、數量選擇、加入購物車、收藏與按讚功能
-->
<template>
  <div class="product-purchase-card-container">
    <div class="product-purchase-card">
      <!-- 💰 價格顯示區 -->
      <div class="price-display p-3 border-bottom">
        <div class="d-flex align-items-baseline gap-2">
          <h4 class="text-danger fw-bold mb-0">
            NT${{ formatPrice(currentPrice) }}
          </h4>
          <span v-if="unitText" class="small text-muted mt-1">/ {{ unitText }}</span>

          <!-- 折扣徽章 -->
          <span v-if="hasDiscount" class="badge bg-danger small">
            省 {{ discountPercent }}%
          </span>
        </div>

        <!-- 原價 -->
        <div>
          <span v-if="hasDiscount" class="text-muted text-decoration-line-through">
            NT${{ formatPrice(originalPrice) }}
          </span>

          <!-- 單價提示 -->
          <span v-if="unitText && hasDiscount" class="small text-muted mt-1">
            / {{ unitText }}
          </span>
        </div>
      </div>

      <!-- 🧮 數量與購買操作 -->
      <div class="action-buttons p-3">
        <!-- 數量選擇 -->
        <div class="quantity-selector mb-3">
          <label class="form-label fw-semibold">數量</label>
          <div class="quantity-box d-flex align-items-center justify-content-between">
            <button type="button" class="btn-qty" @click="decreaseQuantity">
              <i class="bi bi-dash"></i>
            </button>

            <input
              type="number"
              v-model.number="internalQuantity"
              min="1"
              class="qty-input text-center"
              @change="updateQuantity"
            />

            <button type="button" class="btn-qty" @click="increaseQuantity">
              <i class="bi bi-plus"></i>
            </button>
          </div>
        </div>

        <!-- 加入購物車 -->
        <button class="btn btn-warning btn-lg w-100 mb-3 fw-bold" @click="handleAddToCart">
          <i class="bi bi-cart-plus me-2"></i>
          加入購物車
        </button>
      </div>
    </div>

    <!-- ❤️ 收藏 & 👍 按讚 -->
    <div class="d-flex gap-2 mt-3">
      <!-- ❤️ 收藏按鈕 -->
      <button
        class="btn btn-outline-secondary flex-fill d-flex justify-content-center align-items-center gap-2"
        :disabled="togglingFavorite"
        @click="$emit('toggle-favorite', productId)"
      >
        <i :class="isFavorited ? 'bi bi-heart-fill text-danger' : 'bi bi-heart'"></i>
        <span>{{ isFavorited ? '已收藏' : '收藏' }}</span>
        <small class="text-muted">({{ favoriteCount }})</small>
      </button>

      <!-- 👍 按讚按鈕 -->
      <button
        class="btn btn-outline-primary flex-fill d-flex justify-content-center align-items-center gap-2"
        :disabled="togglingLike"
        @click="$emit('toggle-like', productId)"
      >
        <i :class="isLiked ? 'bi bi-hand-thumbs-up-fill text-primary' : 'bi bi-hand-thumbs-up'"></i>
        <span>{{ isLiked ? '已按讚' : '按讚' }}</span>
        <small class="text-muted">({{ likeCount }})</small>
      </button>
    </div>
  </div>
</template>

<script setup>
// 宣告 emits
const emit = defineEmits([
  'add-to-cart',
  'toggle-favorite',
  'toggle-like',
  'update:quantity'
])

import { ref, watch } from 'vue'

// 接收父層傳入的 props
const props = defineProps({
  currentPrice: Number,
  originalPrice: Number,
  hasDiscount: Boolean,
  discountPercent: Number,
  quantity: { type: Number, default: 1 },
  unitText: { type: String, default: '' },
  selectedSku: { type: Object, default: null },
  productId: { type: Number, required: true },
  isFavorited: { type: Boolean, default: false },
  favoriteCount: { type: Number, default: 0 }, // ❤️ 收藏數
  togglingFavorite: { type: Boolean, default: false },
  isLiked: { type: Boolean, default: false },
  togglingLike: { type: Boolean, default: false },
  likeCount: { type: Number, default: 0 } // 👍 按讚數
})

// 數量內部綁定
const internalQuantity = ref(props.quantity)

watch(
  () => props.quantity,
  (newVal) => {
    internalQuantity.value = newVal
  }
)

// 格式化金額
const formatPrice = (price) => {
  if (price == null) return '-'
  return price.toLocaleString('zh-TW', { minimumFractionDigits: 0 })
}

// 增減數量
const increaseQuantity = () => {
  internalQuantity.value++
  updateQuantity()
}

const decreaseQuantity = () => {
  if (internalQuantity.value > 1) {
    internalQuantity.value--
    updateQuantity()
  }
}

const updateQuantity = () => {
  if (internalQuantity.value < 1) internalQuantity.value = 1
  emit('update:quantity', internalQuantity.value)
}

// ✅ 加入購物車事件
const handleAddToCart = () => {
  if (!props.selectedSku) {
    console.warn('請選擇規格')
    return
  }
  emit('add-to-cart', props.selectedSku, internalQuantity.value)
}
</script>

<style scoped>
.product-purchase-card-container {
  position: sticky;
  top: 20px;
  width: 300px !important;
}

.product-purchase-card {
  background: #fff;
  border-radius: 8px;
  border: 1px solid #ccc;
}

.price-display {
  background-color: #f8f9fa;
}

.badge.small {
  font-size: 0.7rem;
}

/* 數量選擇器 */
.quantity-selector .input-group {
  border: 1px solid #dee2e6;
  border-radius: 4px;
  overflow: hidden;
}

.quantity-selector .btn {
  border: none;
  background-color: #f8f9fa;
}

.quantity-selector .btn:hover {
  background-color: #e9ecef;
}

.btn-warning {
  background-color: #f68b1e;
  border-color: #f68b1e;
}

.btn-warning:hover {
  background-color: #e57a0d;
}

/* === 數量選擇器（修正版）=== */
.quantity-box {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  min-width: 160px;
  border: 1px solid #ccc;
  border-radius: 6px;
  overflow: hidden;
  background-color: #fff;
}

/* ➖ / ➕ 按鈕 */
.btn-qty {
  flex: 0 0 auto;
  width: 48px;
  height: 48px;
  background-color: #f3f8f5;
  border: none;
  color: #4f7d6f;
  font-size: 1.4rem;
  font-weight: bold;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
  cursor: pointer;
}

.btn-qty:hover {
  background-color: #d8efe4;
  color: #2f5b4b;
}

/* 中間輸入框 */
.qty-input {
  flex: 1;
  min-width: 60px;
  height: 48px;
  border: none;
  font-size: 1.1rem;
  font-weight: 600;
  text-align: center;
  background-color: #fff;
}

.qty-input::-webkit-outer-spin-button,
.qty-input::-webkit-inner-spin-button {
  -webkit-appearance: none;
  margin: 0;
}

.qty-input:focus {
  outline: none;
  box-shadow: none;
}

/* 💙 按讚按鈕樣式 */
.btn-outline-primary {
  border-color: #0d6efd;
  color: #0d6efd;
}

.btn-outline-primary:hover {
  background-color: #0d6efd;
  color: #fff;
}

/* 收藏 + 按讚 按鈕區塊 */
.d-flex.gap-2.mt-3 button {
  height: 48px;
  font-weight: 500;
}

.btn-outline-secondary {
  color: #6c757d;
  border-color: #6c757d;
}

.btn-outline-secondary:hover {
  background-color: #6c757d;
  color: white;
}

.btn-outline-primary {
  color: #0d6efd;
  border-color: #0d6efd;
}

.btn-outline-primary:hover {
  background-color: #0d6efd;
  color: white;
}
</style>