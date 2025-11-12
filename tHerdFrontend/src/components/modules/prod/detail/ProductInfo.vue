<!--
  ProductInfo.vue - 商品資訊組件
  功能：商品標題、評分、規格選擇、價格、加入購物車
-->
<template>
  <div class="product-info">
    <!-- 品牌優惠跑馬燈 -->
    <div v-if="brandPromotion" class="brand-promo-marquee">
      <div class="promo-track">
        <div class="promo-content">
          <i class="bi bi-megaphone-fill text-success me-2"></i>
          <strong class="me-2">{{ brandPromotion.title }}</strong>
          <span class="promo-desc">{{ brandPromotion.desc }}</span>
          <span v-if="brandPromotion.daysLeft > 0" class="countdown ms-3 text-danger fw-bold">
            ⏰ 剩 {{ brandPromotion.daysLeft }} 天
          </span>
        </div>
        <div class="promo-content">
          <i class="bi bi-megaphone-fill text-success me-2"></i>
          <strong class="me-2">{{ brandPromotion.title }}</strong>
          <span class="promo-desc">{{ brandPromotion.desc }}</span>
          <span v-if="brandPromotion.daysLeft > 0" class="countdown ms-3 text-danger fw-bold">
            ⏰ 剩 {{ brandPromotion.daysLeft }} 天
          </span>
        </div>
      </div>
    </div>

    <!-- 商品標籤 -->
    <div class="brand-badge mb-3">
      <span class="badge bg-warning text-dark">{{ product.badgeName }}</span>
    </div>

    <!-- 商品標題 -->
    <h1 class="product-title">{{ product.productName }}</h1>

    <!-- 品牌名稱 -->
    <p class="brand-name text-muted mb-2">由 {{ product.brandName }}</p>

    <!-- 評分與評價 -->
    <div class="rating-section mb-3">
      <span class="rating-value">{{ product.avgRating || 0 }}</span>
      <div class="stars">
        <span v-for="i in 5" :key="i" class="star">
          <i
            class="bi"
            :class="
              i <= Math.floor(product.avgRating || 0)
                ? 'bi-star-fill text-warning'
                : 'bi-star text-warning'
            "
          ></i>
        </span>
      </div>
      <a href="#reviews" class="reviews-link" @click.prevent="scrollToReviews">
        {{ product.reviewCount || 0 }} 則評價
      </a>
    </div>

    <!-- 即時庫存顯示 -->
    <div class="stock-status mb-3">
      <span v-if="selectedSpec?.stockQty > 20" class="badge bg-success">
        庫存 {{ selectedSpec.stockQty }} 件
      </span>
      <span v-else-if="selectedSpec?.stockQty > 0" class="badge bg-warning text-dark">
        庫存緊張：僅剩 {{ selectedSpec.stockQty }} 件
      </span>
      <span v-else class="badge bg-danger">暫無庫存</span>
    </div>

    <!-- 促銷訊息 : 暫時不放，要跟MKT串 -->
    <!-- div class="promo-message mb-3 p-3 bg-light border-start border-warning border-4">
      <i class="bi bi-gift-fill text-warning me-2"></i>
      <strong>「定期自動送貨優惠」特價品</strong>
      <p class="mb-0 mt-1 small">首次訂購可享 30% 優惠至多批！後續訂購享 15% 折扣！</p>
    </div-->

    <!-- 規格選擇 -->
    <div class="spec-section mb-4">
      <label class="form-label fw-bold">
        規格: {{ selectedSpec?.optionName}}
      </label>

      <div class="spec-options">
        <button
          v-for="spec in product.skus"
          v-show="spec.isActive"
          :key="spec.skuId"
          class="spec-button"
          :class="{
            active: selectedSpec?.skuId === spec.skuId,
            disabled: !spec.hasStock,
          }"
          :disabled="!spec.hasStock"
          @click="spec.hasStock && selectSpec(spec)"
        >

          <!-- 規格名稱：黃底 -->
          <div v-if="spec.optionName" class="spec-name-box">
            <div class="spec-name">{{ spec.optionName }}</div>
          </div>

          <div class="spec-price">
            <!-- 有優惠價 -->
            <template v-if="spec.salePrice && spec.salePrice > 0 && spec.salePrice < spec.unitPrice">
              <div class="price-old text-muted text-decoration-line-through small">
                NT${{ spec.unitPrice }}
              </div>
              <div class="price-sale text-danger fw-bold">
                NT${{ spec.salePrice }}
              </div>
            </template>

            <!-- 沒有優惠價 -->
            <template v-else>
              <div class="price-normal text-dark fw-semibold">
                NT${{ spec.unitPrice || spec.listPrice }}
              </div>
            </template>
          </div>
        </button>
      </div>
    </div>

    <!-- 商品基本資訊 -->
    <div class="product-meta mb-4">
      <ul class="list-unstyled small">
        <!--li><strong>包裝規格：</strong>{{ selectedSpec?.optionName || product.PackageType }}</li-->
        <!--li><strong>效期：</strong>{{ formatDate(product.expiryDate) }}</li-->
        <li v-if="product.dimensions">
          <strong>約尺寸：</strong>{{ product.weight / 10 }}公克，{{ product.dimensions }}
        </li>
        <li><strong>商品編號：</strong>{{ product.productId }}</li>
        <li><strong>產品代碼：</strong>{{ product.productCode }}</li>
        <li><strong>UPC 代碼：</strong>{{ product.upcCode }}</li>
      </ul>
    </div>

    <!-- 包裝描述 : 暫時不放，要跟物流串 -->
    <!-- div class="package-info mb-4 p-3 bg-light rounded">
      <p class="mb-0 small">
        包裝使用可全面回收的瓶罐，無 BPA 成分，無 PVC 塑膠材質，按此
        <a href="#" class="text-primary">比較</a>
      </p>
    </div-->

    <!-- 警語說明 : 暫時不放，要另外建Table -->
    <!-- div class="warning-info mb-4">
      <p class="small mb-1">
        <i class="bi bi-shield-check text-success me-1"></i>
        無添加製造程序所需之外的成分
      </p>
      <p class="small mb-0">
        嗜睡藥物或酒精飲料與本產品一起服用時，會增強嗜睡的效果。
        <a href="#" class="text-primary">比較</a>
      </p>
    </div-->
  </div>
