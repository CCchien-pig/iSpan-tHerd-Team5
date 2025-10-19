<template>
  <div class="container my-5">
    <!-- 購物車標題 -->
    <div class="d-flex justify-content-between align-items-center mb-4 pb-3 border-bottom">
      <h2 class="main-color-green-text fw-bold">
        <i class="bi bi-cart3"></i> 購物車
      </h2>
      <span class="badge bg-danger rounded-pill fs-6">{{ cartItems.length }} 項商品</span>
    </div>

    <!-- 購物車為空 -->
    <div v-if="cartItems.length === 0" class="text-center py-5">
      <i class="bi bi-cart-x text-muted" style="font-size: 5rem;"></i>
      <p class="mt-4 fs-5 text-muted">您的購物車是空的</p>
      <button class="btn teal-reflect-button mt-3" @click="goToProducts">
        <i class="bi bi-shop"></i> 前往購物
      </button>
    </div>

    <!-- 購物車內容 -->
    <div v-else>
      <div class="row g-4">
        <!-- 左側：購物車商品列表 -->
        <div class="col-lg-8">
          <div class="card shadow-sm border-0 mb-3" v-for="item in cartItems" :key="`${item.productId}-${item.skuId}`">
            <div class="card-body p-4">
              <div class="row align-items-center g-3">
                <!-- 商品圖片 -->
                <div class="col-md-2 col-3">
                  <img
                    :src="getProductImage(item.productId)"
                    :alt="item.productName"
                    class="img-fluid rounded"
                    @error="handleImageError"
                  />
                </div>

                <!-- 商品資訊 -->
                <div class="col-md-5 col-9">
                  <h5 class="mb-2 fw-bold">{{ item.productName }}</h5>
                  <p class="text-muted mb-2">
                    <small><i class="bi bi-tag"></i> 規格: {{ item.optionName }}</small>
                  </p>
                  <div class="price-info">
                    <span class="text-danger fw-bold fs-5">${{ item.salePrice.toLocaleString() }}</span>
                    <span
                      v-if="item.unitPrice > item.salePrice"
                      class="text-muted text-decoration-line-through ms-2"
                    >
                      ${{ item.unitPrice.toLocaleString() }}
                    </span>
                  </div>
                </div>

                <!-- 數量調整 -->
                <div class="col-md-3 col-6 mt-3 mt-md-0">
                  <div class="input-group">
                    <button
                      class="btn btn-outline-secondary"
                      type="button"
                      @click="decreaseQuantity(item)"
                      :disabled="item.quantity <= 1"
                    >
                      <i class="bi bi-dash"></i>
                    </button>
                    <input
                      type="number"
                      class="form-control text-center"
                      v-model.number="item.quantity"
                      @change="updateQuantity(item)"
                      min="1"
                      max="99"
                      style="max-width: 70px;"
                    />
                    <button
                      class="btn btn-outline-secondary"
                      type="button"
                      @click="increaseQuantity(item)"
                      :disabled="item.quantity >= 99"
                    >
                      <i class="bi bi-plus"></i>
                    </button>
                  </div>
                </div>

                <!-- 小計與刪除 -->
                <div class="col-md-2 col-6 mt-3 mt-md-0 text-end">
                  <div class="fw-bold main-color-green-text fs-5 mb-2">
                    ${{ item.subtotal.toLocaleString() }}
                  </div>
                  <button
                    class="btn btn-sm btn-outline-danger"
                    @click="removeItem(item)"
                    title="移除商品"
                  >
                    <i class="bi bi-trash"></i> 移除
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 右側：結帳摘要 -->
        <div class="col-lg-4">
          <div class="card shadow border-0 position-sticky" style="top: 20px;">
            <div class="card-header main-color-green">
              <h5 class="mb-0 main-color-white-text">
                <i class="bi bi-receipt"></i> 購物車摘要
              </h5>
            </div>
            <div class="card-body p-4">
              <div class="d-flex justify-content-between mb-3">
                <span class="text-muted">商品件數:</span>
                <span class="fw-bold">{{ totalItems }} 件</span>
              </div>
              <div class="d-flex justify-content-between mb-3">
                <span class="text-muted">商品小計:</span>
                <span class="fw-bold">${{ totalAmount.toLocaleString() }}</span>
              </div>
              <div class="d-flex justify-content-between mb-3">
                <span class="text-muted">運費:</span>
                <span class="text-success fw-bold">免運費</span>
              </div>
              <hr class="my-3" />
              <div class="d-flex justify-content-between mb-4">
                <span class="fs-5 fw-bold">總計:</span>
                <span class="fs-4 fw-bold text-danger">${{ totalAmount.toLocaleString() }}</span>
              </div>
              
              <button
                class="btn teal-reflect-button w-100 mb-2 py-3"
                @click="checkout"
                :disabled="isProcessing"
              >
                <i class="bi bi-credit-card me-2"></i>
                {{ isProcessing ? '處理中...' : '前往結帳' }}
              </button>
              
              <button
                class="btn silver-reflect-button w-100"
                @click="continueShopping"
              >
                <i class="bi bi-arrow-left me-2"></i> 繼續購物
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 錯誤訊息 Modal -->
    <div
      class="modal fade"
      id="errorModal"
      tabindex="-1"
      ref="errorModal"
    >
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header bg-danger text-white">
            <h5 class="modal-title">
              <i class="bi bi-exclamation-triangle"></i> 結帳失敗
            </h5>
            <button
              type="button"
              class="btn-close btn-close-white"
              data-bs-dismiss="modal"
            ></button>
          </div>
          <div class="modal-body">
            <p>{{ errorMessage }}</p>
            <div v-if="checkoutErrors.length > 0">
              <h6 class="fw-bold">商品問題:</h6>
              <ul class="list-group">
                <li
                  v-for="(error, index) in checkoutErrors"
                  :key="index"
                  class="list-group-item"
                >
                  <strong>{{ error.productName }}</strong>
                  <span v-if="error.optionName"> - {{ error.optionName }}</span>
                  <br />
                  <span class="text-danger">{{ error.reason }}</span>
                  <span v-if="error.currentStock !== null">
                    (目前庫存: {{ error.currentStock }})
                  </span>
                </li>
              </ul>
            </div>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
              關閉
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { Modal } from 'bootstrap';

