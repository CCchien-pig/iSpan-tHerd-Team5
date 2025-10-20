<template>
  <div class="container my-5">
    <div class="d-flex align-items-center mb-4">
      <i class="bi bi-cart3 me-3" style="font-size: 2rem"></i>
      <h2 class="fw-bold text-teal mb-0">購物車測試</h2>
    </div>

    <div class="row g-4">
      <!-- 左：商品明細 -->
      <div class="col-lg-8">
        <div
          v-for="item in cartItems"
          :key="`${item.productId}-${item.skuId}`"
          class="product-card p-4 mb-3 d-flex justify-content-between align-items-center"
        >
          <div class="flex-grow-1 pe-3">
            <h5 class="fw-bold mb-1">{{ item.productName }}</h5>
            <p class="text-muted mb-1"><i class="bi bi-tag"></i> 規格：{{ item.skuName }}</p>
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
                :title="item.quantity === 1 ? '刪除商品' : '減少數量'"
              >-</button>
              <input type="text" class="qty-input" :value="item.quantity" readonly />
              <button
                class="circle-btn"
                @click="increaseQuantity(item)"
                title="增加數量"
              >+</button>
            </div>

            <div class="text-teal fw-bold fs-5" style="min-width: 96px;">
              NT$ {{ item.subtotal.toLocaleString() }}
            </div>

            <button class="btn btn-outline-danger btn-sm" @click="confirmRemove(item)" title="移除商品">
              <i class="bi bi-trash"></i>
            </button>
          </div>
        </div>
      </div>

      <!-- 右：訂單摘要 -->
      <div class="col-lg-4">
        <div class="card shadow-sm border-0 sticky-top p-4" style="top:20px;">
          <h5 class="fw-bold mb-4 text-teal"><i class="bi bi-receipt"></i> 訂單摘要</h5>

          <label class="fw-bold mb-2">優惠券代碼</label>
          <div class="input-group mb-4">
            <input type="text" class="form-control" v-model="couponCode" placeholder="請輸入優惠券" />
            <button class="btn teal-reflect-button" @click="applyCoupon">套用</button>
          </div>

          <hr />
          <div class="summary-row"><span>商品原價</span><span class="text-muted text-decoration-line-through">NT$ {{ subtotalBeforeDiscount.toLocaleString() }}</span></div>
          <div class="summary-row text-success"><span>商品優惠</span><span>-NT$ {{ productDiscount.toLocaleString() }}</span></div>
          <div class="summary-row fw-bold"><span>商品小計</span><span>NT$ {{ subtotal.toLocaleString() }}</span></div>
          <div class="summary-row"><span>運費</span><span class="text-success">免運</span></div>

          <hr />
          <div class="summary-row align-items-center">
            <h5 class="fw-bold mb-0">應付金額</h5>
            <h3 class="text-danger fw-bold mb-0">NT$ {{ finalTotal.toLocaleString() }}</h3>
          </div>

          <button class="btn w-100 py-3 mt-3 teal-reflect-button" @click="checkout">
            <i class="bi bi-credit-card"></i> 前往結帳
          </button>
          <button class="btn w-100 py-3 mt-2 silver-reflect-button" @click="continueShopping">
            <i class="bi bi-arrow-left"></i> 繼續購物
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import axios from "axios";

export default {
  name: "CartComponent",
  data() {
    return {
      couponCode: "",
      cartItems: [
        {
          productId: 14246,
          skuId: 2680,
          productName: "Lake Avenue Nutrition, Omega-3 魚油，30 粒魚明膠軟膠囊（每粒軟膠囊 1,250 毫克）",
          skuName: "30 單位",
          unitPrice: 500.0,
          salePrice: 346.0,
          quantity: 1,
          subtotal: 346.0
        },
        {
          productId: 14246,
          skuId: 3388,
          productName: "Lake Avenue Nutrition, Omega-3 魚油，90 粒魚明膠軟膠囊（每粒軟膠囊 1,250 毫克）",
          skuName: "90 單位",
          unitPrice: 1000.0,
          salePrice: 898.0,
          quantity: 1,
          subtotal: 898.0
        },
        {
          productId: 14600,
          skuId: 2869,
          productName: "Optimum Nutrition, Opti-Women®，針對活躍女性的多維生素，60 粒膠囊",
          skuName: "60 粒",
          unitPrice: 800.0,
          salePrice: 656.0,
          quantity: 1,
          subtotal: 656.0
        },
        {
          productId: 14600,
          skuId: 3387,
          productName: "Optimum Nutrition, Opti-Women®，針對活躍女性的多維生素，120 粒膠囊",
          skuName: "120 粒",
          unitPrice: 1300.0,
          salePrice: 1188.0,
          quantity: 1,
          subtotal: 1188.0
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
      if (i.quantity < 99) i.quantity++;
      this.updateSubtotal(i);
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
      if (window.confirm(`確定要移除「${i.productName}（${i.skuName}）」嗎？`)) {
        this.cartItems = this.cartItems.filter(
          x => !(x.productId === i.productId && x.skuId === i.skuId)
        );
        alert("✅ 已移除商品");
      }
    },
    applyCoupon() {
      alert("這是示範用優惠券功能");
    },
    async checkout() {
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
        }))
      };

      console.log("🚀 Checkout Payload:", payload);

      try {
        const res = await axios.post(
          "https://localhost:7103/api/ord/Cart/checkout",
          payload,
          {
            headers: {
              "Content-Type": "application/json" // 👈 關鍵
            }
          }
        );

        console.log("✅ Response:", res.data);

        if (res.data?.success) {
          alert(`✅ 訂單建立成功！編號：${res.data.orderNo}`);
        } else {
          alert(`❌ ${res.data?.message ?? "結帳失敗"}`);
        }
      } catch (e) {
        console.error("❌ Checkout Error:", e);
        if (e.response) {
          alert(`伺服器錯誤：${e.response.status} - ${e.response.statusText}`);
        } else {
          alert("❌ 無法結帳，請確認 API 是否啟動。");
        }
      }
    },
    continueShopping() {
      window.location.href = "/";
    }
  }
};
</script>


<style scoped>
.text-teal { color:#007083; }

/* 卡片 hover 效果 */
.product-card {
  border:1px solid #e9ecef;
  border-radius:12px;
  background:#fff;
  transition:box-shadow .2s, transform .12s;
}
.product-card:hover {
  box-shadow:0 10px 24px rgba(0,0,0,.08);
  transform:translateY(-1px);
}

/* 數量控制 */
.quantity-row {
  display:flex;
  align-items:center;
  justify-content:center;
  gap:8px;
  height:42px;
}
.circle-btn {
  width:42px; height:42px; border-radius:50%;
  border:none; background:#007083; color:#fff;
  font-size:1.35rem; font-weight:700;
  display:flex; align-items:center; justify-content:center;
  transition:all .2s ease;
}
.circle-btn:hover { background:#0096a8; box-shadow:0 2px 6px rgba(0,0,0,.15); }
.qty-input {
  width:56px; height:42px; text-align:center;
  border:1.5px solid #ccc; border-radius:8px;
  font-weight:700; font-size:1.1rem; background:#fff;
}

/* 訂單摘要 */
.summary-row {
  display:flex; justify-content:space-between;
  align-items:center; margin-bottom:10px; font-size:1.05rem;
}
</style>
