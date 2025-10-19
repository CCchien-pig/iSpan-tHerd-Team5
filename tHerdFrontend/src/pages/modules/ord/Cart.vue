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
                <div class="col-md-4 col-9">
                  <h6 class="mb-2 fw-bold">{{ item.productName }}</h6>
                  <p class="text-muted mb-2">
                    <small><i class="bi bi-tag"></i> 規格: {{ item.optionName }}</small>
                  </p>
                  <div class="d-flex align-items-center gap-2">
                    <span class="text-danger fw-bold">${{ item.salePrice.toLocaleString() }}</span>
                    <span v-if="item.unitPrice > item.salePrice" class="text-muted text-decoration-line-through">
                      <small>${{ item.unitPrice.toLocaleString() }}</small>
                    </span>
                  </div>
                </div>

                <!-- 數量調整（置中對齊） -->
                <div class="col-md-3 col-6 d-flex justify-content-center">
                  <div class="quantity-control">
                    <button 
                      class="quantity-btn quantity-minus" 
                      type="button" 
                      @click="decreaseQuantity(item)">
                      -
                    </button>
                    <input
                      type="text"
                      class="quantity-input"
                      :value="item.quantity"
                      readonly
                    />
                    <button 
                      class="quantity-btn quantity-plus" 
                      type="button" 
                      @click="increaseQuantity(item)">
                      +
                    </button>
                  </div>
                </div>

                <!-- 價格與刪除 -->
                <div class="col-md-3 col-6 text-end">
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
          <!-- 優惠券折扣碼 -->
          <div class="card shadow-sm border-0 mb-3 coupon-card">
            <div class="card-body p-4">
              <h6 class="mb-3 d-flex align-items-center">
                <i class="bi bi-ticket-perforated-fill text-warning me-2" style="font-size: 1.2rem;"></i>
                <span class="fw-bold">優惠券折扣碼</span>
              </h6>
              <div class="input-group mb-2">
                <input
                  type="text"
                  class="form-control"
                  placeholder="請輸入優惠碼"
                  v-model="couponCode"
                  :disabled="isCouponApplied"
                  @keyup.enter="applyCoupon"
                />
                <button
                  class="btn main-color-green"
                  type="button"
                  @click="applyCoupon"
                  :disabled="!couponCode || isCouponApplied || isProcessing"
                >
                  <i class="bi bi-check-circle me-1"></i>
                  <span class="main-color-white-text">{{ isCouponApplied ? '已套用' : '套用' }}</span>
                </button>
              </div>
              
              <!-- 優惠券套用成功訊息 -->
              <div v-if="isCouponApplied" class="alert alert-success d-flex align-items-center mb-0 py-2 px-3">
                <i class="bi bi-check-circle-fill me-2"></i>
                <small class="flex-grow-1">
                  優惠券已套用: <strong>{{ couponCode }}</strong>
                </small>
                <button
                  type="button"
                  class="btn-close btn-close-sm"
                  @click="removeCoupon"
                  aria-label="移除優惠券"
                ></button>
              </div>
              
              <!-- 錯誤訊息 -->
              <div v-if="couponError" class="alert alert-danger d-flex align-items-center mb-0 py-2 px-3 mt-2">
                <i class="bi bi-exclamation-triangle-fill me-2"></i>
                <small>{{ couponError }}</small>
              </div>
            </div>
          </div>

          <!-- 購物車摘要 -->
          <div class="card shadow-sm border-0">
            <div class="card-header main-color-green text-white">
              <h5 class="mb-0">
                <i class="bi bi-receipt"></i> 購物車摘要
              </h5>
            </div>
            <div class="card-body p-4">
              <!-- 商品件數 -->
              <div class="d-flex justify-content-between mb-3">
                <span class="text-muted">商品件數:</span>
                <span class="fw-bold">{{ totalItems }} 件</span>
              </div>

              <!-- 商品小計 -->
              <div class="d-flex justify-content-between mb-3">
                <span class="text-muted">商品小計:</span>
                <span class="fw-bold fs-5">${{ subtotalBeforeDiscount.toLocaleString() }}</span>
              </div>

              <!-- 優惠折扣 -->
              <div v-if="totalDiscount > 0" class="d-flex justify-content-between mb-3">
                <span class="text-danger">
                  <i class="bi bi-tag-fill me-1"></i>優惠折扣:
                </span>
                <span class="text-danger fw-bold fs-5">-${{ totalDiscount.toLocaleString() }}</span>
              </div>

              <!-- 運費 -->
              <div class="d-flex justify-content-between mb-3 pb-3 border-bottom">
                <span class="text-muted">運費:</span>
                <span class="text-success fw-bold">免運費</span>
              </div>

              <!-- 總計 -->
              <div class="d-flex justify-content-between align-items-center mb-4">
                <h5 class="mb-0">總計:</h5>
                <div class="text-end">
                  <h3 class="mb-0 main-color-green-text fw-bold">
                    ${{ finalTotal.toLocaleString() }}
                  </h3>
                  <small v-if="totalDiscount > 0" class="text-muted">
                    <del>${{ subtotalBeforeDiscount.toLocaleString() }}</del>
                  </small>
                </div>
              </div>

              <!-- 前往結帳按鈕 -->
              <button
                class="btn main-color-green btn-lg w-100 mb-3 text-white"
                @click="checkout"
                :disabled="isProcessing"
              >
                <i class="bi bi-credit-card me-2"></i>
                <span v-if="isProcessing">
                  <span class="spinner-border spinner-border-sm me-2"></span>
                  處理中...
                </span>
                <span v-else>前往結帳</span>
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
    <div class="modal fade" id="errorModal" tabindex="-1" ref="errorModal">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header bg-danger text-white">
            <h5 class="modal-title">
              <i class="bi bi-exclamation-triangle"></i> 結帳失敗
            </h5>
            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
          </div>
          <div class="modal-body">
            <p>{{ errorMessage }}</p>
            <div v-if="checkoutErrors.length > 0">
              <h6 class="fw-bold">商品問題:</h6>
              <ul class="list-group">
                <li v-for="(error, index) in checkoutErrors" :key="index" class="list-group-item">
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
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">關閉</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { Modal } from 'bootstrap';

