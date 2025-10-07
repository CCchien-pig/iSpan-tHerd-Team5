<template>
  <div class="product-card h-100 d-flex flex-column">
  <!-- 商品圖片 -->
  <div class="product-image position-relative">
    <img :src="product.image" :alt="product.name" class="img-fluid" />

    <!-- 加入購物車按鈕 -->
    <button 
      class="btn btn-warning add-to-cart-btn fw-bold"
      @click="$emit('add-to-cart', product)"
    >
      加入購物車
    </button>
  </div>

    <!-- 商品資訊 -->
    <div class="product-info flex-grow-1 d-flex flex-column justify-content-between p-2">
      <!-- 商品名稱 -->
      <p class="product-name mb-1">{{ product.name }}</p>

      <!-- 評分 + 評價數 -->
      <div class="rating d-flex align-items-center mb-1">
        <span v-for="i in 5" :key="i" class="star">
          <i class="bi" 
             :class="i <= Math.round(product.rating) ? 'bi-star-fill text-warning' : 'bi-star text-warning'">
          </i>
        </span>
        <span class="reviews text-primary ms-1">{{ product.reviews }}</span>
      </div>

      <!-- 價格 -->
      <div class="price">
        <span class="current-price">NT${{ product.price }}</span>
        <span v-if="product.originalPrice" class="original-price">NT${{ product.originalPrice }}</span>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: "ProductCard",
  props: {
    product: {
      type: Object,
      required: true,
      validator: p => p.id && p.name && p.image && p.price
    }
  }
};
</script>

<style scoped>
.product-card {
  background: #fff;
  border: 1px solid #eee;
  border-radius: 0.5rem;
  overflow: hidden;
  transition: box-shadow 0.3s ease, transform 0.2s;
  cursor: pointer;
}

.product-card:hover {
  box-shadow: 0 5px 15px rgba(0,0,0,0.15);
  transform: translateY(-3px);
}

.product-image {
  position: relative;
  height: 180px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #fff;
}

.product-image img {
  max-height: 100%;
  max-width: 100%;
  object-fit: contain;
}

.add-to-cart-btn {
  position: absolute;
  bottom: 10px;         /* 固定在底部 */
  left: 20%;           /* 距離左邊 10px，不再用 translate */
  right: 20%;          /* 距離右邊 10px，讓它自動置中 */
  background-color: #f68b1e;
  color: white;
  border: none;
  padding: 0.4rem 0;    /* 自動撐滿左右 */
  font-size: 0.85rem;
  border-radius: 4px;
  text-align: center;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.3s ease;
  width: 60%;
}

.product-card:hover .add-to-cart-btn {
  opacity: 1;
  pointer-events: auto;
}

.product-info {
  padding: 0.5rem 0.75rem;
  text-align: center;
}

.product-name {
  font-size: 0.85rem;
  line-height: 1.2rem;
  color: #333;
  min-height: 2.4rem; /* 兩行固定高度 */
  overflow: hidden;
}

.rating {
  display: flex;
  align-items: center;
  justify-content: center; /* 讓整塊置中 */
  line-height: 1; /* 避免 baseline 不一樣 */
}

.rating .star i {
  font-size: 0.9rem;
  vertical-align: middle; /* 強制與數字同一高度 */
}

.reviews {
  font-size: 0.8rem;
  margin-left: 4px; /* 取代 ms-1 更精準 */
  vertical-align: middle;
}

.rating .star {
  font-size: 0.9rem;
}
.price {
  margin-top: 0.2rem;
}

.current-price {
  font-size: 1rem;
  font-weight: bold;
  color: #d32f2f;
  margin-right: 0.4rem;
}

.original-price {
  font-size: 0.85rem;
  color: #999;
  text-decoration: line-through;
}
</style>