export default {
  name: 'Cart',
  data() {
    return {
      cartItems: [],
      isProcessing: false,
      errorMessage: '',
      checkoutErrors: [],
      errorModal: null
    };
  },
  computed: {
    totalItems() {
      return this.cartItems.reduce((sum, item) => sum + item.quantity, 0);
    },
    totalAmount() {
      return this.cartItems.reduce((sum, item) => sum + item.subtotal, 0);
    }
  },
  mounted() {
    this.loadCart();
    this.errorModal = new Modal(this.$refs.errorModal);
  },
  methods: {
    async loadCart() {
      // 🔥 暫時使用寫死的 Mock 資料,避免 API 串接問題
      this.cartItems = [
        {
          productId: 14246,
          skuId: 2680,
          productName: "Lake Avenue Nutrition, Omega-3 魚油，30 粒魚明膠軟膠囊（每粒軟膠囊 1,250 毫克）",
          optionName: "30 單位",
          unitPrice: 500.00,
          salePrice: 346.00,
          quantity: 1,
          subtotal: 346.00
        },
        {
          productId: 14246,
          skuId: 3388,
          productName: "Lake Avenue Nutrition, Omega-3 魚油，90 粒魚明膠軟膠囊（每粒軟膠囊 1,250 毫克）",
          optionName: "90 單位",
          unitPrice: 1000.00,
          salePrice: 898.00,
          quantity: 1,
          subtotal: 898.00
        },
        {
          productId: 14600,
          skuId: 2869,
          productName: "Optimum Nutrition, Opti-Women®，針對活躍 女性的多維生素，60 粒膠囊",
          optionName: "60 粒",
          unitPrice: 800.00,
          salePrice: 656.00,
          quantity: 1,
          subtotal: 656.00
        },
        {
          productId: 14600,
          skuId: 3387,
          productName: "Optimum Nutrition, Opti-Women®，針對活躍 女性的多維生素，120 粒膠囊",
          optionName: "120 粒",
          unitPrice: 1300.00,
          salePrice: 1188.00,
          quantity: 1,
          subtotal: 1188.00
        }
      ];
      
      console.log('✅ 購物車資料載入成功 (Mock):', this.cartItems);
      
      /* 🔧 之後要串接真實 API 時,取消註解這段:
      try {
        const response = await fetch('/api/ord/Cart/items');
        if (!response.ok) throw new Error('載入購物車失敗');
        
        const data = await response.json();
        this.cartItems = data.map(item => ({
          ...item,
          subtotal: item.salePrice * item.quantity
        }));
      } catch (error) {
        console.error('載入購物車錯誤:', error);
      }
      */
    },

    increaseQuantity(item) {
      if (item.quantity < 99) {
        item.quantity++;
        this.updateItemSubtotal(item);
      }
    },

    decreaseQuantity(item) {
      if (item.quantity > 1) {
        item.quantity--;
        this.updateItemSubtotal(item);
      }
    },

    updateQuantity(item) {
      if (item.quantity < 1) {
        item.quantity = 1;
      } else if (item.quantity > 99) {
        item.quantity = 99;
      }
      this.updateItemSubtotal(item);
    },

    updateItemSubtotal(item) {
      item.subtotal = item.salePrice * item.quantity;
    },

    async removeItem(item) {
      if (confirm(`確定要從購物車移除 ${item.productName}?`)) {
        const index = this.cartItems.findIndex(
          i => i.productId === item.productId && i.skuId === item.skuId
        );
        if (index > -1) {
          this.cartItems.splice(index, 1);
        }
      }
    },

    async checkout() {
      if (this.isProcessing) return;
      
      this.isProcessing = true;
      this.errorMessage = '';
      this.checkoutErrors = [];

      try {
        const response = await fetch('/ORD/CartTest/Checkout', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            cartItems: this.cartItems
          })
        });

        const result = await response.json();

        if (result.success) {
          alert(`結帳成功!\n訂單編號: ${result.orderNo}\n訂單金額: $${result.totalAmount.toLocaleString()}`);
          this.cartItems = [];
          this.$router.push(`/order/${result.orderNo}`);
        } else {
          this.errorMessage = result.message || '結帳時發生錯誤';
          
          if (result.errors && result.errors.length > 0) {
            this.checkoutErrors = result.errors;
          }
          
          this.errorModal.show();
        }
      } catch (error) {
        console.error('結帳錯誤:', error);
        alert('系統發生錯誤,請稍後再試');
      } finally {
        this.isProcessing = false;
      }
    },

    continueShopping() {
      this.$router.push('/products');
    },

    goToProducts() {
      this.$router.push('/products');
    },

    getProductImage(productId) {
      return `/images/products/${productId}.jpg`;
    },

    handleImageError(event) {
      event.target.src = '/images/products/placeholder.jpg';
    }
  }
};
</script>

<style scoped>
/* 使用 main.css 的公用樣式,這裡只加入購物車特定樣式 */

.card {
  transition: all 0.3s ease;
}

.card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15) !important;
}

/* 數量控制按鈕樣式調整 */
.input-group .btn-outline-secondary {
  border-color: rgb(0, 112, 131);
  color: rgb(0, 112, 131);
}

.input-group .btn-outline-secondary:hover:not(:disabled) {
  background-color: rgb(0, 112, 131);
  color: white;
}

.input-group .btn-outline-secondary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* 響應式調整 */
@media (max-width: 768px) {
  .card-body {
    padding: 1rem !important;
  }
  
  .input-group {
    max-width: 100%;
  }
}
</style>