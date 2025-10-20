<!--
  ProductTabs.vue - 商品詳細資訊 Tabs
  功能：商品描述、成分、屬性、問答、評價
-->
<template>
  <div class="product-tabs">
    <!-- Tabs 導航 -->
    <ul class="nav nav-tabs" role="tablist">
      <li class="nav-item" role="presentation">
        <button
          class="nav-link active"
          data-bs-toggle="tab"
          data-bs-target="#description"
          type="button"
          role="tab"
        >
          商品描述
        </button>
      </li>
      <li class="nav-item" role="presentation">
        <button
          class="nav-link"
          data-bs-toggle="tab"
          data-bs-target="#ingredients"
          type="button"
          role="tab"
        >
          成分資訊
        </button>
      </li>
      <li class="nav-item" role="presentation">
        <button
          class="nav-link"
          data-bs-toggle="tab"
          data-bs-target="#attributes"
          type="button"
          role="tab"
        >
          商品屬性
        </button>
      </li>
      <li class="nav-item" role="presentation">
        <button
          class="nav-link"
          data-bs-toggle="tab"
          data-bs-target="#reviews"
          type="button"
          role="tab"
        >
          顧客評價 ({{ product.reviewCount || 0 }})
        </button>
      </li>
      <li class="nav-item" role="presentation">
        <button class="nav-link" data-bs-toggle="tab" data-bs-target="#qa" type="button" role="tab">
          問與答
        </button>
      </li>
    </ul>

    <!-- Tabs 內容 -->
    <div class="tab-content">
      <!-- 商品描述 -->
      <div class="tab-pane fade show active" id="description" role="tabpanel">
        <div class="p-4">
          <h4>商品說明</h4>
          <p class="mb-3">{{ product.shortDesc }}</p>
          <div v-html="product.fullDesc"></div>
        </div>
      </div>

      <!-- 成分資訊 -->
      <div class="tab-pane fade" id="ingredients" role="tabpanel">
        <div class="p-4">
          <h4>成分資訊</h4>
          <table
            class="table table-bordered"
            v-if="product.ingredients && product.ingredients.length > 0"
          >
            <thead>
              <tr>
                <th>成分名稱</th>
                <th>含量 (mg)</th>
                <th>備註</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="ingredient in product.ingredients" :key="ingredient.ingredientName">
                <td>{{ ingredient.ingredientName }}</td>
                <td>{{ ingredient.percentage }}</td>
                <td>{{ ingredient.note || '-' }}</td>
              </tr>
            </tbody>
          </table>
          <p v-else class="text-muted">暫無成分資訊</p>
        </div>
      </div>

      <!-- 商品屬性 -->
      <div class="tab-pane fade" id="attributes" role="tabpanel">
        <div class="p-4">
          <h4>商品屬性</h4>
          <table
            class="table table-bordered"
            v-if="product.attributes && product.attributes.length > 0"
          >
            <thead>
              <tr>
                <th>屬性</th>
                <th>內容</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="attr in product.attributes" :key="attr.attributeName">
                <td>{{ attr.attributeName }}</td>
                <td>{{ attr.optionName }}</td>
              </tr>
            </tbody>
          </table>
          <p v-else class="text-muted">暫無屬性資訊</p>
        </div>
      </div>

      <!-- 顧客評價 -->
      <div class="tab-pane fade" id="reviews" role="tabpanel">
        <div class="p-4">
          <ProductReviews
            :product-id="product.productId"
            :avg-rating="product.avgRating || 0"
            :review-count="product.reviewCount || 0"
          />
        </div>
      </div>

      <!-- 問與答 -->
      <div class="tab-pane fade" id="qa" role="tabpanel">
        <div class="p-4">
          <ProductQA :product-id="product.productId" />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import ProductQA from './ProductQA.vue'
import ProductReviews from './ProductReviews.vue'

defineProps({
  product: {
    type: Object,
    required: true,
  },
})
</script>

<style scoped>
.product-tabs {
  background: #fff;
  border-radius: 8px;
  overflow: hidden;
}

.nav-tabs {
  border-bottom: 2px solid #e0e0e0;
  padding: 0 20px;
}

.nav-tabs .nav-link {
  border: none;
  color: #666;
  font-weight: 500;
  padding: 1rem 1.5rem;
  margin-bottom: -2px;
}

.nav-tabs .nav-link:hover {
  color: #f68b1e;
  border-color: transparent;
}

.nav-tabs .nav-link.active {
  color: #f68b1e;
  border-bottom: 3px solid #f68b1e;
  background: transparent;
}

.tab-content {
  min-height: 300px;
}

/* 評價摘要 */
.review-summary .rating-score {
  text-align: center;
}

.review-summary .score {
  font-size: 3rem;
  font-weight: 700;
  color: #333;
}

.review-summary .stars {
  font-size: 1.2rem;
  margin: 10px 0;
}

.review-summary .count {
  color: #666;
  font-size: 0.9rem;
}

/* 表格樣式 */
.table {
  margin-top: 1rem;
}

.table th {
  background-color: #f5f5f5;
  font-weight: 600;
}
</style>
