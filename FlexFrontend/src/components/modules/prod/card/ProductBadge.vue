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
        熱銷: 'badge-hot', // 紅色背景
        新品: 'badge-new', // 綠色背景
        特價: 'badge-sale', // 黃色背景
        限量: 'badge-limited', // 紫色背景
      };
      return badgeMap[this.badge] || 'badge-default'; // 默認灰色背景
    },
  },
};
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

.badge-hot {
  background: #dc3545;
}

.badge-new {
  background: #28a745;
}

.badge-sale {
  background: #ffc107;
  color: #000;
}

.badge-limited {
  background: #6f42c1;
}

.badge-default {
  background: #6c757d;
}
</style>
