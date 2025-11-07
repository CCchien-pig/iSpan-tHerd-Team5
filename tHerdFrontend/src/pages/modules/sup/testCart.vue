<template>
  <div class="container my-5">
    <div class="d-flex align-items-center mb-4">
      <i class="bi bi-cart3 me-3 icon-teal" style="font-size: 2rem"></i>
      <h2 class="fw-bold text-teal mb-0">購物車</h2>
    </div>

    <div class="row g-4">
      <div class="col-lg-8">
        <div class="card shadow-sm border-0 p-4 mb-3">
          <h5 class="fw-bold mb-3 text-teal"><i class="bi bi-bag me-2"></i>商品明細</h5>

          <div v-if="cartItems.length === 0" class="text-center py-5">
            <i class="bi bi-cart-x icon-gray" style="font-size: 4rem"></i>
            <h4 class="mt-3 text-muted">購物車是空的</h4>
            <button class="btn btn-primary mt-3" @click="continueShopping">
              <i class="bi bi-arrow-left me-2"></i> 繼續購物
            </button>
          </div>

          <div
            v-for="item in cartItems"
            :key="`${item.productId}-${item.skuId}`"
            class="product-card p-4 mb-3 d-flex justify-content-between align-items-center"
          >
            <div class="flex-grow-1 pe-3">
              <h5 class="fw-bold mb-1">{{ item.productName }}</h5>
              <p class="text-muted mb-1"><i class="bi bi-tag me-1"></i>規格：{{ item.skuName }}</p>
              <div class="text-muted text-decoration-line-through small">
                NT$ {{ item.unitPrice.toLocaleString() }}
              </div>
              <div class="text-danger fw-bold fs-5 mb-0">
                NT$ {{ item.salePrice.toLocaleString() }}
              </div>
            </div>

            <div class="text-end d-flex align-items-center gap-3">
              <div class="quantity-row">
                <button class="circle-btn" @click="decreaseOnce(item)" :disabled="isCheckingOut">
                  -
                </button>
                <input type="text" class="qty-input" :value="item.quantity" readonly />
                <button
                  class="circle-btn"
                  @click="increaseQuantity(item)"
                  :disabled="isCheckingOut"
                >
                  +
                </button>
              </div>
              <div class="text-teal fw-bold fs-5" style="min-width: 96px">
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
        </div>

        <div class="card shadow-sm border-0 p-4 mb-3" v-if="cartItems.length > 0">
          <h5 class="fw-bold mb-3 text-teal"><i class="bi bi-truck me-2"></i>配送資訊</h5>

          <div class="mb-4 ms-2">
            <label class="form-label fw-bold fs-5 me-3">配送方式：</label>
            <div class="form-check form-check-inline me-3">
              <input
                class="form-check-input"
                type="radio"
                id="deliveryHome"
                value="HOME"
                v-model="deliveryMethod"
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
              />
              <label class="form-check-label fs-5" for="deliveryCVS">超商取貨</label>
            </div>
          </div>

          <div
            v-if="deliveryMethod === 'CVS'"
            class="px-4 py-3 border-start border-4 border-teal ms-2"
          >
            <h6 class="fw-bold mb-3">取貨門市</h6>

            <div v-if="cartStore.pickupStore" class="mb-3">
              <div
                class="alert alert-success d-flex justify-content-between align-items-center mb-3 p-3"
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
              <SelectedStoreMap
                :address="cartStore.pickupStore.address"
                :store-name="cartStore.pickupStore.storeName"
              />
            </div>
            <div v-else class="alert alert-warning mb-3">
              <i class="bi bi-exclamation-triangle-fill me-2"></i> 請點擊下方按鈕選擇取貨門市
            </div>

            <div class="d-flex gap-3 mb-4">
              <button
                class="btn btn-outline-primary flex-grow-1 py-2 store-select-btn d-flex align-items-center justify-content-center"
                @click="chooseStore('UNIMARTC2C')"
                :disabled="isCheckingOut || isLoadingMap"
              >
                <img src="https://www.7-11.com.tw/favicon.ico" alt="7-11" width="20" class="me-2" />
                <span
                  >{{
                    cartStore.pickupStore?.subType === 'UNIMARTC2C' ? '重新選擇' : '選擇'
                  }}
                  7-ELEVEN</span
                >
              </button>
              <button
                class="btn btn-outline-primary flex-grow-1 py-2 store-select-btn d-flex align-items-center justify-content-center"
                @click="chooseStore('FAMIC2C')"
                :disabled="isCheckingOut || isLoadingMap"
              >
                <img
                  src="https://www.family.com.tw/favicon.ico"
                  alt="FamilyMart"
                  width="20"
                  class="me-2"
                />
                <span
                  >{{
                    cartStore.pickupStore?.subType === 'FAMIC2C' ? '重新選擇' : '選擇'
                  }}
                  全家</span
                >
              </button>
            </div>
            <div v-if="isLoadingMap" class="text-center mt-2 text-muted">
              <span class="spinner-border spinner-border-sm me-1"></span> 正在開啟地圖...
            </div>

            <h6 class="fw-bold mb-3">取貨人資訊</h6>
            <div class="row g-3">
              <div class="col-md-6">
                <label class="form-label">取貨人姓名 <span class="text-danger">*</span></label>
                <input
                  type="text"
                  class="form-control"
                  v-model.trim="receiverName"
                  placeholder="請輸入證件真實姓名"
                  :disabled="isCheckingOut"
                />
              </div>
              <div class="col-md-6">
                <label class="form-label">手機號碼 <span class="text-danger">*</span></label>
                <input
                  type="tel"
                  class="form-control"
                  v-model.trim="receiverPhone"
                  placeholder="例: 0912345678"
                  :disabled="isCheckingOut"
                />
              </div>
            </div>
            <div class="mt-3">
              <button
                class="btn btn-outline-secondary btn-sm"
                @click="demoFill"
                :disabled="isCheckingOut"
              >
                Demo 填入
              </button>
            </div>
          </div>

          <div
            v-if="deliveryMethod === 'HOME'"
            class="px-4 py-3 border-start border-4 border-secondary ms-2"
          >
            <h6 class="fw-bold mb-3">收件人資訊</h6>
            <div class="row g-3">
              <div class="col-md-6">
                <label class="form-label">收件人姓名 <span class="text-danger">*</span></label>
                <input
                  type="text"
                  class="form-control"
                  v-model.trim="receiverName"
                  placeholder="請輸入真實姓名"
                  :disabled="isCheckingOut"
                />
              </div>
              <div class="col-md-6">
                <label class="form-label">手機號碼 <span class="text-danger">*</span></label>
                <input
                  type="tel"
                  class="form-control"
                  v-model.trim="receiverPhone"
                  placeholder="例: 0912345678"
                  :disabled="isCheckingOut"
                />
              </div>
              <div class="col-12">
                <label class="form-label">收件地址 <span class="text-danger">*</span></label>
                <input
                  type="text"
                  class="form-control"
                  v-model.trim="receiverAddress"
                  placeholder="請輸入完整地址 (含縣市區域)"
                  :disabled="isCheckingOut"
                />
              </div>
            </div>
            <div class="mt-3">
              <button
                class="btn btn-outline-secondary btn-sm"
                @click="demoFill"
                :disabled="isCheckingOut"
              >
                Demo 填入
              </button>
            </div>
          </div>
        </div>
      </div>

      <div class="col-lg-4">
        <div class="card shadow-sm border-0 sticky-top p-4" style="top: 20px">
          <h5 class="fw-bold mb-3 text-teal"><i class="bi bi-receipt me-2"></i>訂單摘要</h5>

          <label class="fw-bold mb-2">優惠券代碼（可選）</label>
          <div class="input-group mb-3">
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
            <span class="text-muted text-decoration-line-through"
              >NT$ {{ subtotalBeforeDiscount.toLocaleString() }}</span
            >
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
          <div class="summary-row align-items-center mb-3">
            <h5 class="fw-bold mb-0">應付金額</h5>
            <h3 class="text-danger fw-bold mb-0">NT$ {{ finalTotal.toLocaleString() }}</h3>
          </div>

          <button
            class="btn w-100 py-3 mt-3 teal-reflect-button"
            @click="checkout"
            :disabled="isCheckingOut || !canCheckout"
          >
            <span v-if="!isCheckingOut"><i class="bi bi-credit-card me-2"></i>前往結帳 (綠界)</span>
            <span v-else
              ><span class="spinner-border spinner-border-sm me-2"></span> 正在處理...</span
            >
          </button>

          <button
            class="btn w-100 py-3 mt-2 silver-reflect-button"
            @click="continueShopping"
            :disabled="isCheckingOut"
          >
            <i class="bi bi-arrow-left me-2"></i>繼續購物
          </button>
        </div>
      </div>
    </div>

    <div id="ecpayFormContainer" style="display: none"></div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { http } from '@/api/http'
