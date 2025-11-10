<template>
  <div class="container my-5">
    <div class="d-flex align-items-center mb-4">
      <i class="bi bi-cart3 me-3" style="font-size: 2rem"></i>
      <h2 class="fw-bold text-teal mb-0">購物車</h2>
    </div>

    <div class="row g-4">
      <!-- 左側：商品列表 -->
      <div class="col-lg-8">
        <div
          v-for="item in cartItems"
          :key="item.cartItemId"
          class="product-card p-4 mb-3 d-flex justify-content-between align-items-center"
          :class="{ 'disabled-item': !item.isValid }"
        >
          <div class="flex-grow-1 pe-3">
            <h5 class="fw-bold mb-1" :class="{ 'text-muted': !item.isValid }">
              {{ item.productName }}
            </h5>
            <p class="text-muted mb-1">
              <i class="bi bi-tag"></i> 規格: {{ item.skuName }}
            </p>
            
            <div v-if="!item.isValid" class="text-danger fw-bold mb-2">
              ❌ {{ item.disabledReason }}
            </div>

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
                :disabled="!item.isValid || isCheckingOut"
              >
                -
              </button>
              <input 
                type="text" 
                class="qty-input" 
                :value="item.quantity" 
                readonly 
                :class="{ 'text-muted': !item.isValid }"
              />
              <button
                class="circle-btn"
                @click="increaseQuantity(item)"
                :disabled="!item.isValid || isCheckingOut"
              >
                +
              </button>
            </div>

            <div 
              class="fw-bold fs-5" 
              :class="item.isValid ? 'text-teal' : 'text-muted'"
              style="min-width: 96px;"
            >
              NT$ {{ (item.salePrice * item.quantity).toLocaleString() }}
            </div>

            <button 
              class="btn btn-outline-danger btn-sm" 
              @click="confirmRemove(item)" 
              :disabled="isCheckingOut"
            >
              <i class="bi bi-trash"></i>
            </button>
          </div>
        </div>

        <div v-if="cartItems.length === 0" class="text-center py-5">
          <i class="bi bi-cart-x" style="font-size: 4rem; color: #ccc;"></i>
          <h4 class="mt-3 text-muted">購物車是空的</h4>
          <button class="btn btn-primary mt-3" @click="continueShopping">
            <i class="bi bi-arrow-left"></i> 繼續購物
          </button>
        </div>
      </div>

      <!-- 右側：訂單摘要 -->
      <div class="col-lg-4">
        <div class="card shadow-sm border-0 sticky-top p-4" style="top:20px;">
          <h5 class="fw-bold mb-4 text-teal">
            <i class="bi bi-receipt"></i> 訂單摘要
          </h5>

          <div v-if="invalidCount > 0" class="alert alert-warning mb-3">
            <i class="bi bi-exclamation-triangle"></i>
            您的購物車有 {{ invalidCount }} 件商品無法結帳
          </div>

          <label class="fw-bold mb-2">優惠券代碼</label>
          
          <!-- 🔥 修改：優惠券輸入區 -->
          <div v-if="!couponCode || !promotionResult" class="input-group mb-2">
            <input 
              type="text" 
              class="form-control" 
              v-model="couponCode" 
              placeholder="請輸入優惠券"
              :disabled="isCheckingOut || !canCheckout || isFindingBestCoupon"
            />
            <button 
              class="btn teal-reflect-button" 
              @click="applyCoupon"
              :disabled="isCheckingOut || !canCheckout || !couponCode || isFindingBestCoupon"
            >
              套用
            </button>
          </div>

          <!-- 🔥 新增：已套用優惠券顯示 -->
          <div v-else class="applied-coupon-box mb-2">
            <div class="d-flex align-items-center justify-content-between">
              <div class="flex-grow-1">
                <div class="d-flex align-items-center">
                  <i class="bi bi-ticket-perforated-fill text-success me-2" style="font-size: 1.3rem;"></i>
                  <div>
                    <div class="fw-bold" style="font-size: 0.95rem;">已套用優惠券</div>
                    <div class="text-muted" style="font-size: 0.85rem;">{{ couponCode }}</div>
                  </div>
                </div>
              </div>
              <button 
                class="btn btn-sm btn-outline-danger"
                @click="removeCoupon"
                :disabled="isCheckingOut"
                title="取消使用此優惠券"
              >
                <i class="bi bi-x-lg"></i>
              </button>
            </div>
          </div>

          <!-- 🔥 自動找最優惠券按鈕 -->
          <button 
            class="btn btn-outline-success w-100 mb-3" 
            @click="applyBestCoupon"
            :disabled="isCheckingOut || !canCheckout || isFindingBestCoupon"
          >
            <span v-if="!isFindingBestCoupon">
              <i class="bi bi-lightning-charge"></i> 自動套用最優惠券
            </span>
            <span v-else>
              <span class="spinner-border spinner-border-sm me-2"></span>
              計算中...
            </span>
          </button>
          
          <!-- 🔔 未達門檻的優惠券提示 -->
          <div v-if="nearestCouponHint && nearestCouponHint.type === 'nearly'" class="coupon-nearly-hint mb-4">
            <div class="d-flex align-items-center mb-2">
              <i class="bi bi-lightbulb-fill me-2"></i>
              <div class="flex-grow-1">
                <div class="hint-text">
                  再買 <strong class="amount-highlight">NT$ {{ nearestCouponHint.gap.toLocaleString() }}</strong> 即可使用
                </div>
                <div class="coupon-tag mt-1">
                  <i class="bi bi-gift-fill"></i> {{ nearestCouponHint.couponName }}
                  <span class="discount-badge">折 NT$ {{ nearestCouponHint.discountAmount.toLocaleString() }}</span>
                </div>
              </div>
            </div>
            
            <!-- 進度條 -->
            <div class="progress-container">
              <div 
                class="progress-bar-custom" 
                :style="{ width: calculateCouponProgress(nearestCouponHint) + '%' }"
              >
                <span class="progress-text">{{ calculateCouponProgress(nearestCouponHint).toFixed(0) }}%</span>
              </div>
            </div>
            
            <div class="threshold-info mt-2">
              <i class="bi bi-info-circle-fill"></i>
              門檻 NT$ {{ nearestCouponHint.threshold.toLocaleString() }} 
              <span class="ms-2 current-amount">
                (目前 NT$ {{ subtotal.toLocaleString() }})
              </span>
            </div>
          </div>

          <!-- 🎉 已達門檻可使用的優惠券提示 -->
          <div v-if="availableCouponHint" class="coupon-available-hint mb-4">
            <div class="d-flex align-items-center mb-2">
              <i class="bi bi-gift-fill me-2 gift-icon"></i>
              <div class="flex-grow-1">
                <!-- 🔥 根據是否比當前更好顯示不同文字 -->
                <div class="hint-text" v-if="availableCouponHint.isBetterThanCurrent">
                  <strong>💎 發現更優惠的券！</strong>
                </div>
                <div class="hint-text" v-else>
                  <strong>🎉 恭喜！您可以使用優惠券</strong>
                </div>
                <div class="coupon-tag-available mt-1">
                  <i class="bi bi-star-fill"></i> {{ availableCouponHint.couponName }}
                  <span class="discount-badge-large">折 NT$ {{ availableCouponHint.discountAmount.toLocaleString() }}</span>
                </div>
                <!-- 🔥 如果比當前更好，顯示可多省 -->
                <div v-if="availableCouponHint.isBetterThanCurrent" class="savings-info-green mt-1">
                  <i class="bi bi-piggy-bank-fill"></i>
                  可多省 NT$ {{ (availableCouponHint.discountAmount - availableCouponHint.currentDiscount).toLocaleString() }}
                </div>
              </div>
            </div>
            
            <button 
              class="btn btn-success w-100 btn-sm mt-2 pulse-button"
              @click="applyAvailableCoupon(availableCouponHint)"
              :disabled="isCheckingOut"
            >
              <i class="bi bi-check-circle-fill me-1"></i>
              <span v-if="availableCouponHint.isBetterThanCurrent">立即切換使用</span>
              <span v-else>立即使用此優惠券</span>
            </button>
            
            <div class="threshold-info mt-2 text-success">
              <i class="bi bi-check-circle-fill"></i>
              <span v-if="availableCouponHint.threshold > 0">
                已達門檻 NT$ {{ availableCouponHint.threshold.toLocaleString() }}
              </span>
              <span v-else>
                已可使用此優惠券
              </span>
              <span class="ms-2">
                (目前 NT$ {{ subtotal.toLocaleString() }})
              </span>
            </div>
          </div>

          <!-- 🔥 已套用但有更好未達門檻的券提示 -->
          <div v-if="betterCouponHint" class="better-coupon-hint mb-4">
            <div class="d-flex align-items-center mb-2">
              <i class="bi bi-star-fill me-2 star-icon"></i>
              <div class="flex-grow-1">
                <div class="hint-text">
                  <strong>💡 升級優惠券機會</strong>
                </div>
                <div class="hint-subtitle mt-1">
                  再加購 <strong class="amount-highlight">NT$ {{ betterCouponHint.gap.toLocaleString() }}</strong> 
                  可升級使用
                </div>
                <div class="coupon-tag-better mt-2">
                  <i class="bi bi-arrow-up-circle-fill"></i> {{ betterCouponHint.couponName }}
                  <span class="discount-badge-better">折 NT$ {{ betterCouponHint.discountAmount.toLocaleString() }}</span>
                </div>
                <div class="savings-info mt-1">
                  <i class="bi bi-piggy-bank-fill"></i>
                  可多省 NT$ {{ (betterCouponHint.discountAmount - promotionDiscount).toLocaleString() }}
                </div>
              </div>
            </div>
            
            <!-- 進度條 -->
            <div class="progress-container">
              <div 
                class="progress-bar-better" 
                :style="{ width: calculateCouponProgress(betterCouponHint) + '%' }"
              >
                <span class="progress-text">{{ calculateCouponProgress(betterCouponHint).toFixed(0) }}%</span>
              </div>
            </div>
            
            <div class="threshold-info mt-2">
              <i class="bi bi-info-circle-fill"></i>
              門檻 NT$ {{ betterCouponHint.threshold.toLocaleString() }} 
              <span class="ms-2 current-amount">
                (目前 NT$ {{ subtotal.toLocaleString() }})
              </span>
            </div>
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

          <div class="summary-row text-success" v-if="promotionDiscount > 0">
            <span>優惠券折扣</span>
            <span>-NT$ {{ promotionDiscount.toLocaleString() }}</span>
          </div>

          <div class="summary-row" style="margin-bottom: 0;">
            <span>運費</span>
            <span v-if="shippingFee === 0 && amountAfterCoupon >= 1500" class="text-success">免運</span>
            <span v-else-if="shippingFee === 0">計算中...</span>
            <span v-else>NT$ {{ shippingFee.toLocaleString() }}</span>
          </div>

          <!-- 🔥 免運提示 -->
          <div v-if="amountAfterCoupon > 0 && amountAfterCoupon < 1500" class="free-shipping-hint mt-2 mb-3">
            <div class="d-flex align-items-center mb-2">
              <i class="bi bi-truck me-2"></i>
              <div class="flex-grow-1">
                <div class="hint-text">
                  再購買 <strong class="amount-highlight-shipping">NT$ {{ (1500 - amountAfterCoupon).toLocaleString() }}</strong> 即可免運
                </div>
              </div>
            </div>
            <div class="progress-container-shipping">
              <div 
                class="progress-bar-shipping" 
                :style="{ width: calculateShippingProgress() + '%' }"
              >
                <span class="progress-text">{{ calculateShippingProgress().toFixed(0) }}%</span>
              </div>
            </div>
          </div>
          
          <div v-else-if="amountAfterCoupon >= 1500" class="free-shipping-achieved mt-2 mb-3">
            <i class="bi bi-check-circle-fill me-2"></i>
            已達免運門檻！
          </div>

          <hr />
          
          <div class="summary-row align-items-center">
            <h5 class="fw-bold mb-0">應付金額</h5>
            <h3 class="text-danger fw-bold mb-0">
              NT$ {{ finalTotal.toLocaleString() }}
            </h3>
          </div>

          <!-- 物流選擇 -->
          <div class="mt-4">
            <label class="fw-bold mb-2">配送方式</label>
            <select 
              class="form-select mb-3" 
              v-model.number="selectedLogisticsId"
              @change="calculateShippingFee"
              :disabled="!canCheckout"
            >
              <option :value="1000">宅配到府（順豐速運）</option>
              <option :value="1001">低溫宅配（黑貓宅急便）</option>
              <option :value="1002">超商店到店（7-ELEVEN）</option>
              <option :value="1003">i郵箱（中華郵政）</option>
              <option :value="1004">掛號包裹（中華郵政）</option>
            </select>
          </div>

          <!-- 收件人資訊 -->
          <div class="mt-3">
            <div class="d-flex justify-content-between align-items-center mb-2">
              <label class="fw-bold mb-0">收件人資訊</label>
              <button 
                class="btn btn-sm btn-outline-secondary" 
                @click="demoFill"
                :disabled="!canCheckout"
              >
                Demo一鍵填入
              </button>
            </div>

            <input 
              type="text" 
              class="form-control mb-3" 
              v-model="receiverName"
              placeholder="收件人姓名"
              :disabled="!canCheckout"
            />
            
            <input 
              type="text" 
              class="form-control mb-3" 
              v-model="receiverPhone"
              placeholder="收件人電話"
              :disabled="!canCheckout"
            />
            
            <input 
              type="text" 
              class="form-control mb-3" 
              v-model="receiverAddress"
              placeholder="收件地址"
              :disabled="!canCheckout"
              @blur="calculateShippingFee"
            />
          </div>

          <button 
            class="btn w-100 py-3 mt-3 teal-reflect-button" 
            @click="checkout"
            :disabled="isCheckingOut || !canCheckout || cartItems.length === 0"
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

    <div id="ecpayFormContainer" style="display:none;"></div>
  </div>