export default {
  name: 'CartComponent',
  data() {
    return {
      cartItems: [
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
          productName: "Optimum Nutrition, Opti-Women®，針對活躍女性的多維生素，60 粒膠囊",
          optionName: "60 粒",
          unitPrice: 800.00,
          salePrice: 656.00,
          quantity: 1,
          subtotal: 656.00
        },
        {
          productId: 14600,
          skuId: 3387,
          productName: "Optimum Nutrition, Opti-Women®，針對活躍女性的多維生素，120 粒膠囊",
          optionName: "120 粒",
          unitPrice: 1300.00,
          salePrice: 1188.00,
          quantity: 1,
          subtotal: 1188.00
        }
      ],
      couponCode: '',
      isCouponApplied: false,
      couponDiscountAmount: 0,
      couponError: '',
      isProcessing: false,
      errorMessage: '',
      checkoutErrors: [],
      errorModal: null
    };
  },
  computed: {
    subtotalBeforeDiscount() {
      return this.cartItems.reduce((sum, item) => {
        return sum + (item.unitPrice * item.quantity);
      }, 0);
    },
    subtotal() {
      return this.cartItems.reduce((sum, item) => {
        return sum + item.subtotal;
      }, 0);
    },
    productDiscount() {
      return this.cartItems.reduce((sum, item) => {
        return sum + ((item.unitPrice - item.salePrice) * item.quantity);
      }, 0);
    },
    totalDiscount() {
      return this.productDiscount + this.couponDiscountAmount;
    },
    totalItems() {
      return this.cartItems.reduce((sum, item) => sum + item.quantity, 0);
    },
    finalTotal() {
      const total = this.subtotal - this.couponDiscountAmount;
      return Math.max(0, total);
    }
  },
  mounted() {
    if (this.$refs.errorModal) {
      this.errorModal = new Modal(this.$refs.errorModal);
    }
  },
  methods: {
    updateQuantity(item) {
      if (item.quantity < 1) item.quantity = 1;
      if (item.quantity > 99) item.quantity = 99;
      item.subtotal = item.salePrice * item.quantity;
    },

    increaseQuantity(item) {
      if (item.quantity < 99) {
        item.quantity++;
        this.updateQuantity(item);
      }
    },

    decreaseQuantity(item) {
      // 數量為 1 時按減號不做任何動作
      if (item.quantity > 1) {
        item.quantity--;
        this.updateQuantity(item);
      }
    },

    removeItem(item) {
      if (confirm(`確定要移除「${item.productName}」嗎？`)) {
        const index = this.cartItems.findIndex(
          (i) => i.productId === item.productId && i.skuId === item.skuId
        );
        if (index > -1) {
          this.cartItems.splice(index, 1);
        }
      }
    },

    async applyCoupon() {
      if (!this.couponCode.trim()) {
        this.couponError = '請輸入優惠碼';
        return;
      }

      this.isProcessing = true;
      this.couponError = '';

      try {
        if (this.couponCode.toUpperCase() === 'SAVE100') {
          this.couponDiscountAmount = 100;
          this.isCouponApplied = true;
          this.couponError = '';
          this.isProcessing = false;
          return;
        }

        const response = await fetch('/api/ord/Cart/validate-coupon', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            couponCode: this.couponCode,
            subtotal: this.subtotal
          })
        });

        const result = await response.json();

        if (result.success) {
          this.couponDiscountAmount = result.data.discountAmount;
          this.isCouponApplied = true;
          this.couponError = '';
        } else {
          this.couponError = result.message || '優惠券無效';
          this.couponDiscountAmount = 0;
          this.isCouponApplied = false;
        }
      } catch (error) {
        console.error('驗證優惠券失敗:', error);
        this.couponError = '系統發生錯誤，請稍後再試';
      } finally {
        this.isProcessing = false;
      }
    },

    removeCoupon() {
      this.couponCode = '';
      this.couponDiscountAmount = 0;
      this.isCouponApplied = false;
      this.couponError = '';
    },

    async checkout() {
      if (this.isProcessing) return;
      
      this.isProcessing = true;
      this.errorMessage = '';
      this.checkoutErrors = [];

      try {
        const response = await fetch('/api/ord/Cart/checkout', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            sessionId: 'test-session',
            userNumberId: null,
            cartItems: this.cartItems.map(item => ({
              productId: item.productId,
              skuId: item.skuId,
              productName: item.productName,
              optionName: item.optionName,
              salePrice: item.salePrice,
              quantity: item.quantity
            })),
            couponCode: this.isCouponApplied ? this.couponCode : null,
            discountAmount: this.couponDiscountAmount
          })
        });

        const result = await response.json();

        if (result.success) {
          alert(`結帳成功!\n訂單編號: ${result.orderNo}\n訂單金額: $${result.totalAmount.toLocaleString()}`);
          this.cartItems = [];
          this.removeCoupon();
        } else {
          this.errorMessage = result.message || '結帳時發生錯誤';
          
          if (result.errors && result.errors.length > 0) {
            this.checkoutErrors = result.errors;
          }
          
          if (this.errorModal) {
            this.errorModal.show();
          }
        }
      } catch (error) {
        console.error('結帳錯誤:', error);
        alert('系統發生錯誤,請稍後再試');
      } finally {
        this.isProcessing = false;
      }
    },

    continueShopping() {
      window.location.href = '/';
    },

    goToProducts() {
      window.location.href = '/';
    },

    getProductImage(productId) {
      return 'https://via.placeholder.com/150x150/007083/FFFFFF?text=Product';
    },

    handleImageError(event) {
      if (!event.target.dataset.errorHandled) {
        event.target.dataset.errorHandled = 'true';
        event.target.src = 'https://via.placeholder.com/150x150/cccccc/666666?text=No+Image';
      }
    }
  }
};
</script>