import { useAuthStore } from '@/stores/auth'
import { useTestCartStore } from '@/components/modules/sup/logistics/testcart'
import SelectedStoreMap from '@/components/modules/sup/logistics/SelectedStoreMap.vue'

const router = useRouter()
const authStore = useAuthStore()
const cartStore = useTestCartStore()

// ================= 狀態定義 =================
const isCheckingOut = ref(false)
const isLoadingMap = ref(false)
const couponCode = ref('')
const deliveryMethod = ref('HOME') // 預設宅配
const receiverName = ref('')
const receiverPhone = ref('')
const receiverAddress = ref('')

// 購物車資料 (這裡放假資料方便展示，實際請從 API 取得)
const cartItems = ref([
  {
    productId: 14246,
    skuId: 2680,
    productName: 'Lake Avenue Nutrition, Omega-3 魚油 30粒',
    skuName: '30 單位',
    unitPrice: 500.0,
    salePrice: 346.0,
    quantity: 1,
  },
  {
    productId: 14246,
    skuId: 3388,
    productName: 'Lake Avenue Nutrition, Omega-3 魚油 90粒',
    skuName: '90 單位',
    unitPrice: 1000.0,
    salePrice: 898.0,
    quantity: 1,
  },
])

// ================= 計算屬性 =================
const subtotalBeforeDiscount = computed(() =>
  cartItems.value.reduce((s, i) => s + i.unitPrice * i.quantity, 0),
)
const productDiscount = computed(() =>
  cartItems.value.reduce((s, i) => s + (i.unitPrice - i.salePrice) * i.quantity, 0),
)
const subtotal = computed(() => cartItems.value.reduce((s, i) => s + i.salePrice * i.quantity, 0))
const finalTotal = computed(() => subtotal.value)

