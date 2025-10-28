<template>
  <div class="container my-5">
    <div class="d-flex align-items-center mb-4">
      <i class="bi bi-cart3 me-3" style="font-size: 2rem"></i>
      <h2 class="fw-bold text-teal mb-0">購物車測試</h2>
    </div>

    <div class="row g-4">
      <!-- 左:商品明細 -->
      <div class="col-lg-8">
        <div
          v-for="item in cartItems"
          :key="`${item.productId}-${item.skuId}`"
          class="product-card p-4 mb-3 d-flex justify-content-between align-items-center"
        >
          <div class="flex-grow-1 pe-3">
            <h5 class="fw-bold mb-1">{{ item.productName }}</h5>
            <p class="text-muted mb-1"><i class="bi bi-tag"></i> 規格:{{ item.skuName }}</p>
            <div class="text-muted text-decoration-line-through small">
              NT$ {{ item.unitPrice.toLocaleString() }}
            </div>
            <div class="text-danger fw-bold fs-5 mb-0">
              NT$ {{ item.salePrice.toLocaleString() }}
            </div>
          </div>

          <div class="text-end d-flex align-items-center gap-3">
            <div class="quantity-row">
              <button
                class="circle-btn"
                @click="decreaseOnce(item)"
                :disabled="isCheckingOut"
                :title="item.quantity === 1 ? '刪除商品' : '減少數量'"
              >-</button>
              <input type="text" class="qty-input" :value="item.quantity" readonly />
              <button
                class="circle-btn"
                @click="increaseQuantity(item)"
                :disabled="isCheckingOut"
                title="增加數量"
              >+</button>
            </div>

            <div class="text-teal fw-bold fs-5" style="min-width: 96px;">
              NT$ {{ item.subtotal.toLocaleString() }}
            </div>

            <button 
              class="btn btn-outline-danger btn-sm" 
              @click="confirmRemove(item)" 
              :disabled="isCheckingOut"
              title="移除商品"
            >
              <i class="bi bi-trash"></i>
            </button>
          </div>
        </div>

        <!-- 購物車空狀態 -->
        <div v-if="cartItems.length === 0" class="text-center py-5">
          <i class="bi bi-cart-x" style="font-size: 4rem; color: #ccc;"></i>
          <h4 class="mt-3 text-muted">購物車是空的</h4>
          <button class="btn btn-primary mt-3" @click="continueShopping">
            <i class="bi bi-arrow-left"></i> 繼續購物
          </button>
        </div>
      </div>

      <!-- 右:訂單摘要 -->
      <div class="col-lg-4">
        <div class="card shadow-sm border-0 sticky-top p-4" style="top:20px;">
          <h5 class="fw-bold mb-4 text-teal"><i class="bi bi-receipt"></i> 訂單摘要</h5>

          <label class="fw-bold mb-2">優惠券代碼</label>
          <div class="input-group mb-4">
            <input 
              type="text" 
              class="form-control" 
              v-model="couponCode" 
              placeholder="請輸入優惠券"
              :disabled="isCheckingOut"
            />
            <button 
              class="btn teal-reflect-button" 
              @click="applyCoupon"
              :disabled="isCheckingOut"
            >套用</button>
          </div>

          <hr />
          <div class="summary-row">
            <span>商品原價</span>
            <span class="text-muted text-decoration-line-through">
              NT$ {{ subtotalBeforeDiscount.toLocaleString() }}
            </span>
          </div>
          <div class="summary-row text-success">
            <span>商品優惠</span>
            <span>-NT$ {{ productDiscount.toLocaleString() }}</span>
          </div>
          <div class="summary-row fw-bold">
            <span>商品小計</span>
            <span>NT$ {{ subtotal.toLocaleString() }}</span>
          </div>
          <div class="summary-row">
            <span>運費</span>
            <span class="text-success">免運</span>
          </div>

          <hr />
          <div class="summary-row align-items-center">
            <h5 class="fw-bold mb-0">應付金額</h5>
            <h3 class="text-danger fw-bold mb-0">NT$ {{ finalTotal.toLocaleString() }}</h3>
          </div>

          <button 
            class="btn w-100 py-3 mt-3 teal-reflect-button" 
            @click="checkout"
            :disabled="isCheckingOut || cartItems.length === 0"
          >
            <span v-if="!isCheckingOut">
              <i class="bi bi-credit-card"></i> 前往結帳
            </span>
            <span v-else>
              <span class="spinner-border spinner-border-sm me-2"></span>
              正在跳轉至綠界...
            </span>
          </button>
          
          <button 
            class="btn w-100 py-3 mt-2 silver-reflect-button" 
            @click="continueShopping"
            :disabled="isCheckingOut"
          >
            <i class="bi bi-arrow-left"></i> 繼續購物
          </button>
        </div>
      </div>
    </div>

    <!-- ✅ 綠界表單容器 (隱藏) -->
    <div id="ecpayFormContainer" style="display:none;"></div>
  </div>
</template>

<script>
import axios from "axios";

