<!--
  ProductBadge.vue - 產品標籤組件
  功能：顯示產品標籤，如熱銷、新品、特價等
  特色：動態樣式、位置固定、多種標籤類型
  用途：用於產品卡片上顯示產品狀態標籤
-->
<template>
  <!-- 產品標籤容器 -->
  <div class="product-badge" :class="badgeClass">
    {{ badge }}
  </div>
</template>

<script>
/**
 * ProductBadge.vue 組件配置
 * 功能：可重用的產品標籤組件
 * 特色：支持多種標籤類型、動態樣式、位置固定
 */
export default {
  name: 'ProductBadge', // 組件名稱

  /**
   * Props定義 - 組件的可配置屬性
   */
  props: {
    // 標籤文字
    badge: {
      type: String,
      required: true, // 必須提供標籤文字
    },
  },

  /**
   * 計算屬性 - 根據標籤類型返回對應的CSS類
   */
  computed: {
    /**
     * 根據標籤文字返回對應的樣式類
     * 支持熱銷、新品、特價、限量等標籤類型
     */
    badgeClass() {
      const badgeMap = {
        '特價!': 'badge-danger',        // 🔴 紅色：促銷、特價
        '只限 tHerd': 'badge-success',  // 🟢 綠色：會員限定
        '新品搶先購': 'badge-primary',  // 🔵 藍色：新品
        '好物試用!': 'badge-pink',      // 💖 粉色：試用活動
      }
      return badgeMap[this.badge] || 'badge-secondary' // ⚪ 灰色：預設
    },
  },
}
</script>

<style scoped>
.product-badge {
  position: absolute;
  top: 10px;
  right: 10px;
  padding: 0.25rem 0.5rem;
  border-radius: 0.5rem;
  font-size: 0.75rem;
  font-weight: bold;
  color: white;
}

.badge-danger { background-color: #dc3545; color: white; }   /* 紅 */
.badge-success { background-color: #28a745; color: white; }  /* 綠 */
.badge-primary { background-color: #4981d4; color: white; }  /* 藍 */
.badge-pink { background-color: #f57b56; color: white; }     /* 粉 */
.badge-secondary { background-color: #6c757d; color: white; }/* 灰 */
</style>