// 驗證是否可結帳
const canCheckout = computed(() => {
  if (cartItems.value.length === 0) return false
  // 共通必填：姓名、電話
  if (!receiverName.value?.trim() || !receiverPhone.value?.trim()) return false

  if (deliveryMethod.value === 'HOME') {
    return !!receiverAddress.value?.trim() // 宅配需地址
  } else {
    return !!cartStore.pickupStore // 超商需已選門市
  }
})

// ================= 方法定義 =================
function increaseQuantity(i) {
  if (i.quantity < 99) i.quantity++
}
function decreaseOnce(i) {
  if (i.quantity === 1) {
    confirmRemove(i)
    return
  }
  i.quantity--
}
function confirmRemove(i) {
  if (window.confirm(`確定移除「${i.productName}」嗎?`)) {
    cartItems.value = cartItems.value.filter(
      (x) => !(x.productId === i.productId && x.skuId === i.skuId),
    )
  }
}
function applyCoupon() {
  if (!couponCode.value) return alert('請輸入優惠券代碼')
  alert('示範代碼: ' + couponCode.value)
}
function continueShopping() {
  router.push('/')
}
function demoFill() {
  receiverName.value = '測試收件人'
  receiverPhone.value = '0912345678'
  if (deliveryMethod.value === 'HOME') receiverAddress.value = '台北市中正區測試路1號'
}

// 取得商店類型名稱
function getStoreTypeName(subType) {
  const map = { UNIMARTC2C: '7-ELEVEN', FAMIC2C: '全家', HILIFEC2C: '萊爾富', OKMARTC2C: 'OK超商' }
  return map[subType] || subType
}

// 開啟綠界地圖
async function chooseStore(logisticsSubType) {
  if (isLoadingMap.value) return
  isLoadingMap.value = true
  try {
    // 呼叫後端取得地圖表單 HTML (請確認後端位址)
    const res = await http.post('https://localhost:7103/api/sup/logistics/map', {
      LogisticsSubType: logisticsSubType,
      IsCollection: false,
      Device: 0,
    })

    const div = document.createElement('div')
    div.style.display = 'none'
    div.innerHTML = res.data
    document.body.appendChild(div)
    const form = div.querySelector('form')
    if (form) form.submit()
    else throw new Error('無法載入綠界表單')
  } catch (err) {
    console.error('開啟地圖失敗', err)
    alert('開啟地圖失敗，請稍後再試')
    isLoadingMap.value = false
  }
}