export default {
  name: "CartComponent",
  data() {
    return {
      couponCode: "",
      isCheckingOut: false,
      cartItems: [
        {
          productId: 14246,
          skuId: 2680,
          productName: "Lake Avenue Nutrition, Omega-3 魚油,30 粒魚明膠軟膠囊(每粒軟膠囊 1,250 毫克)",
          skuName: "30 單位",
          unitPrice: 500.0,
          salePrice: 346.0,
          quantity: 1,
          subtotal: 346.0
        },
        {
          productId: 14246,
          skuId: 3388,
          productName: "Lake Avenue Nutrition, Omega-3 魚油,90 粒魚明膠軟膠囊(每粒軟膠囊 1,250 毫克)",
          skuName: "90 單位",
          unitPrice: 1000.0,
          salePrice: 898.0,
          quantity: 1,
          subtotal: 898.0
        }
      ]
    };
  },
  computed: {
    subtotalBeforeDiscount() {
      return this.cartItems.reduce((s, i) => s + i.unitPrice * i.quantity, 0);
    },
    productDiscount() {
      return this.cartItems.reduce((s, i) => s + (i.unitPrice - i.salePrice) * i.quantity, 0);
    },
    subtotal() {
      return this.cartItems.reduce((s, i) => s + i.subtotal, 0);
    },
    finalTotal() {
      return this.subtotal;
    }
  },
  methods: {
    updateSubtotal(i) {
      i.subtotal = i.salePrice * i.quantity;
    },
    increaseQuantity(i) {
      if (i.quantity < 99) {
        i.quantity++;
        this.updateSubtotal(i);
      }
    },
    decreaseOnce(i) {
      if (i.quantity === 1) {
        this.confirmRemove(i);
        return;
      }
      i.quantity--;
      this.updateSubtotal(i);
    },
    confirmRemove(i) {
      if (window.confirm(`確定要移除「${i.productName}(${i.skuName})」嗎?`)) {
        this.cartItems = this.cartItems.filter(
          x => !(x.productId === i.productId && x.skuId === i.skuId)
        );
        alert("✅ 已移除商品");
      }
    },
    applyCoupon() {
      if (!this.couponCode) {
        alert("請輸入優惠券代碼");
        return;
      }
      alert("這是示範用優惠券功能\n代碼:" + this.couponCode);
    },
    
    // 🔥 完整的結帳流程
    async checkout() {
      // 防止重複點擊
      if (this.isCheckingOut) {
        console.log("⏳ 處理中,請稍候...");
        return;
      }

      // 檢查購物車
      if (!this.cartItems || this.cartItems.length === 0) {
        alert("❌ 購物車是空的!");
        return;
      }
      
      this.isCheckingOut = true;
      console.log("🛒 開始結帳流程...");

      // 組裝請求資料
      const payload = {
        sessionId: "session123",
        userNumberId: 1056,
        cartItems: this.cartItems.map(i => ({
          productId: i.productId,
          skuId: i.skuId,
          productName: i.productName,
          optionName: i.skuName,
          salePrice: i.salePrice,
          quantity: i.quantity
        })),
        couponCode: this.couponCode || null,
        discountAmount: 0
      };

      console.log("📦 Checkout Payload:", payload);

      try {
        // 呼叫後端 API
        const res = await axios.post(
          "http://localhost:7200/api/ord/cart/checkout",
          payload,
          {
            headers: {
              "Content-Type": "application/json"
            }
          }
        );

        console.log("✅ 後端回應:", res.data);

        // 檢查是否成功
        if (res.data?.success) {
          // 🔥 修正: ecpayFormHtml 在根層級
          const ecpayHtml = res.data.ecpayFormHtml;
          
          if (!ecpayHtml) {
            console.error("完整回應:", JSON.stringify(res.data, null, 2));
            throw new Error("後端未回傳 ecpayFormHtml");
          }

          console.log("🔥 收到綠界表單,準備提交...");
          if (res.data.data) {
            console.log("訂單編號:", res.data.data.orderNo);
            console.log("訂單金額:", res.data.data.total);
          }

          // 🔥 插入表單並自動提交
          this.submitECPayForm(ecpayHtml);

        } else {
          // 結帳失敗
          const errorMsg = res.data?.message || "結帳失敗,請稍後再試";
          console.error("❌ 結帳失敗:", errorMsg);
          
          if (res.data?.errors && res.data.errors.length > 0) {
            alert("❌ " + res.data.errors.join("\n"));
          } else {
            alert("❌ " + errorMsg);
          }
          
          this.isCheckingOut = false;
        }
      } catch (error) {
        console.error("❌ 結帳錯誤:", error);
        this.isCheckingOut = false;
        
        // 解析錯誤訊息
        let errorMsg = "結帳失敗,請稍後再試";
        
        if (error.response) {
          console.error("Error Response:", error.response.data);
          errorMsg = error.response.data?.message || 
                     `伺服器錯誤 (${error.response.status})`;
          
          // 顯示詳細錯誤 (開發時有用)
          if (error.response.data?.detail) {
            console.error("詳細錯誤:", error.response.data.detail);
          }
        } else if (error.request) {
          errorMsg = "無法連接到伺服器,請檢查:\n1. 後端是否啟動 (http://localhost:7200)\n2. 網路連線是否正常";
        } else {
          errorMsg = error.message || "未知錯誤";
        }
        
        alert("❌ " + errorMsg);
      }
    },

    // 🔥 提交綠界表單 (關鍵方法!)
    submitECPayForm(htmlString) {
      try {
        console.log("📝 正在處理綠界表單...");
        
        // 取得容器
        const container = document.getElementById("ecpayFormContainer");
        if (!container) {
          throw new Error("找不到 ecpayFormContainer 元素");
        }

        // 插入 HTML
        container.innerHTML = htmlString;
        console.log("✅ 表單 HTML 已插入 DOM");

        // 找到表單
        const form = container.querySelector("form");
        if (!form) {
          console.error("HTML 內容:", htmlString.substring(0, 500));
          throw new Error("找不到 form 元素");
        }

        console.log("✅ 找到表單:", form.id || "無 ID");
        console.log("📍 表單 action:", form.action);
        console.log("📍 表單 method:", form.method);
        
        // 列出表單欄位 (開發時有用)
        const inputs = form.querySelectorAll("input");
        console.log(`📋 表單欄位數量: ${inputs.length}`);
        inputs.forEach(input => {
          const value = input.value.length > 50 
            ? input.value.substring(0, 50) + "..." 
            : input.value;
          console.log(`  - ${input.name}: ${value}`);
        });

        // 🔥 提交表單 (會跳轉到綠界)
        console.log("🚀 正在提交表單到綠界...");
        form.submit();

        // 提交後會離開當前頁面
        console.log("✅ 表單已提交");
      } catch (error) {
        console.error("❌ 提交綠界表單失敗:", error);
        alert("❌ 付款表單載入失敗: " + error.message);
        this.isCheckingOut = false;
      }
    },

    continueShopping() {
      window.location.href = "/";
    }
  },

  mounted() {
    console.log("🛒 購物車組件已載入");
    console.log("📦 商品數量:", this.cartItems.length);
    console.log("💰 總金額:", this.finalTotal);
  }
};
</script>