</template>

<script setup>
import { onMounted, computed } from 'vue'
import * as bootstrap from 'bootstrap'

const props = defineProps({
  product: Object,
  selectedSpec: Object
})
const emit = defineEmits(['spec-selected'])

/**
 * 選擇規格
 */
const selectSpec = (spec) => {
  emit('spec-selected', spec)
}

// 頁面載入時，若有 mainSkuId，自動選定對應規格
onMounted(() => {
  if (props.product?.mainSkuId && props.product?.skus?.length) {
    const mainSpec = props.product.skus.find(
      (s) => s.skuId === props.product.mainSkuId
    )
    if (mainSpec) {
      selectSpec(mainSpec)
    }
  }
})

function scrollToReviews() {
  // 切換到顧客評價分頁
  const reviewsTab = document.querySelector('[data-bs-target="#reviews"]')
  if (!reviewsTab) return console.warn('找不到 reviews 分頁按鈕')

  const tab = new bootstrap.Tab(reviewsTab)
  tab.show()

  // 延遲滾動，確保 Tab 動畫已完成
  setTimeout(() => {
    const section = document.querySelector('#reviews')
    if (section) {
      const headerOffset = 80 // 若有固定導覽列，可調整高度
      const elementPosition = section.getBoundingClientRect().top + window.scrollY
      const offsetPosition = elementPosition - headerOffset
      window.scrollTo({ top: offsetPosition, behavior: 'smooth' })
    }
  }, 200)
}

/**
 * 品牌優惠顯示設定（過期自動隱藏）
 */
const brandPromotion = computed(() => {
  const p = props.product
  if (!p) return null

  // 無折扣率或 >= 1（無優惠）
  if (!p.brandDiscountRate || p.brandDiscountRate >= 1) return null

  // 沒有日期也不顯示
  if (!p.brandDiscountStart || !p.brandDiscountEnd) return null

  const now = new Date()
  const start = new Date(p.brandDiscountStart)
  const end = new Date(p.brandDiscountEnd)

  // ✅ 若未開始或已結束 → 不顯示
  if (now < start || now > end) return null

  // ✅ 活動進行中 → 顯示跑馬燈
  const rate = (p.brandDiscountRate * 10).toFixed(1).replace(/\.0$/, '') // 0.9 → 9
  const startText = formatDate(p.brandDiscountStart)
  const endText = formatDate(p.brandDiscountEnd)

  const daysLeft = Math.ceil((end - now) / (1000 * 60 * 60 * 24))

  return {
    title: `${p.brandName} 全館 ${rate} 折優惠 🔥`,
    desc: `活動期間：${startText} ～ ${endText}`,
    daysLeft
  }
})

/**
 * 格式化日期
 */
const formatDate = (dateString) => {
  if (!dateString) return 'N/A'
  const date = new Date(dateString)
  return `${date.getFullYear()}年${date.getMonth() + 1}月${date.getDate()}日`
}
</script>

<style scoped>
.product-info {
  background: #fff;
  padding: 30px;
  border-radius: 8px;
}

