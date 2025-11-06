<!--
  ProductPurchaseCard.vue - 商品購買卡片組件
  功能：顯示價格、數量選擇、加入購物車等購買相關功能
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
          <span v-if="unitText" class="small text-muted mt-1">
             / {{ unitText }}
          </span>

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

        <!-- 加入購物車 -->
        <button class="btn btn-warning btn-lg w-100 mb-3 fw-bold" @click="handleAddToCart">
          <i class="bi bi-cart-plus me-2"></i>
          加入購物車
        </button>
      </div>
    </div>

    <!-- ❤️ 收藏 -->
    <button class="btn btn-outline-secondary mt-3 w-100" @click="$emit('toggle-favorite')">
      <i class="bi bi-heart"></i> 加到願望清單
    </button>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'

// 接收父層傳入的 props
const props = defineProps({
  currentPrice: Number,
  originalPrice: Number,
  hasDiscount: Boolean,
  discountPercent: Number,
  quantity: {
    type: Number,
    default: 1,
  },
  unitText: {
    type: String,
    default: '', // 例如「瓶」、「包」、「盒」
  },
  // 新增：接收父層傳入的已選規格（selectedSpec / selectedSku）
  selectedSku: {
    type: Object,
    default: null
  }
})

// 宣告 emits
const emit = defineEmits(['add-to-cart', 'toggle-favorite', 'toggle-like', 'update:quantity'])

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

// ✅ 正確 emit
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
</style>