</template>

<script>
import { http } from '@/api/http'
import { useAuthStore } from '@/stores/auth'

export default {
  name: 'CartComponent',
  data() {
    return {
      couponCode: '',
      isCheckingOut: false,
      isFindingBestCoupon: false,
      receiverName: '',
      receiverPhone: '',
      receiverAddress: '',
      cartItems: [],
      promotionResult: null,
      canCheckout: true,
      invalidCount: 0,
      shippingFee: 0,
      selectedLogisticsId: 1000,
      nearestCouponHint: null,
      availableCouponHint: null,
      betterCouponHint: null
    }
  },
  computed: {
    subtotalBeforeDiscount() {
      return this.cartItems
        .filter(i => i.isValid)
        .reduce((s, i) => s + (i.unitPrice || 0) * i.quantity, 0)
    },
    productDiscount() {
      return this.cartItems
        .filter(i => i.isValid)
        .reduce((s, i) => s + ((i.unitPrice || 0) - (i.salePrice || 0)) * i.quantity, 0)
    },
    subtotal() {
      return this.cartItems
        .filter(i => i.isValid)
        .reduce((s, i) => s + (i.salePrice || 0) * i.quantity, 0)
    },
    promotionDiscount() {
      if (!this.promotionResult) return 0
      const discount = this.promotionResult.discounts?.[0]
      return discount?.discountAmount || 0
    },
    amountAfterCoupon() {
      return this.subtotal - this.promotionDiscount
    },
    finalTotal() {
      return Math.max(0, this.subtotal - this.promotionDiscount + this.shippingFee)
    }
  },
  methods: {
    calculateCouponProgress(hint) {
      if (!hint || !hint.threshold) return 0
      const currentAmount = this.subtotal
      const progress = (currentAmount / hint.threshold) * 100
      return Math.min(Math.max(progress, 0), 100)
    },

    calculateShippingProgress() {
      const currentAmount = this.amountAfterCoupon
      const threshold = 1500
      const progress = (currentAmount / threshold) * 100
      return Math.min(Math.max(progress, 0), 100)
    },

    async loadCart() {
      try {
        const res = await http.get('/ord/cart/get')
        if (res?.data?.success) {
          this.cartItems = res.data.data.items || []
          this.canCheckout = res.data.data.canCheckout ?? true
          this.invalidCount = res.data.data.invalidCount || 0
          
          if (this.cartItems.length > 0) {
            await this.calculateShippingFee()
            await this.updateCouponHint()
          }
        }
      } catch (err) {
        console.error('載入購物車失敗:', err)
        alert('無法載入購物車')
      }
    },

    async calculateShippingFee() {
      const amountAfterCoupon = this.subtotal - this.promotionDiscount
      
      if (amountAfterCoupon >= 1500) {
        this.shippingFee = 0
        return
      }

      if (!this.receiverAddress?.trim()) {
        this.shippingFee = 0
        return
      }

      const validItems = this.cartItems.filter(i => i.isValid)
      if (validItems.length === 0) {
        this.shippingFee = 0
        return
      }

      try {
        const firstItem = validItems[0]
        const res = await http.post('/sup/LogisticsRate/order-shipping-fee', {
          skuId: firstItem.skuId,
          qty: firstItem.quantity,
          logisticsId: this.selectedLogisticsId
        })

        if (res?.data?.success && res.data.data?.success && res.data.data.data?.shippingFee) {
          this.shippingFee = res.data.data.data.shippingFee
        } else {
          this.shippingFee = 100
        }
      } catch (err) {
        console.error('計算運費失敗:', err)
        this.shippingFee = 100
      }
    },

    async validateCoupon(couponInput, showAlert = true) {
      const code = typeof couponInput === 'string'
          ? couponInput
          : (couponInput?.couponCode ?? couponInput?.couponId ?? '')
      if (!code || this.cartItems.length === 0) {
        return { isValid: false, data: null }
      }

      try {
        const auth = useAuthStore()
        
        if (!auth?.accessToken) {
          if (showAlert) alert('❌ 請先登入會員才能使用優惠券')
          return { isValid: false, data: null }
        }

        const userNumberId = auth.user?.userNumberId || auth.userNumberId

        if (!userNumberId || userNumberId <= 0) {
          if (showAlert) {
            alert('❌ 無法取得會員資訊，請重新登入')
            this.$router.push({ name: 'userlogin', query: { returnUrl: this.$route.fullPath } })
          }
          return { isValid: false, data: null }
        }

        const payload = {
          userNumberId: userNumberId,
          subtotal: this.subtotal,
          couponId: code
        }

        const res = await http.post('/promotion/calculate', payload)
        
        if (res?.data?.success && res.data.data?.discounts?.length > 0) {
          return { isValid: true, data: res.data.data, reason: null }
        }
        
        const msg = res?.data?.message || '不符合優惠券使用規定'
        let reason = { code: 'unknown', message: msg }
        const m = msg.match(/未達滿額\s*(\d+)\s*元/)
        if (m) {
          const threshold = Number(m[1])
          const gap = Math.max(0, threshold - this.subtotal)
          reason = { code: 'min_spend_not_met', message: msg, threshold, gap }
        }
        if (showAlert) alert('❌ ' + msg)
        return { isValid: false, data: null, reason }
      } catch (err) {
        console.error('驗證優惠券失敗:', err)
        if (showAlert) {
          const errorMsg = err?.response?.data?.message || err?.message || '優惠券驗證失敗'
          alert('❌ ' + errorMsg)
        }
        return { isValid: false, data: null }
      }
    },

    async revalidatePromotion() {
      if (!this.couponCode || !this.promotionResult) {
        return
      }

      console.log('🔍 重新驗證優惠券:', this.couponCode)
      
      const savedCouponCode = this.couponCode
      const result = await this.validateCoupon(this.couponCode, false)
      
      if (result.isValid) {
        console.log('✅ 優惠券仍然有效')
        this.promotionResult = result.data
        await this.calculateShippingFee()
      } else {
        console.log('❌ 優惠券已失效，清空並通知')
        this.couponCode = ''
        this.promotionResult = null
        await this.calculateShippingFee()
        alert(`⚠️ 優惠券「${savedCouponCode}」已取消使用\n原因：不符合優惠券使用條件`)
        
        await this.updateCouponHint()
      }
    },

    async applyCoupon() {
      if (!this.couponCode) {
        alert('請輸入優惠券代碼')
        return
      }
      
      const result = await this.validateCoupon(this.couponCode, true)
      
      if (result.isValid) {
        this.promotionResult = result.data
        await this.calculateShippingFee()
        this.nearestCouponHint = null
        this.availableCouponHint = null
        alert('✅ 優惠券套用成功')
        await this.updateCouponHint()
      } else {
        this.couponCode = ''
        this.promotionResult = null
        await this.updateCouponHint()
      }
    },

    // 🔥 新增：取消使用優惠券
    async removeCoupon() {
      if (!this.couponCode) return
      
      const confirmed = confirm('確定要取消使用此優惠券嗎？')
      if (!confirmed) return
      
      console.log('🗑️ 取消使用優惠券:', this.couponCode)
      
      this.couponCode = ''
      this.promotionResult = null
      this.appliedCouponWalletId = null
      
      await this.calculateShippingFee()
      await this.updateCouponHint()
      
      alert('✅ 已取消使用優惠券')
    },

    async applyAvailableCoupon(couponHint) {
      this.couponCode = couponHint.couponCode
      this.appliedCouponWalletId = couponHint.couponWalletId
      this.promotionResult = couponHint.promotionResult
      await this.calculateShippingFee()
      this.availableCouponHint = null
      this.nearestCouponHint = null
      this.betterCouponHint = null
      alert(`✅ 已套用優惠券：${couponHint.couponName}\n折扣金額：NT$ ${couponHint.discountAmount.toLocaleString()}`)
      await this.updateCouponHint()
    },

    async updateCouponHint() {
      try {
        await this.findBestCoupon()
        console.log('🔄 優惠券提示已更新')
      } catch (err) {
        console.error('更新優惠券提示失敗:', err)
      }
    },

    async findBestCoupon() {
      try {
        const auth = useAuthStore()
        
        if (!auth?.accessToken) {
          console.warn('未登入，無法查詢優惠券')
          this.nearestCouponHint = null
          this.availableCouponHint = null
          this.betterCouponHint = null
          return null
        }

        if (this.cartItems.length === 0 || this.subtotal <= 0) {
          console.warn('購物車為空或金額為0，無法計算優惠券')
          this.nearestCouponHint = null
          this.availableCouponHint = null
          this.betterCouponHint = null
          return null
        }

        const userNumberId = auth.user?.userNumberId || auth.userNumberId
        if (!userNumberId || userNumberId <= 0) {
          console.warn('無法取得會員ID')
          this.nearestCouponHint = null
          this.availableCouponHint = null
          this.betterCouponHint = null
          return null
        }

        const walletRes = await http.get('/user/coupons/wallet?onlyUsable=true&pageSize=100')
        const availableCoupons = walletRes?.data?.items || []
        
        if (availableCoupons.length === 0) {
          console.log('目前沒有可用的優惠券')
          this.nearestCouponHint = null
          this.availableCouponHint = null
          this.betterCouponHint = null
          return null
        }

        console.log(`找到 ${availableCoupons.length} 張可用優惠券，開始計算...`)

        const applicable = []
        const nearly = []
        
        for (const item of availableCoupons) {
          if (!item.isUsable || !item.coupon?.couponCode) continue
          
          const result = await this.validateCoupon(
            { couponId: item.coupon.couponId, couponCode: item.coupon.couponCode },
            false
          )
          
          if (result.isValid) {
            const discount = result.data.discounts[0]
            applicable.push({
              couponWalletId: item.couponWalletId,
              couponId: item.coupon.couponId,
              couponCode: item.coupon.couponCode,
              couponName: item.coupon.couponName || item.coupon.couponCode,
              discountAmount: discount.discountAmount || 0,
              threshold: discount.minSpend || item.coupon.minSpend || item.coupon.threshold || 0,
              promotionResult: result.data
            })
          } else if (result?.reason?.code === 'min_spend_not_met') {
            nearly.push({
              couponId: item.coupon.couponId,
              couponCode: item.coupon.couponCode,
              couponName: item.coupon.couponName || item.coupon.couponCode,
              threshold: result.reason.threshold,
              gap: result.reason.gap,
              discountAmount: item.coupon.discountAmount || 0
            })
          }
        }

        if (this.couponCode && this.promotionResult) {
          const currentDiscount = this.promotionDiscount
          
          const betterApplicable = applicable.filter(c => 
            c.couponCode !== this.couponCode && 
            c.discountAmount > currentDiscount
          )
          
          if (betterApplicable.length > 0) {
            betterApplicable.sort((a, b) => b.discountAmount - a.discountAmount)
            const best = betterApplicable[0]
            
            console.log(`💎 有更好的可用優惠券: ${best.couponName}, 可多省 NT$ ${best.discountAmount - currentDiscount}`)
            
            this.availableCouponHint = {
              ...best,
              isBetterThanCurrent: true,
              currentDiscount: currentDiscount
            }
            this.betterCouponHint = null
            this.nearestCouponHint = null
            return { type: 'better_applicable', best }
          }
          
          const betterNearly = nearly.filter(c => 
            c.discountAmount > currentDiscount && c.gap > 0
          )
          
          if (betterNearly.length > 0) {
            betterNearly.sort((a, b) => {
              if (a.gap !== b.gap) return a.gap - b.gap
              return b.discountAmount - a.discountAmount
            })
            
            const closest = betterNearly[0]
            console.log(`🎯 有更好但未達門檻的優惠券: ${closest.couponName}，還差 NT$${closest.gap}`)
            
            this.betterCouponHint = {
              type: 'better_nearly',
              couponName: closest.couponName,
              couponCode: closest.couponCode,
              threshold: closest.threshold,
              gap: closest.gap,
              discountAmount: closest.discountAmount
            }
            this.availableCouponHint = null
            this.nearestCouponHint = null
            return { type: 'better_nearly', closest }
          }
          
          console.log('✅ 目前已是最優惠券')
          this.betterCouponHint = null
          this.availableCouponHint = null
          this.nearestCouponHint = null
          return { type: 'already_best' }
        }

        if (applicable.length > 0) {
          applicable.sort((a, b) => b.discountAmount - a.discountAmount)
          const best = applicable[0]
          
          console.log(`✅ 有可用優惠券: ${best.couponName}, 折扣: NT$ ${best.discountAmount}`)
          
          this.availableCouponHint = best
          this.nearestCouponHint = null
          this.betterCouponHint = null
          
          return { type: 'applicable', best, applicable, nearly }
        }
        
        if (nearly.length > 0) {
          nearly.sort((a, b) => {
            if (a.gap !== b.gap) return a.gap - b.gap
            return b.discountAmount - a.discountAmount
          })
          
          const closest = nearly[0]
          console.log(`📍 未達門檻，最接近優惠券：${closest.couponName}，還差 NT$${closest.gap}`)
          
          this.nearestCouponHint = {
            type: 'nearly',
            couponName: closest.couponName,
            threshold: closest.threshold,
            gap: closest.gap,
            couponCode: closest.couponCode,
            discountAmount: closest.discountAmount
          }
          this.availableCouponHint = null
          this.betterCouponHint = null
          
          return { type: 'nearly', closest, applicable, nearly }
        }
        
        console.log('沒有優惠券符合使用條件')
        this.nearestCouponHint = null
        this.availableCouponHint = null
        this.betterCouponHint = null
        return { type: 'none' }
      } catch (err) {
        console.error('查詢最優惠券失敗:', err)
        this.nearestCouponHint = null
        this.availableCouponHint = null
        this.betterCouponHint = null
        return { type: 'error', error: err }
      }
    },

    async applyBestCoupon() {
      this.isFindingBestCoupon = true
      
      try {
        const pick = await this.findBestCoupon()
        
        if (!pick || pick.type === 'none' || pick.type === 'error') {
          this.couponCode = ''
          this.promotionResult = null
          alert('❌ 目前沒有可用的優惠券或無符合條件的優惠券')
          return
        }
        
        if (pick.type === 'applicable') {
          const best = pick.best
          this.couponCode = best.couponCode
          this.appliedCouponWalletId = best.couponWalletId
          this.promotionResult = best.promotionResult
          await this.calculateShippingFee()
          this.nearestCouponHint = null
          this.availableCouponHint = null
          alert(`✅ 已自動套用最優惠券：${best.couponName}\n折扣金額：NT$ ${best.discountAmount.toLocaleString()}`)
          await this.updateCouponHint()
          return
        }
        
        if (pick.type === 'nearly') {
          const c = pick.closest
          alert(
            `⚠️ 目前未達門檻\n\n` +
            `最接近可用的優惠券：${c.couponName}\n` +
            `折扣金額：NT$ ${c.discountAmount.toLocaleString()}\n` +
            `使用門檻：NT$ ${c.threshold.toLocaleString()}\n\n` +
            `💡 再加購：NT$ ${c.gap.toLocaleString()} 即可使用此券\n`
          )
          return
        }

        if (pick.type === 'better_applicable' || pick.type === 'better_nearly') {
          const c = pick.type === 'better_applicable' ? pick.best : pick.closest
          const gap = c.gap || 0
          
          if (gap === 0) {
            alert(
              `💎 發現更優惠的優惠券！\n\n` +
              `${c.couponName}\n` +
              `折扣金額：NT$ ${c.discountAmount.toLocaleString()}\n` +
              `可多省：NT$ ${(c.discountAmount - this.promotionDiscount).toLocaleString()}\n\n` +
              `已達使用門檻，可立即切換使用！`
            )
          } else {
            alert(
              `💡 發現更優惠的優惠券！\n\n` +
              `${c.couponName}\n` +
              `折扣金額：NT$ ${c.discountAmount.toLocaleString()}\n` +
              `可多省：NT$ ${(c.discountAmount - this.promotionDiscount).toLocaleString()}\n` +
              `使用門檻：NT$ ${c.threshold.toLocaleString()}\n\n` +
              `💡 再加購：NT$ ${gap.toLocaleString()} 即可使用此券\n`
            )
          }
          return
        }
      } catch (err) {
        console.error('套用最優惠券失敗:', err)
        this.couponCode = ''
        this.promotionResult = null
        alert('❌ 套用最優惠券失敗')
      } finally {
        this.isFindingBestCoupon = false
      }
    },

    demoFill() {
      this.receiverName = '測試人員2'
      this.receiverPhone = '0912345678'
      this.receiverAddress = '桃園市中壢區新生路二段421號'
      this.calculateShippingFee()
    },

    async updateQuantity(item, newQty) {
      try {
        await http.put(`/ord/cart/update/${item.cartItemId}`, { qty: newQty })
        await this.loadCart()
        await this.revalidatePromotion()
        await this.updateCouponHint()
      } catch (err) {
        console.error('更新失敗:', err)
        alert('更新數量失敗')
      }
    },

    increaseQuantity(i) {
      if (i.quantity < 99) {
        this.updateQuantity(i, i.quantity + 1)
      }
    },

    decreaseOnce(i) {
      if (i.quantity === 1) {
        this.confirmRemove(i)
        return
      }
      this.updateQuantity(i, i.quantity - 1)
    },

    async confirmRemove(i) {
      if (!window.confirm(`確定要移除「${i.productName}」嗎?`)) return

      try {
        await http.delete(`/ord/cart/remove/${i.cartItemId}`)
        await this.loadCart()
        await this.revalidatePromotion()
        await this.updateCouponHint()
        alert('✅ 已移除商品')
      } catch (err) {
        console.error('刪除失敗:', err)
        alert('刪除失敗')
      }
    },

    async checkout() {
      if (this.isCheckingOut || !this.canCheckout) return

      if (!this.receiverName?.trim() || !this.receiverPhone?.trim() || !this.receiverAddress?.trim()) {
        alert('請填寫完整的收件人資料')
        return
      }

      const auth = useAuthStore()
      if (!auth?.accessToken) {
        alert('請先登入會員再結帳')
        this.$router.push({ name: 'userlogin', query: { returnUrl: this.$route.fullPath } })
        return
      }

      this.isCheckingOut = true
      try {
        const validItems = this.cartItems.filter(i => i.isValid)
        
        const payload = {
          cartItems: validItems.map(i => ({
            productId: i.productId,
            skuId: i.skuId,
            productName: i.productName,
            salePrice: i.salePrice,
            quantity: i.quantity
          })),
          receiverName: this.receiverName,
          receiverPhone: this.receiverPhone,
          receiverAddress: this.receiverAddress,
          couponCode: this.couponCode || null,
          couponWalletId: this.appliedCouponWalletId || null,
          logisticsId: Number(this.selectedLogisticsId),
          shippingFee: Number(this.shippingFee)
        }

        const res = await http.post('/ord/cart/checkout', payload)

        if (res?.data?.success) {
          const html = res.data.ecpayFormHtml
          if (!html) throw new Error('後端未回傳 ecpayFormHtml')

          const parser = new DOMParser()
          const doc = parser.parseFromString(html, 'text/html')
          const form = doc.querySelector('form')
          if (!form) throw new Error('找不到綠界 <form>')

          document.body.appendChild(form)
          form.submit()
          return
        }

        const msg = res?.data?.errors?.length
          ? res.data.errors.join('\n')
          : (res?.data?.message || '結帳失敗')
        alert('❌ ' + msg)
      } catch (err) {
        if (err?.response?.status === 401 || err?.response?.status === 403) {
          alert('登入逾時,請重新登入')
          this.$router.push({ name: 'userlogin', query: { returnUrl: this.$route.fullPath } })
          return
        }

        let errorMsg = err?.response?.data?.message || err?.message || '結帳失敗'
        alert('❌ ' + errorMsg)
        console.error('checkout error:', err)
      } finally {
        this.isCheckingOut = false
      }
    },

    continueShopping() {
      this.$router.push({ name: 'home' })
    }
  },

  async mounted() {
    await this.loadCart()
  }
}
</script>

