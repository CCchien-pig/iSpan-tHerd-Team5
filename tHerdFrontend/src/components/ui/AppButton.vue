<!--
  AppButton.vue - 可重用按鈕組件
  功能：提供統一的按鈕樣式，支持多種變體和自定義配置
  特色：可配置顏色、邊框、大小、hover效果
  用途：用於全站統一的按鈕樣式
-->
<template>
  <!-- 按鈕容器 -->
  <button
    :class="buttonClasses"
    :type="type"
    :disabled="disabled"
    @click="handleClick"
  >
    <!-- 左側圖標 -->
    <i v-if="leftIcon" :class="leftIcon" class="me-2"></i>

    <!-- 插槽內容 -->
    <slot></slot>

    <!-- 右側圖標 -->
    <i v-if="rightIcon" :class="rightIcon" class="ms-2"></i>
  </button>
</template>

<script>
/**
 * AppButton.vue 組件配置
 * 功能：可重用的按鈕組件
 * 特色：支持多種變體、自定義樣式、hover效果
 */
export default {
  name: 'AppButton', // 組件名稱

  /**
   * Props定義 - 組件的可配置屬性
   */
  props: {
    // 按鈕類型
    type: {
      type: String,
      default: 'button',
      validator: value => ['button', 'submit', 'reset'].includes(value),
    },
    // 按鈕變體
    variant: {
      type: String,
      default: 'primary',
      validator: value =>
        [
          'primary',
          'secondary',
          'success',
          'danger',
          'warning',
          'info',
          'light',
          'dark',
          'outline-primary',
          'outline-secondary',
          'outline-success',
          'outline-danger',
          'outline-warning',
          'outline-info',
          'outline-light',
          'outline-dark',
        ].includes(value),
    },
    // 按鈕大小
    size: {
      type: String,
      default: 'md',
      validator: value => ['sm', 'md', 'lg'].includes(value),
    },
    // 是否禁用
    disabled: {
      type: Boolean,
      default: false,
    },
    // 左側圖標
    leftIcon: {
      type: String,
      default: '',
    },
    // 右側圖標
    rightIcon: {
      type: String,
      default: '',
    },
    // 是否顯示邊框
    showBorder: {
      type: Boolean,
      default: true,
    },
    // 自定義CSS類
    customClass: {
      type: String,
      default: '',
    },
  },

  /**
   * 計算屬性 - 動態生成按鈕樣式類
   */
  computed: {
    /**
     * 按鈕樣式類組合
     * 根據props動態生成Bootstrap類名
     */
    buttonClasses() {
      const classes = ['btn'];

      // 添加變體類
      if (this.variant.startsWith('outline-')) {
        classes.push(`btn-${this.variant}`);
      } else {
        classes.push(`btn-${this.variant}`);
      }

      // 添加大小類
      if (this.size !== 'md') {
        classes.push(`btn-${this.size}`);
      }

      // 添加邊框控制
      if (this.showBorder) {
        classes.push('border-1 border-secondary');
      }

      // 添加自定義類
      if (this.customClass) {
        classes.push(this.customClass);
      }

      return classes.join(' ');
    },
  },

  /**
   * 方法定義 - 處理按鈕點擊事件
   */
  methods: {
    /**
     * 處理按鈕點擊事件
     * 發送click事件給父組件
     */
    handleClick(event) {
      if (!this.disabled) {
        this.$emit('click', event);
      }
    },
  },
};
</script>

<style>
/* 樣式已移至全局 main.css */
</style>
