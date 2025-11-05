<template>
  <div class="container my-5">
    <div class="d-flex align-items-center mb-4">
      <i class="bi bi-cart3 me-3" style="font-size: 2rem"></i>
      <h2 class="fw-bold text-teal mb-0">購物車測試</h2>
    </div>

    <div class="row g-4">
      <div class="col-lg-8">
        <div class="card shadow-sm border-0 p-4 mb-3">
          <h5 class="fw-bold mb-3 text-teal"><i class="bi bi-bag"></i> 商品明細</h5>
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
                >
                  -
                </button>
                <input type="text" class="qty-input" :value="item.quantity" readonly />
                <button
                  class="circle-btn"
                  @click="increaseQuantity(item)"
                  :disabled="isCheckingOut"
                  title="增加數量"
                >
                  +
                </button>
              </div>

              <div class="text-teal fw-bold fs-5" style="min-width: 96px">
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

          <div v-if="cartItems.length === 0" class="text-center py-5">
            <i class="bi bi-cart-x" style="font-size: 4rem; color: #ccc"></i>
            <h4 class="mt-3 text-muted">購物車是空的</h4>
            <button class="btn btn-primary mt-3" @click="continueShopping">
              <i class="bi bi-arrow-left"></i> 繼續購物
            </button>
          </div>
        </div>

        <div class="card shadow-sm border-0 p-4 mb-3" v-if="cartItems.length > 0">
          <h5 class="fw-bold mb-3 text-teal"><i class="bi bi-truck"></i> 配送資訊</h5>

          <div class="mb-4">
            <label class="form-label fw-bold">選擇配送方式：</label>
            <div>
              <div class="form-check form-check-inline me-4">
                <input
                  class="form-check-input"
                  type="radio"
                  id="deliveryHome"
                  value="HOME"
                  v-model="deliveryMethod"
                  @change="onDeliveryMethodChange"
                />
                <label class="form-check-label fs-5" for="deliveryHome">宅配到府</label>
              </div>
              <div class="form-check form-check-inline">
                <input
                  class="form-check-input"
                  type="radio"
                  id="deliveryCVS"
                  value="CVS"
                  v-model="deliveryMethod"
                  @change="onDeliveryMethodChange"
                />
                <label class="form-check-label fs-5" for="deliveryCVS">超商取貨</label>
              </div>
            </div>
          </div>

          <div v-if="deliveryMethod === 'CVS'" class="ps-4 border-start border-4 border-teal">
            <h6 class="fw-bold mb-3">取貨門市</h6>

            <div v-if="cartStore.pickupStore">
              <div
                class="alert alert-success d-flex justify-content-between align-items-center mb-3"
              >
                <div>
                  <span class="badge bg-success me-2">{{
                    getStoreTypeName(cartStore.pickupStore.subType)
                  }}</span>
                  <strong class="fs-5">{{ cartStore.pickupStore.storeName }}</strong>
                  <div class="text-muted mt-1">{{ cartStore.pickupStore.address }}</div>
                </div>
                <i class="bi bi-check-circle-fill text-success fs-3"></i>
              </div>

              <div class="mb-3">
                <SelectedStoreMap
                  :address="cartStore.pickupStore.address"
                  :store-name="cartStore.pickupStore.storeName"
                />
              </div>
            </div>

            <div v-else class="alert alert-warning">
              <i class="bi bi-exclamation-triangle-fill me-2"></i> 請點擊下方按鈕選擇取貨門市
            </div>

            <div class="d-flex gap-3 mt-3">
              <button
                class="btn btn-outline-teal flex-grow-1 py-2"
                @click="chooseStore('UNIMARTC2C')"
                :disabled="isCheckingOut || isLoadingMap"
              >
                <img src="https://www.7-11.com.tw/favicon.ico" alt="7-11" width="20" class="me-1" />
                {{ cartStore.pickupStore?.subType === 'UNIMARTC2C' ? '重新選擇' : '選擇' }} 7-ELEVEN
              </button>
              <button
                class="btn btn-outline-primary flex-grow-1 py-2"
                @click="chooseStore('FAMIC2C')"
                :disabled="isCheckingOut || isLoadingMap"
              >
                <img
                  src="https://www.family.com.tw/favicon.ico"
                  alt="FamilyMart"
                  width="20"
                  class="me-1"
                />
                {{ cartStore.pickupStore?.subType === 'FAMIC2C' ? '重新選擇' : '選擇' }} 全家
              </button>
            </div>

            <div v-if="isLoadingMap" class="text-center mt-2 text-muted">
              <span class="spinner-border spinner-border-sm me-1"></span> 正在開啟地圖...
            </div>
          </div>

          <div v-if="deliveryMethod === 'HOME'" class="ps-4 border-start border-4 border-secondary">
            <h6 class="fw-bold mb-3">收件人資訊</h6>
            <div class="row g-3">
              <div class="col-md-6">
                <label class="form-label">收件人姓名 <span class="text-danger">*</span></label>
                <input
                  type="text"
                  class="form-control"
                  v-model="receiverName"
                  placeholder="請輸入真實姓名"
                />
              </div>
              <div class="col-md-6">
                <label class="form-label">手機號碼 <span class="text-danger">*</span></label>
                <input
                  type="tel"
                  class="form-control"
                  v-model="receiverPhone"
                  placeholder="例: 0912345678"
                />
              </div>
              <div class="col-12">
                <label class="form-label">收件地址 <span class="text-danger">*</span></label>
                <input
                  type="text"
                  class="form-control"
                  v-model="receiverAddress"
                  placeholder="請輸入完整地址 (含縣市區域)"
                />
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="col-lg-4">
        <div class="card shadow-sm border-0 sticky-top p-4" style="top: 20px">
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
            <button class="btn teal-reflect-button" @click="applyCoupon" :disabled="isCheckingOut">
              套用
            </button>
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
            <span v-if="!isCheckingOut"> <i class="bi bi-credit-card"></i> 前往結帳 </span>
            <span v-else>
              <span class="spinner-border spinner-border-sm me-2"></span>
              正在處理...
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
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'
import SelectedStoreMap from '@/components/modules/sup/logistics/SelectedStoreMap.vue'

