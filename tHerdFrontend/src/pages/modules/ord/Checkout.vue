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
import { http } from '@/api/http'              // 共用 axios 實例（自動夾帶 Token）
import { useAuthStore } from '@/stores/auth'   // 讀取 accessToken
import { useRouter, useRoute } from 'vue-router'

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
  // 1) 必填檢查
  if (!this.canCheckout) {
    alert('請填寫完整收件資料')
    return
  }
  if (this.invalidItems.length > 0) {
    alert('購物車中有無效商品，請移除後再結帳')
    return
  }

  // 2) 檢查是否登入
  const auth = useAuthStore()
  if (!auth?.accessToken) {
    const router = useRouter(); const route = useRoute()
    alert('請先登入會員再結帳')
    router.push({ name: 'userlogin', query: { returnUrl: route.fullPath } })
    return
  }

  // 3) 組 payload（❌ 不要再送 sessionId）
  const payload = {
    cartItems: this.validItems.map(i => ({
      productId: i.productId,
      skuId: i.skuId,
      productName: i.productName,
      // ⬇️ 送成交價：以前是 unitPrice（原價）
      salePrice: i.salePrice ?? i.unitPrice, 
      quantity: i.qty ?? i.quantity ?? 1
    })),
    receiverName: this.receiverName,
    receiverPhone: this.receiverPhone,
    receiverAddress: this.receiverAddress,
    couponCode: this.couponCode || null
  }

  try {
    // 4) 用共用 http（自動帶 Token）
    const res = await http.post('http:localhost:7200/api/ord/cart/checkout', payload)

    if (res.data?.success) {
      const html = res.data.ecpayFormHtml
      if (!html) throw new Error('後端未回傳 ecpayFormHtml')

      // ✅ 安全提交綠界表單：插入 DOM 後 submit
      const parser = new DOMParser()
      const doc = parser.parseFromString(html, 'text/html')
      const form = doc.querySelector('form')
      if (!form) throw new Error('找不到綠界 <form>')

      document.body.appendChild(form)
      form.submit()
      return
    }

    // 失敗訊息
    if (res.data?.errors?.length) {
      alert('結帳失敗：\n' + res.data.errors.join('\n'))
    } else {
      alert(res.data?.message || '結帳失敗，請稍後再試')
    }
  } catch (err) {
    // 401/403 → 重新登入
    if (err?.response?.status === 401 || err?.response?.status === 403) {
      const router = useRouter(); const route = useRoute()
      alert('登入逾時，請重新登入')
      router.push({ name: 'userlogin', query: { returnUrl: route.fullPath } })
      return
    }
    // 其他錯誤
    const msg = err?.response?.data?.message 
      || err?.message 
      || '結帳失敗，請稍後再試'
    alert('❌ ' + msg)
    console.error('checkout error:', err)
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