/* 品牌標籤 */
.brand-badge .badge {
  font-size: 0.75rem;
  padding: 0.35rem 0.65rem;
}

/* 商品標題 */
.product-title {
  font-size: 1.5rem;
  font-weight: 600;
  color: #333;
  line-height: 1.4;
  margin-bottom: 0.5rem;
}

.brand-name {
  font-size: 0.9rem;
}

/* 評分區域 */
.rating-section {
  display: flex;
  align-items: center;
  gap: 10px;
}

.stars {
  display: flex;
  gap: 2px;
}

.star i {
  font-size: 1rem;
}

.rating-value {
  font-weight: 600;
  color: #333;
}

.reviews-link {
  color: #0066c0;
  text-decoration: none;
}

.reviews-link:hover {
  text-decoration: underline;
}

/* 規格選擇 */
.spec-options {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.spec-button {
  flex: 1;
  min-width: 120px;
  border: 2px solid #d0d0d0;
  border-radius: 8px;
  background: #fff;
  cursor: pointer;
  transition: all 0.3s ease;
  text-align: center;
  padding: 0; /* 拿掉多餘間距，讓子區塊自己控制 */
  overflow: hidden;
}

.spec-button:hover:not(.disabled) {
  border-color: #f68b1e;
  box-shadow: 0 2px 8px rgba(246, 139, 30, 0.2);
}

.spec-button.active {
  border-color: #f68b1e;
  background-color: #fff7f0;
  box-shadow: 0 2px 8px rgba(246, 139, 30, 0.3);
}

.spec-button.disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.spec-name {
  font-size: 0.9rem;
  font-weight: 600;
  color: #333;
}

/* 規格名稱區：黃底 */
.spec-name-box {
  background-color: #fff8e1; /* 🔹淡黃色底 */
  padding: 8px 0;
}

.spec-price {
  font-size: 1.1rem;
  color: #d32f2f;
  font-weight: 700;
}

.price-old {
  font-size: 0.7rem;
  color: #999;
  text-decoration: line-through;
  display: block;
}

.spec-price-box {
  background-color: #f8f9fa; /* 🔹淡灰底 */
  padding: 6px 0;
  border-top: 1px solid #e0e0e0;
}

.price-sale {
  font-size: 1rem;
  font-weight: 700;
  color: #d32f2f;
}

.price-normal {
  font-size: 1rem;
  font-weight: 600;
  color: #333;
}

/* Hover 與選中狀態 */
.spec-button:hover {
  border-color: #f5c542;
  box-shadow: 0 2px 8px rgba(245, 197, 66, 0.25);
}

.spec-button.active {
  border-color: #f5c542;
  box-shadow: 0 0 0 3px rgba(245, 197, 66, 0.3);
}

/* ✅ 品牌跑馬燈樣式 */
.brand-promo-marquee {
  background: #e9f8ec;
  border-left: 5px solid #28a745;
  height: 30px;
  overflow: hidden;
  position: relative;
}

.promo-track {
  display: flex;
  width: max-content;
  animation: marquee 7s linear infinite;
  will-change: transform;
}

.promo-content {
  display: flex;
  align-items: center;
  white-space: nowrap;
  padding-right: 4rem; /* 兩段之間的間距 */
  color: #155724;
  font-size: 0.95rem;
  font-weight: 500;
}

/* 無縫滾動動畫：移動半段寬度 */
@keyframes marquee {
  0% {
    transform: translateX(0);
  }
  100% {
    transform: translateX(-50%);
  }
}

/* 讓描述文字有對比感 */
.promo-desc {
  color: #0d6e27;
}

/* hover 暫停滾動 */
.brand-promo-marquee:hover .promo-track {
  animation-play-state: paused;
}

/* 響應式設計 */
@media (max-width: 768px) {
  .spec-options {
    display: grid;
    grid-template-columns: repeat(2, 1fr); /* 每行兩個 */
    gap: 8px;
  }

  .spec-button {
    min-width: auto; /* 移除固定寬度 */
    width: 100%;
    padding: 0;
  }

  .spec-name-box {
    padding: 6px 0;
  }

  .spec-price,
  .spec-price-box {
    padding: 4px 0;
  }

  .price-sale {
    font-size: 0.9rem;
  }

  .price-old {
    font-size: 0.65rem;
  }

    /* 無庫存按鈕樣式 */
  .spec-button.disabled,
  .spec-button:disabled {
    background-color: #f5f5f5;
    border-color: #ddd;
    color: #aaa;
    cursor: not-allowed;
    opacity: 0.6;
    box-shadow: none;
    pointer-events: none;
  }

  /* 無庫存時禁止 hover 效果 */
  .spec-button.disabled:hover {
    border-color: #ddd;
    box-shadow: none;
  }
}
</style>