// 結帳流程
async function checkout() {
  if (isCheckingOut.value || !canCheckout.value) return

  if (!authStore.accessToken) {
    alert('請先登入會員')
    router.push({ name: 'login', query: { redirect: router.currentRoute.value.fullPath } })
    return
  }

  isCheckingOut.value = true
  try {
    // 1. 準備物流資訊 payload
    let logisticsPayload = {}
    if (deliveryMethod.value === 'CVS') {
      const store = cartStore.pickupStore
      logisticsPayload = {
        deliveryMethod: 'CVS',
        logisticsSubType: store.subType,
        cvsStoreID: store.storeId, // 對應後端新欄位
        cvsStoreName: store.storeName, // 對應後端新欄位
        receiverAddress: store.address, // 超商時，地址填門市地址
        receiverName: receiverName.value,
        receiverPhone: receiverPhone.value,
      }
    } else {
      logisticsPayload = {
        deliveryMethod: 'HOME',
        logisticsSubType: 'TCAT', // 宅配預設黑貓
        cvsStoreID: null,
        cvsStoreName: null,
        receiverAddress: receiverAddress.value, // 宅配時，填用戶輸入的地址
        receiverName: receiverName.value,
        receiverPhone: receiverPhone.value,
      }
    }

    // 2. 組裝最終 payload
    const payload = {
      ...logisticsPayload,
      cartItems: cartItems.value.map((i) => ({
        productId: i.productId,
        skuId: i.skuId,
        quantity: i.quantity,
        salePrice: i.salePrice,
      })),
      couponCode: couponCode.value || null,
    }

    console.log('📦 結帳 Payload:', payload)

    // 3. 發送請求 (請確認訂單後端 Port)
    const res = await http.post('http://localhost:7200/api/ord/cart/checkout', payload)

    if (res.data?.success && res.data?.ecpayFormHtml) {
      const div = document.createElement('div')
      div.style.display = 'none'
      div.innerHTML = res.data.ecpayFormHtml
      document.body.appendChild(div)
      div.querySelector('form')?.submit()
    } else {
      throw new Error(res.data?.message || '結帳失敗')
    }
  } catch (err) {
    console.error('結帳錯誤', err)
    alert('結帳失敗: ' + (err.response?.data?.message || err.message))
    isCheckingOut.value = false
  }
}

// 初始化
onMounted(() => {
  // 確保 store 有初始化方法能從 localStorage 讀回資料
  if (cartStore.initCart) cartStore.initCart()
  // 如果有已選門市，自動切換到超商模式
  if (cartStore.pickupStore) {
    deliveryMethod.value = 'CVS'
  }
})

// 監聽切換
watch(deliveryMethod, (newMethod) => {
  // 切換模式時可做一些清空動作，目前暫留空
})
</script>

<style scoped>
/* 基本顏色 */
.text-teal,
.icon-teal {
  color: #007083;
}
.border-teal {
  border-color: #007083 !important;
}
.icon-gray {
  color: #ccc;
}

/* 卡片與商品卡樣式 */
.product-card {
  border: 1px solid #e9ecef;
  border-radius: 12px;
  background: #fff;
  transition:
    box-shadow 0.2s,
    transform 0.12s;
}
.product-card:hover {
  box-shadow: 0 10px 24px rgba(0, 0, 0, 0.08);
  transform: translateY(-1px);
}

/* 數量調整區塊 */
.quantity-row {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  height: 42px; /* 統一高度 */
}
.circle-btn {
  width: 36px; /* 調整大小 */
  height: 36px;
  border-radius: 50%;
  border: none;
  background: #007083;
  color: #fff;
  font-size: 1.2rem; /* 調整字體大小 */
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
  width: 50px; /* 調整寬度 */
  height: 36px; /* 調整高度 */
  text-align: center;
  border: 1.5px solid #ccc;
  border-radius: 8px;
  font-weight: 700;
  font-size: 1rem; /* 調整字體大小 */
  background: #fff;
}

/* 訂單摘要行 */
.summary-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 10px;
  font-size: 1.05rem;
}

/* 配送方式 Radio */
.form-check-input:checked {
  background-color: #007083;
  border-color: #007083;
}
.form-check-label {
  font-size: 1rem; /* 與設計稿保持一致 */
}

/* 超商選擇按鈕 */
.store-select-btn {
  border-color: #007083; /* 統一顏色 */
  color: #007083;
}
.store-select-btn:hover:not(:disabled) {
  background-color: #007083;
  color: white;
}
.store-select-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  background-color: #e9ecef; /* 更柔和的禁用背景色 */
  border-color: #e9ecef;
  color: #6c757d;
}

/* 主要按鈕樣式 */
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

.spinner-border-sm {
  width: 1rem;
  height: 1rem;
  border-width: 0.15em;
}

/* Google Map元件的樣式由SelectedStoreMap.vue控制 */

/* 引入 Bootstrap Icons (如果沒有全域引入的話) */
@import url('https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css');
</style>
