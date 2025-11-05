<template>
  <div class="checkout-page">
    <h2>結帳</h2>

    <!-- 購物車商品 -->
    <div class="cart-summary">
      <div v-for="item in validItems" :key="item.skuId" class="item">
        <span>{{ item.productName }}</span>
        <span>x{{ item.quantity }}</span>
        <span>${{ item.salePrice * item.quantity }}</span>
      </div>

      <!-- 失效商品警告 -->
      <div v-if="invalidItems.length > 0" class="invalid-items">
        <h4>⚠️ 以下商品無法結帳：</h4>
        <div v-for="item in invalidItems" :key="item.cartItemId" class="invalid-item">
          <span>{{ item.productName }}</span>
          <span class="warning">{{ item.warningMessage }}</span>
          <button @click="removeItem(item.cartItemId)">移除</button>
        </div>
      </div>
    </div>

    <!-- 金額計算 -->
    <div class="price-summary">
      <div>商品小計: ${{ calculation.subtotal }}</div>
      <div>運費: ${{ calculation.shippingFee }}</div>
      <div v-if="calculation.needMoreForFreeShipping > 0" class="tip">
        💡 再買 ${{ calculation.needMoreForFreeShipping }} 即可免運
      </div>
      <div v-if="calculation.discount > 0" class="discount">
        優惠折扣: -${{ calculation.discount }}
      </div>
      <div class="total">總計: ${{ calculation.total }}</div>
    </div>

    <!-- 優惠券 -->
    <div class="coupon-section">
      <input v-model="couponCode" placeholder="輸入優惠券代碼" @blur="applyCoupon" />
      <button @click="applyCoupon">套用優惠券</button>
    </div>

    <!-- 收件資料 -->
    <div class="shipping-form">
      <h3>收件資料</h3>
      <input v-model="receiverName" placeholder="姓名*" required />
      <input v-model="receiverPhone" placeholder="電話*" required />
      <input v-model="receiverAddress" placeholder="地址*" required @blur="sendAddressToSUP" />
      <button @click="demoFill" class="btn-demo">Demo一鍵填入</button>
    </div>

    <!-- SUP 地圖顯示區域（由SUP提供） -->
    <div id="sup-map" style="height: 300px; border: 1px solid #ddd"></div>

    <!-- 結帳按鈕 -->
    <button 
      @click="checkout" 
      class="btn-checkout" 
      :disabled="!canCheckout">
      {{ checkoutButtonText }}
    </button>
  </div>
</template>

<script>
import CartAPI from '@/api/cart'

export default {
  data() {
    return {
      cartItems: [],
      couponCode: '',
      receiverName: '',
      receiverPhone: '',
      receiverAddress: '',
      calculation: {
        subtotal: 0,
        shippingFee: 0,
        discount: 0,
        total: 0,
        needMoreForFreeShipping: 0
      }
    }
  },

  computed: {
    validItems() {
      return this.cartItems.filter(i => i.isAvailable && i.isInStock)
    },

    invalidItems() {
      return this.cartItems.filter(i => !i.isAvailable || !i.isInStock)
    },

    canCheckout() {
      return this.validItems.length > 0 && 
             this.receiverName && 
             this.receiverPhone && 
             this.receiverAddress
    },

    checkoutButtonText() {
      if (!this.canCheckout) return '請填寫完整資料'
      return `確認結帳 $${this.calculation.total}`
    }
  },

  mounted() {
    this.loadCart()
  },

  methods: {
    async loadCart() {
      const sessionId = localStorage.getItem('sessionId')
      const response = await CartAPI.get({ sessionId })
      
      if (response.data.success) {
        this.cartItems = response.data.data.items
        
        if (this.validItems.length > 0) {
          this.calculateOrder()
        }
      }
    },

    async calculateOrder() {
      const response = await CartAPI.calculate({
        cartItems: this.validItems.map(i => ({
          skuId: i.skuId,
          salePrice: i.unitPrice,
          quantity: i.qty
        })),
        couponCode: this.couponCode
      })

      if (response.data.success) {
        this.calculation = response.data.data
      }
    },

    async applyCoupon() {
      if (!this.couponCode) return
      await this.calculateOrder()
    },

    async sendAddressToSUP() {
      if (!this.receiverAddress) return

      try {
        await CartAPI.sendAddressToSUP({
          receiverName: this.receiverName,
          receiverPhone: this.receiverPhone,
          receiverAddress: this.receiverAddress
        })
        // SUP會在地圖區域顯示配送資訊
      } catch (error) {
        console.error('地址傳送失敗:', error)
      }
    },

    async removeItem(cartItemId) {
      await CartAPI.remove(cartItemId)
      this.loadCart()
    },

    demoFill() {
      this.receiverName = '測試收件人'
      this.receiverPhone = '0912345678'
      this.receiverAddress = '台北市中正區測試路1號'
      this.sendAddressToSUP()
    },

    async checkout() {
      if (!this.canCheckout) {
        alert('請填寫完整收件資料')
        return
      }

      if (this.invalidItems.length > 0) {
        alert('購物車中有無效商品，請移除後再結帳')
        return
      }

      const sessionId = localStorage.getItem('sessionId')

      const response = await CartAPI.checkout({
        sessionId,
        cartItems: this.validItems.map(i => ({
          productId: i.productId,
          skuId: i.skuId,
          productName: i.productName,
          salePrice: i.unitPrice,
          quantity: i.qty
        })),
        receiverName: this.receiverName,
        receiverPhone: this.receiverPhone,
        receiverAddress: this.receiverAddress,
        couponCode: this.couponCode
      })

      if (response.data.success) {
        // 顯示綠界付款表單
        document.body.innerHTML = response.data.ecpayFormHtml
        document.forms[0].submit()
      } else {
        if (response.data.errors) {
          alert('結帳失敗：\n' + response.data.errors.join('\n'))
        } else {
          alert(response.data.message)
        }
      }
    }
  }
}
</script>

<style scoped>
.invalid-items {
  background: #fff3cd;
  padding: 15px;
  margin: 10px 0;
  border-left: 4px solid #ff9800;
}

.invalid-item {
  display: flex;
  justify-content: space-between;
  padding: 8px 0;
}

.warning {
  color: #d32f2f;
  font-weight: bold;
}

.discount {
  color: #4caf50;
  font-weight: bold;
}

.tip {
  color: #2196f3;
  font-size: 14px;
}
</style>