// import { useCartStore } from '@/components/modules/ord/composables/useCart.js'
import { useTestCartStore } from '@/components/modules/sup/logistics/testcart.js'

import { useRouter } from 'vue-router'

const router = useRouter()

// const cartStore = useCartStore()
const cartStore = useTestCartStore()

// ================= 狀態定義 =================
const isCheckingOut = ref(false)
const isLoadingMap = ref(false)
const couponCode = ref('')
const deliveryMethod = ref('HOME') // 'HOME' | 'CVS'
const receiverName = ref('')
const receiverPhone = ref('')
const receiverAddress = ref('')

// 假資料 (實際應從後端 API 取得)
const cartItems = ref([
  {
    productId: 14246,
    skuId: 2680,
    productName: 'Lake Avenue Nutrition, Omega-3 魚油',
    skuName: '30 單位',
    unitPrice: 500.0,
    salePrice: 346.0,
    quantity: 1,
    subtotal: 346.0,
  },
  {
    productId: 14246,
    skuId: 3388,
    productName: 'Lake Avenue Nutrition, Omega-3 魚油',
    skuName: '90 單位',
    unitPrice: 1000.0,
    salePrice: 898.0,
    quantity: 1,
    subtotal: 898.0,
  },
])

// ================= 計算屬性 =================
const subtotalBeforeDiscount = computed(() =>
  cartItems.value.reduce((s, i) => s + i.unitPrice * i.quantity, 0),
)
const productDiscount = computed(() =>
  cartItems.value.reduce((s, i) => s + (i.unitPrice - i.salePrice) * i.quantity, 0),
)
const subtotal = computed(() => cartItems.value.reduce((s, i) => s + i.subtotal, 0))
const finalTotal = computed(() => subtotal.value)

// ================= 方法定義 =================

// 購物車操作 (保持原樣，僅改為 setup 寫法)
function updateSubtotal(i) {
  i.subtotal = i.salePrice * i.quantity
}
function increaseQuantity(i) {
  if (i.quantity < 99) {
    i.quantity++
    updateSubtotal(i)
  }
}
function decreaseOnce(i) {
  if (i.quantity === 1) {
    confirmRemove(i)
    return
  }
  i.quantity--
  updateSubtotal(i)
}
function confirmRemove(i) {
  if (window.confirm(`確定要移除「${i.productName}」嗎?`)) {
    cartItems.value = cartItems.value.filter(
      (x) => !(x.productId === i.productId && x.skuId === i.skuId),
    )
  }
}
function applyCoupon() {
  alert('這是示範用優惠券功能\n代碼:' + couponCode.value)
}
function continueShopping() {
  router.push('/')
}

// 物流相關
function onDeliveryMethodChange() {
  // 當切換回宅配時，可選擇是否清除已選門市，或保留給下次切換回來用
  // cartStore.clearPickupStore()
}

function getStoreTypeName(subType) {
  if (subType === 'UNIMARTC2C') return '7-ELEVEN'
  if (subType === 'FAMIC2C') return '全家 FamilyMart'
  return subType
}