<style scoped>
.text-teal { color: #007083; }
.disabled-item {
  opacity: 0.6;
  background-color: #f8f9fa !important;
}
.product-card{border:1px solid #e9ecef;border-radius:12px;background:#fff;transition:box-shadow .2s,transform .12s}
.product-card:hover{box-shadow:0 10px 24px rgba(0,0,0,.08);transform:translateY(-1px)}
.quantity-row{display:flex;align-items:center;justify-content:center;gap:8px;height:42px}
.circle-btn{width:42px;height:42px;border-radius:50%;border:none;background:#007083;color:#fff;font-size:1.35rem;font-weight:700;display:flex;align-items:center;justify-content:center;transition:all .2s ease;cursor:pointer}
.circle-btn:hover:not(:disabled){background:#0096a8;box-shadow:0 2px 6px rgba(0,0,0,.15)}
.circle-btn:disabled{background:#ccc;cursor:not-allowed;opacity:.6}
.qty-input{width:56px;height:42px;text-align:center;border:1.5px solid #ccc;border-radius:8px;font-weight:700;font-size:1.1rem;background:#fff}
.summary-row{display:flex;justify-content:space-between;align-items:center;margin-bottom:10px;font-size:1.1rem}

/* 🔥 新增：已套用優惠券顯示框 */
.applied-coupon-box {
  background: linear-gradient(135deg, 
    rgba(40, 167, 69, 0.08) 0%, 
    rgba(34, 139, 58, 0.12) 100%);
  border: 2px solid rgba(40, 167, 69, 0.3);
  border-radius: 10px;
  padding: 12px 14px;
  transition: all 0.3s ease;
}

.applied-coupon-box:hover {
  border-color: rgba(40, 167, 69, 0.5);
  box-shadow: 0 2px 8px rgba(40, 167, 69, 0.15);
}

.coupon-nearly-hint {
  background: linear-gradient(135deg, 
    rgba(0, 112, 131, 0.05) 0%, 
    rgba(77, 180, 193, 0.08) 100%);
  border: 2px solid rgba(0, 112, 131, 0.25);
  border-radius: 12px;
  padding: 16px;
  position: relative;
  overflow: hidden;
  transition: all 0.3s ease;
  box-shadow: 0 2px 8px rgba(0, 112, 131, 0.1);
}

.coupon-nearly-hint:hover {
  border-color: rgba(0, 112, 131, 0.4);
  box-shadow: 0 4px 12px rgba(0, 112, 131, 0.2);
  transform: translateY(-2px);
}

.coupon-nearly-hint .bi-lightbulb-fill {
  color: rgb(0, 112, 131);
  font-size: 1.3rem;
  animation: pulse 2s ease-in-out infinite;
}

.coupon-available-hint {
  background: linear-gradient(135deg, 
    rgba(40, 167, 69, 0.08) 0%, 
    rgba(34, 139, 58, 0.12) 100%);
  border: 2px solid rgba(40, 167, 69, 0.4);
  border-radius: 12px;
  padding: 16px;
  position: relative;
  overflow: hidden;
  transition: all 0.3s ease;
  box-shadow: 0 2px 8px rgba(40, 167, 69, 0.15);
  animation: slideIn 0.5s ease-out;
}

.coupon-available-hint:hover {
  border-color: rgba(40, 167, 69, 0.6);
  box-shadow: 0 4px 12px rgba(40, 167, 69, 0.25);
  transform: translateY(-2px);
}

.coupon-available-hint .gift-icon {
  color: #28a745;
  font-size: 1.5rem;
  animation: bounce 1s infinite;
}

.better-coupon-hint {
  background: linear-gradient(135deg, 
    rgba(255, 193, 7, 0.08) 0%, 
    rgba(255, 152, 0, 0.12) 100%);
  border: 2px solid rgba(255, 193, 7, 0.4);
  border-radius: 12px;
  padding: 16px;
  position: relative;
  overflow: hidden;
  transition: all 0.3s ease;
  box-shadow: 0 2px 8px rgba(255, 193, 7, 0.15);
  animation: slideIn 0.5s ease-out;
}

.better-coupon-hint:hover {
  border-color: rgba(255, 193, 7, 0.6);
  box-shadow: 0 4px 12px rgba(255, 193, 7, 0.25);
  transform: translateY(-2px);
}

.better-coupon-hint .star-icon {
  color: #ffc107;
  font-size: 1.4rem;
  animation: sparkle 1.5s ease-in-out infinite;
}

.better-coupon-hint .hint-subtitle {
  font-size: 0.9rem;
  color: #856404;
}

.better-coupon-hint .savings-info {
  font-size: 0.85rem;
  color: #28a745;
  font-weight: 600;
  margin-top: 6px;
}

.better-coupon-hint .savings-info i {
  margin-right: 4px;
}

.savings-info-green {
  font-size: 0.85rem;
  color: #28a745;
  font-weight: 600;
  margin-top: 6px;
}

.savings-info-green i {
  margin-right: 4px;
}

@keyframes sparkle {
  0%, 100% {
    opacity: 1;
    transform: scale(1) rotate(0deg);
  }
  50% {
    opacity: 0.7;
    transform: scale(1.2) rotate(15deg);
  }
}

@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateY(-10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes bounce {
  0%, 100% {
    transform: translateY(0);
  }
  50% {
    transform: translateY(-5px);
  }
}

.pulse-button {
  animation: pulseButton 2s infinite;
}

@keyframes pulseButton {
  0%, 100% {
    box-shadow: 0 0 0 0 rgba(40, 167, 69, 0.7);
  }
  50% {
    box-shadow: 0 0 0 10px rgba(40, 167, 69, 0);
  }
}

.hint-text {
  font-size: 1rem;
  color: #495057;
  font-weight: 500;
}

.amount-highlight {
  color: rgb(0, 112, 131);
  font-weight: 700;
  font-size: 1.15em;
}

.coupon-tag {
  display: inline-block;
  background: linear-gradient(135deg, rgb(0, 112, 131), rgb(0, 147, 171));
  color: white;
  padding: 5px 14px;
  border-radius: 20px;
  font-size: 0.9rem;
  font-weight: 700;
  box-shadow: 0 2px 6px rgba(0, 112, 131, 0.3);
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.2);
}

.coupon-tag i {
  margin-right: 4px;
}

.coupon-tag-available {
  display: inline-block;
  background: #1e7e34;
  color: white;
  padding: 7px 16px;
  border-radius: 20px;
  font-size: 0.95rem;
  font-weight: 700;
  box-shadow: 0 2px 8px rgba(30, 126, 52, 0.4);
  text-shadow: 0 1px 3px rgba(0, 0, 0, 0.3);
}

.coupon-tag-available i {
  margin-right: 4px;
}

.coupon-tag-better {
  display: inline-block;
  background: linear-gradient(135deg, #ff9800, #f57c00);
  color: white;
  padding: 7px 16px;
  border-radius: 20px;
  font-size: 0.95rem;
  font-weight: 700;
  box-shadow: 0 2px 8px rgba(255, 152, 0, 0.4);
  text-shadow: 0 1px 3px rgba(0, 0, 0, 0.3);
}

.coupon-tag-better i {
  margin-right: 4px;
}

.discount-badge {
  display: inline-block;
  background: rgba(255, 255, 255, 0.35);
  padding: 3px 10px;
  border-radius: 10px;
  margin-left: 6px;
  font-size: 0.85em;
  font-weight: 700;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.25);
}

.discount-badge-large {
  display: inline-block;
  background: rgba(255, 255, 255, 0.35);
  padding: 4px 12px;
  border-radius: 12px;
  margin-left: 6px;
  font-size: 0.9em;
  font-weight: 700;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.25);
}

.discount-badge-better {
  display: inline-block;
  background: rgba(255, 255, 255, 0.35);
  padding: 4px 12px;
  border-radius: 12px;
  margin-left: 6px;
  font-size: 0.9em;
  font-weight: 700;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.25);
}

.progress-container {
  width: 100%;
  height: 24px;
  background: rgba(0, 0, 0, 0.03);
  border-radius: 12px;
  overflow: hidden;
  position: relative;
  box-shadow: inset 0 1px 3px rgba(0, 0, 0, 0.08);
}

.progress-bar-custom {
  height: 100%;
  background: linear-gradient(90deg, 
    rgb(0, 112, 131) 0%, 
    rgb(77, 180, 193) 50%,
    rgb(0, 147, 171) 100%);
  background-size: 200% 100%;
  border-radius: 12px;
  transition: width 0.6s ease;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  padding-right: 8px;
  animation: shimmer 2s infinite;
}

.progress-bar-better {
  height: 100%;
  background: linear-gradient(90deg, 
    #ffc107 0%, 
    #ff9800 50%,
    #f57c00 100%);
  background-size: 200% 100%;
  border-radius: 12px;
  transition: width 0.6s ease;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  padding-right: 8px;
  animation: shimmer 2s infinite;
}

.progress-container-shipping {
  width: 100%;
  height: 24px;
  background: rgba(255, 193, 7, 0.1);
  border-radius: 12px;
  overflow: hidden;
  position: relative;
  box-shadow: inset 0 1px 3px rgba(0, 0, 0, 0.08);
}

.progress-bar-shipping {
  height: 100%;
  background: linear-gradient(90deg, 
    #ffc107 0%, 
    #ffb300 50%,
    #ffa000 100%);
  background-size: 200% 100%;
  border-radius: 12px;
  transition: width 0.6s ease;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  padding-right: 8px;
  animation: shimmer 2s infinite;
}

@keyframes shimmer {
  0% {
    background-position: 0% 50%;
  }
  50% {
    background-position: 100% 50%;
  }
  100% {
    background-position: 0% 50%;
  }
}

.progress-text {
  color: white;
  font-size: 0.75rem;
  font-weight: 700;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.3);
  white-space: nowrap;
}

.threshold-info {
  font-size: 0.9rem;
  color: #6c757d;
  display: flex;
  align-items: center;
}

.threshold-info i {
  margin-right: 6px;
  color: rgb(0, 112, 131);
}

.threshold-info.text-success i {
  color: #28a745;
}

.current-amount {
  color: #495057;
  font-weight: 600;
}

@keyframes pulse {
  0%, 100% {
    opacity: 1;
    transform: scale(1);
  }
  50% {
    opacity: 0.7;
    transform: scale(1.15);
  }
}

.free-shipping-hint {
  background: linear-gradient(135deg, 
    rgba(255, 235, 59, 0.15) 0%,
    rgba(255, 224, 130, 0.2) 100%);
  border: 2px solid rgba(255, 193, 7, 0.4);
  border-radius: 12px;
  padding: 16px;
  position: relative;
  overflow: hidden;
  transition: all 0.3s ease;
  box-shadow: 0 2px 8px rgba(255, 193, 7, 0.15);
}

.free-shipping-hint:hover {
  border-color: rgba(255, 193, 7, 0.6);
  box-shadow: 0 4px 12px rgba(255, 193, 7, 0.25);
  transform: translateY(-2px);
}

.free-shipping-hint .bi-truck {
  color: #ff6f00;
  font-size: 1.2rem;
}

.free-shipping-hint .hint-text {
  color: #5d4037;
  font-weight: 600;
}

.free-shipping-hint .amount-highlight-shipping {
  color: #ff6f00;
  font-weight: 700;
  font-size: 1.15em;
}

.free-shipping-achieved {
  background: linear-gradient(135deg, 
    rgba(40, 167, 69, 0.1) 0%, 
    rgba(34, 139, 58, 0.15) 100%);
  border: 2px solid rgba(40, 167, 69, 0.4);
  border-radius: 12px;
  padding: 14px 16px;
  font-size: 0.95rem;
  color: #155724;
  text-align: center;
  font-weight: 600;
  box-shadow: 0 2px 8px rgba(40, 167, 69, 0.15);
  transition: all 0.3s ease;
}

.free-shipping-achieved:hover {
  box-shadow: 0 4px 12px rgba(40, 167, 69, 0.25);
  transform: translateY(-1px);
}

.free-shipping-achieved i {
  margin-right: 8px;
  font-size: 1.1rem;
  color: #28a745;
}

@media (max-width: 576px) {
  .coupon-nearly-hint,
  .coupon-available-hint,
  .better-coupon-hint,
  .free-shipping-hint {
    font-size: 0.9rem;
    padding: 12px;
  }
  
  .progress-container,
  .progress-container-shipping {
    height: 20px;
  }
  
  .progress-text {
    font-size: 0.7rem;
  }
  
  .coupon-tag,
  .coupon-tag-available,
  .coupon-tag-better {
    display: block;
    margin: 8px 0;
    text-align: center;
  }
}

.teal-reflect-button{background:linear-gradient(135deg,#007083 0%,#00a0b8 100%);color:white;border:none;transition:all .3s ease;font-weight:600}
.teal-reflect-button:hover:not(:disabled){background:linear-gradient(135deg,#00586a 0%,#008a9f 100%);transform:translateY(-2px);box-shadow:0 4px 12px rgba(0,112,131,.3)}
.teal-reflect-button:disabled{background:#ccc;cursor:not-allowed;transform:none;opacity:.6}
.silver-reflect-button{background:linear-gradient(135deg,#6c757d 0%,#9ca3af 100%);color:white;border:none;transition:all .3s ease;font-weight:600}
.silver-reflect-button:hover:not(:disabled){background:linear-gradient(135deg,#5a6268 0%,#868e96 100%);transform:translateY(-2px);box-shadow:0 4px 12px rgba(108,117,125,.3)}
.silver-reflect-button:disabled{background:#ccc;cursor:not-allowed;transform:none;opacity:.6}
.spinner-border-sm{width:1rem;height:1rem;border-width:.15em}
@import url('https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css');
</style>