<style scoped>
.text-teal { 
  color: #007083; 
}

/* 卡片 hover 效果 */
.product-card {
  border: 1px solid #e9ecef;
  border-radius: 12px;
  background: #fff;
  transition: box-shadow 0.2s, transform 0.12s;
}
.product-card:hover {
  box-shadow: 0 10px 24px rgba(0, 0, 0, 0.08);
  transform: translateY(-1px);
}

/* 數量控制 */
.quantity-row {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  height: 42px;
}
.circle-btn {
  width: 42px; 
  height: 42px; 
  border-radius: 50%;
  border: none; 
  background: #007083; 
  color: #fff;
  font-size: 1.35rem; 
  font-weight: 700;
  display: flex; 
  align-items: center; 
  justify-content: center;
  transition: all 0.2s ease;
  cursor: pointer;
}
.circle-btn:hover:not(:disabled) { 
  background: #0096a8; 
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.15); 
}
.circle-btn:disabled { 
  background: #ccc; 
  cursor: not-allowed;
  opacity: 0.6;
}
.qty-input {
  width: 56px; 
  height: 42px; 
  text-align: center;
  border: 1.5px solid #ccc; 
  border-radius: 8px;
  font-weight: 700; 
  font-size: 1.1rem; 
  background: #fff;
}

/* 訂單摘要 */
.summary-row {
  display: flex; 
  justify-content: space-between;
  align-items: center; 
  margin-bottom: 10px; 
  font-size: 1.05rem;
}

/* 按鈕樣式 */
.teal-reflect-button {
  background: linear-gradient(135deg, #007083 0%, #00a0b8 100%);
  color: white;
  border: none;
  transition: all 0.3s ease;
  font-weight: 600;
}
.teal-reflect-button:hover:not(:disabled) {
  background: linear-gradient(135deg, #00586a 0%, #008a9f 100%);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 112, 131, 0.3);
}
.teal-reflect-button:disabled {
  background: #ccc;
  cursor: not-allowed;
  transform: none;
  opacity: 0.6;
}

.silver-reflect-button {
  background: linear-gradient(135deg, #6c757d 0%, #9ca3af 100%);
  color: white;
  border: none;
  transition: all 0.3s ease;
  font-weight: 600;
}
.silver-reflect-button:hover:not(:disabled) {
  background: linear-gradient(135deg, #5a6268 0%, #868e96 100%);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(108, 117, 125, 0.3);
}
.silver-reflect-button:disabled {
  background: #ccc;
  cursor: not-allowed;
  transform: none;
  opacity: 0.6;
}

/* Loading spinner */
.spinner-border-sm {
  width: 1rem;
  height: 1rem;
  border-width: 0.15em;
}

/* Bootstrap Icons (確保有引入) */
@import url('https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css');
</style>