// 🔥 開啟綠界電子地圖
// 🔥 開啟綠界電子地圖 (修正版)
async function chooseStore(logisticsSubType) {
  // 1. 避免重複點擊
  if (isLoadingMap.value) return
  isLoadingMap.value = true

  try {
    // 請確認此 URL 是否為您目前正在執行的後端位址 (localhost 或 ngrok)
    const apiUrl = 'https://localhost:7103/api/sup/logistics/map'

    console.log(`準備呼叫後端: ${apiUrl}, 類型: ${logisticsSubType}`)

    const res = await axios.post(apiUrl, {
      LogisticsSubType: logisticsSubType,
      IsCollection: false,
      Device: 0,
    })

    console.log('收到後端回應 HTML，準備跳轉...')

    // 2. 建立一個暫時的容器來放回傳的 HTML
    const div = document.createElement('div')
    // 將不可見設為 true，避免畫面閃爍
    div.style.display = 'none'
    div.innerHTML = res.data
    document.body.appendChild(div)

    // 3. 🔥 關鍵修改：手動抓取表單並送出 (不依賴回傳 HTML 裡的 script)
    const form = div.querySelector('form')
    if (form) {
      form.submit()
      // 注意：成功送出後頁面會跳轉，所以這裡不用把 isLoadingMap 改回 false
    } else {
      throw new Error('找不到綠界跳轉表單，請檢查後端回傳內容')
    }
  } catch (err) {
    console.error('開啟地圖失敗:', err)
    alert('無法開啟門市地圖，請檢查 Console 錯誤訊息')
    // 發生錯誤時，一定要把 loading 狀態改回來，不然會一直轉圈圈
    isLoadingMap.value = false
  }
}

// 🔥 結帳流程 (整合物流資訊)
async function checkout() {
  if (isCheckingOut.value) return

  // 1. 基本驗證
  if (cartItems.value.length === 0) return alert('購物車是空的!')

  // 2. 物流驗證
  if (deliveryMethod.value === 'CVS' && !cartStore.pickupStore) {
    return alert('請選擇取貨門市！')
  }
  if (
    deliveryMethod.value === 'HOME' &&
    (!receiverName.value || !receiverPhone.value || !receiverAddress.value)
  ) {
    return alert('請填寫完整的收件人資訊！')
  }

  isCheckingOut.value = true

  // 3. 組裝 Payload
  const payload = {
    sessionId: 'session123', // 假資料
    userNumberId: 1056, // 假資料
    cartItems: cartItems.value.map((i) => ({
      productId: i.productId,
      skuId: i.skuId,
      quantity: i.quantity,
      salePrice: i.salePrice, // 實際應由後端重算
    })),
    couponCode: couponCode.value || null,

    // 新增: 物流資訊
    deliveryMethod: deliveryMethod.value,
    logisticsInfo:
      deliveryMethod.value === 'CVS'
        ? {
            type: 'CVS',
            subType: cartStore.pickupStore.subType,
            storeId: cartStore.pickupStore.storeId,
            storeName: cartStore.pickupStore.storeName,
            address: cartStore.pickupStore.address,
          }
        : {
            type: 'HOME',
            receiverName: receiverName.value,
            receiverPhone: receiverPhone.value,
            receiverAddress: receiverAddress.value,
          },
  }

  console.log('📦 結帳 Payload:', payload)

  try {
    // 呼叫後端結帳 API (請確認 port 號)
    const res = await axios.post('http://localhost:7200/api/ord/cart/checkout', payload)

    if (res.data?.success && res.data?.ecpayFormHtml) {
      // 自動提交綠界金流表單
      const div = document.createElement('div')
      div.innerHTML = res.data.ecpayFormHtml
      document.body.appendChild(div)
      div.querySelector('form').submit()
    } else {
      throw new Error(res.data?.message || '結帳失敗')
    }
  } catch (error) {
    console.error('結帳錯誤', error)
    alert('結帳失敗: ' + (error.response?.data?.message || error.message))
    isCheckingOut.value = false
  }
}

// ================= 生命週期 =================
onMounted(() => {
  cartStore.initCart()
  // 若有已選門市，自動切到超商模式
  if (cartStore.pickupStore) {
    deliveryMethod.value = 'CVS'
  }
})
</script>

<style scoped>
.text-teal {
  color: #007083;
}
.border-teal {
  border-color: #007083 !important;
}

/* ... (其餘樣式保持您原有的設定) ... */
.product-card {
  border: 1px solid #e9ecef;
  border-radius: 12px;
  background: #fff;
  transition: box-shadow 0.2s;
}
.product-card:hover {
  box-shadow: 0 10px 24px rgba(0, 0, 0, 0.08);
  transform: translateY(-1px);
}

/* 數量控制 */
.quantity-row {
  display: flex;
  align-items: center;
  gap: 8px;
}
.circle-btn {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  border: none;
  background: #007083;
  color: #fff;
  font-weight: bold;
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
  width: 50px;
  text-align: center;
  border: 1px solid #ccc;
  border-radius: 4px;
  height: 36px;
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
  background: linear-gradient(135deg, #007083, #00a0b8);
  color: white;
  border: none;
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
  background: linear-gradient(135deg, #6c757d, #9ca3af);
  color: white;
  border: none;
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

/* 新增樣式 */
.form-check-input:checked {
  background-color: #007083;
  border-color: #007083;
}
</style>
