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
          <div class="input-group mb-2">
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

          <!-- 🔥 自動找最優惠券按鈕 -->
          <button 
            class="btn btn-outline-success w-100 mb-4" 
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

          <!-- 🔥 免運提示（緊接在運費後） -->
          <div v-if="amountAfterCoupon > 0 && amountAfterCoupon < 1500" class="free-shipping-hint mt-2 mb-3">
            <i class="bi bi-truck"></i>
            再購買 <strong class="text-danger">NT$ {{ (1500 - amountAfterCoupon).toLocaleString() }}</strong> 即可免運
          </div>
          <div v-else-if="amountAfterCoupon >= 1500" class="free-shipping-achieved mt-2 mb-3">
            <i class="bi bi-check-circle-fill"></i>
            已達免運門檻
          </div>

          <!-- 🔔 最接近可用的優惠券提示 -->
          <div v-if="nearestCouponHint" class="coupon-nearly-hint mt-2 mb-3">
            <i class="bi bi-lightbulb"></i>
            再買 <strong>NT$ {{ nearestCouponHint.gap.toLocaleString() }}</strong>
            可用 <strong>{{ nearestCouponHint.couponName }}</strong> 優惠券
            <span v-if="nearestCouponHint.threshold" class="text-muted small">
              （門檻 NT$ {{ nearestCouponHint.threshold.toLocaleString() }}）
            </span>
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
      nearestCouponHint: null
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
    // 🔥 新增：扣除優惠券後的金額
    amountAfterCoupon() {
      return this.subtotal - this.promotionDiscount
    },
    finalTotal() {
      return Math.max(0, this.subtotal - this.promotionDiscount + this.shippingFee)
    }
  },
  methods: {
    async loadCart() {
      try {
        const res = await http.get('/ord/cart/get')
        if (res?.data?.success) {
          this.cartItems = res.data.data.items || []
          this.canCheckout = res.data.data.canCheckout ?? true
          this.invalidCount = res.data.data.invalidCount || 0
          
          if (this.cartItems.length > 0) {
            await this.calculateShippingFee()
          }
        }
      } catch (err) {
        console.error('載入購物車失敗:', err)
        alert('無法載入購物車')
      }
    },

    // 🔥 修正：用扣除優惠券後的金額判斷免運
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

        // const res = await http.post('/promotion/calculate', payload)

        // if (res?.data?.success && res.data.data?.discounts?.length > 0) {
        //   return { isValid: true, data: res.data.data }
        // } else {
        //   if (showAlert) {
        //     const msg = res?.data?.message || '不符合優惠券使用規定'
        //     alert('❌ ' + msg)
        //   }
        //   return { isValid: false, data: null }
        // }
          const res = await http.post('/promotion/calculate', payload)
  // 成功：有折扣
  if (res?.data?.success && res.data.data?.discounts?.length > 0) {
    return { isValid: true, data: res.data.data, reason: null }
  }
  // 失敗：解析原因（如：未達滿額 800 元）
  const msg = res?.data?.message || '不符合優惠券使用規定'
  // 嘗試抓門檻金額
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
        // 🔥 優惠券變動後重新計算運費
        await this.calculateShippingFee()
      } else {
        console.log('❌ 優惠券已失效，清空並通知')
        this.couponCode = ''
        this.promotionResult = null
        // 🔥 優惠券清空後重新計算運費
        await this.calculateShippingFee()
        alert(`⚠️ 優惠券「${savedCouponCode}」已取消使用\n原因：不符合優惠券使用條件`)
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
        // 🔥 套用優惠券後重新計算運費
        await this.calculateShippingFee()
        alert('✅ 優惠券套用成功')
      } else {
        this.couponCode = ''
        this.promotionResult = null
      }
    },

    async findBestCoupon() {
      try {
        const auth = useAuthStore()
        
        if (!auth?.accessToken) {
          console.warn('未登入，無法查詢優惠券')
          return null
        }

        if (this.cartItems.length === 0 || this.subtotal <= 0) {
          console.warn('購物車為空或金額為0，無法計算優惠券')
          return null
        }

        const userNumberId = auth.user?.userNumberId || auth.userNumberId
        if (!userNumberId || userNumberId <= 0) {
          console.warn('無法取得會員ID')
          return null
        }

        const walletRes = await http.get('/user/coupons/wallet?onlyUsable=true&pageSize=100')
        const availableCoupons = walletRes?.data?.items || []
        
        if (availableCoupons.length === 0) {
          console.log('目前沒有可用的優惠券')
          return null
        }

        console.log(`找到 ${availableCoupons.length} 張可用優惠券，開始計算...`)

        const results = []
        const applicable = []  // 可立即使用
        const nearly = []      // 差門檻（gap 最小者）
        
        for (const item of availableCoupons) {
          if (!item.isUsable || !item.coupon?.couponCode) continue
          
           const result = await this.validateCoupon(
            { couponId: item.coupon.couponId, couponCode: item.coupon.couponCode },
            false)
          
            if (result.isValid) {
        const discount = result.data.discounts[0]
       applicable.push({
        couponWalletId: item.couponWalletId,
          couponId: item.coupon.couponId,
          couponCode: item.coupon.couponCode,
          couponName: item.coupon.couponName || item.coupon.couponCode,
          discountAmount: discount.discountAmount || 0,
          promotionResult: result.data
        })
     } else if (result?.reason?.code === 'min_spend_not_met') {
       nearly.push({
         couponId: item.coupon.couponId,
         couponCode: item.coupon.couponCode,
         couponName: item.coupon.couponName || item.coupon.couponCode,
         threshold: result.reason.threshold,
         gap: result.reason.gap
       })
      } else {
        console.warn('[coupon not applicable]', {
          couponId: item.coupon.couponId,
          couponCode: item.coupon.couponCode
        })
      }
    }

      if (applicable.length > 0) {
     applicable.sort((a, b) => b.discountAmount - a.discountAmount)
     const best = applicable[0]
     console.log(`最優惠券: ${best.couponName}, 折扣: NT$ ${best.discountAmount}`)
     this.nearestCouponHint = null
     return { type: 'applicable', best, applicable, nearly }
   }
   if (nearly.length > 0) {
     nearly.sort((a, b) => a.gap - b.gap) // 差最少的最先
     const closest = nearly[0]
     console.log(`尚未達門檻，距離最接近券：${closest.couponName}，還差 NT$${closest.gap}`)
     this.nearestCouponHint = {
       couponName: closest.couponName,
       threshold: closest.threshold,
       gap: closest.gap,
       couponCode: closest.couponCode
     }
     return { type: 'nearly', closest, applicable, nearly }
   }
   console.log('沒有優惠券符合使用條件')
   this.nearestCouponHint = null
   return { type: 'none' }
  } catch (err) {
    console.error('查詢最優惠券失敗:', err)
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
     alert(`✅ 已自動套用最優惠券：${best.couponName}\n折扣金額：NT$ ${best.discountAmount.toLocaleString()}`)
     this.nearestCouponHint = null
     return
   }
   if (pick.type === 'nearly') {
     const c = pick.closest
     // 不套用，只提示差額與門檻
     alert(`⚠️ 目前未達門檻\n最接近可用的優惠券：${c.couponName}\n門檻：NT$ ${c.threshold.toLocaleString()}\n還差：NT$ ${c.gap.toLocaleString()}\n\n小提醒：再加購即可使用此券！`)
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

        console.log('=== 結帳 Payload ===')
        console.log('logisticsId:', payload.logisticsId, typeof payload.logisticsId)
        console.log('shippingFee:', payload.shippingFee, typeof payload.shippingFee)
        console.log('完整 payload:', JSON.stringify(payload, null, 2))

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
.summary-row{display:flex;justify-content:space-between;align-items:center;margin-bottom:10px;font-size:1.05rem}

/* 🔥 免運提示樣式 */
.free-shipping-hint {
  background: linear-gradient(135deg, #fff3cd 0%, #ffeaa7 100%);
  border: 1px solid #ffc107;
  border-radius: 8px;
  padding: 10px 12px;
  font-size: 0.9rem;
  color: #856404;
  text-align: center;
}

.free-shipping-hint i {
  margin-right: 6px;
  font-size: 1rem;
}

.free-shipping-achieved {
  background: linear-gradient(135deg, #d4edda 0%, #c3e6cb 100%);
  border: 1px solid #28a745;
  border-radius: 8px;
  padding: 10px 12px;
  font-size: 0.9rem;
  color: #155724;
  text-align: center;
  font-weight: 600;
}

.free-shipping-achieved i {
  margin-right: 6px;
  font-size: 1rem;
  color: #28a745;
}

/*最接近可用券提示 */
.coupon-nearly-hint {
  background: linear-gradient(135deg, #e8f4ff 0%, #e0f0ff 100%);
  border: 1px solid #86b7fe;
 border-radius: 8px;
  padding: 10px 12px;
  font-size: 0.9rem;
  color: #0d6efd;
  text-align: center;
  font-weight: 600;
}
.coupon-nearly-hint i {
  margin-right: 6px;
  font-size: 1rem;
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