<style scoped>
.coupon-card {
  border: 2px dashed #ffc107 !important;
}

.main-color-green {
  background-color: #007083;
  border-color: #007083;
}

.main-color-green:hover {
  background-color: #005a6a;
  border-color: #005a6a;
}

.main-color-green-text {
  color: #007083;
}

.main-color-white-text {
  color: white;
}

.btn-close-sm {
  font-size: 0.7rem;
  padding: 0.25rem;
}

.teal-reflect-button {
  background-color: #007083;
  color: white;
  border: none;
  padding: 10px 30px;
}

.teal-reflect-button:hover {
  background-color: #005a6a;
  color: white;
}

.silver-reflect-button {
  background-color: #6c757d;
  color: white;
  border: none;
}

.silver-reflect-button:hover {
  background-color: #5a6268;
  color: white;
}

/* 數量控制器樣式（模擬購物車風格） */
.quantity-control {
  display: inline-flex;
  align-items: center;
  border: 1px solid #dee2e6;
  border-radius: 4px;
  overflow: hidden;
}

.quantity-btn {
  width: 40px;
  height: 40px;
  border: none;
  background-color: white;
  color: #6c757d;
  font-size: 1.2rem;
  font-weight: bold;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
}

.quantity-btn:hover {
  background-color: #f8f9fa;
  color: #495057;
}

.quantity-btn:active {
  background-color: #e9ecef;
}

.quantity-minus {
  border-right: 1px solid #dee2e6;
}

.quantity-plus {
  border-left: 1px solid #dee2e6;
}

.quantity-input {
  width: 60px;
  height: 40px;
  border: none;
  text-align: center;
  font-weight: bold;
  font-size: 1rem;
  background-color: white;
  color: #212529;
}

.quantity-input:focus {
  outline: none;
}
</style>