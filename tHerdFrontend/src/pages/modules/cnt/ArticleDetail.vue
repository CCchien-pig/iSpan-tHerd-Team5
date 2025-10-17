<template>
  <div class="container py-4">

    <!-- 🔙 返回文章列表 -->
    <router-link to="/cnt/articles" class="btn btn-outline-secondary mb-3">
      ← 返回文章列表
    </router-link>

    <!-- 🏷 標題 -->
    <h1 class="main-color-green-text fw-bold">{{ article.title }}</h1>

    <!-- 🗓 日期 -->
    <p class="text-muted mb-4">{{ article.date }}</p>

    <!-- 📚 文章內容區（支援段落顯示） -->
    <div v-for="(paragraph, index) in article.content" :key="index" class="mb-3">
      <p>{{ paragraph }}</p>
    </div>

    <!-- 📝 未來可支援圖片 / 引用 / Block -->
    <!-- <img :src="article.image" class="img-fluid mb-3" v-if="article.image"> -->

  </div>
</template>

<script>
export default {
  name: 'ArticleDetail',
  props: ['id', 'slug'], // 🔌 從路由 /:slug-:id 接參數
  data() {
    return {
      // 🧪 假資料 （未來改為 API 取得）
      article: {
        title: '均衡飲食與生活習慣的影響',
        date: '2025-01-10',
        content: [
          '均衡的飲食對於維持身體健康至關重要。攝取足夠的蛋白質、脂肪、碳水化合物以及維生素與礦物質，能幫助身體維持正常機能。',
          '除了飲食，充足的睡眠與規律的運動同樣影響身體狀態。根據研究，每週至少150分鐘的中度運動有助於心血管健康。',
          '未來我們將帶您深入探討不同食材的營養成分，幫助您從飲食中做出更明智的選擇。'
        ]
      }
    }
  },
  mounted() {
    // 🛰 未來支援 API → 使用 this.id 或 this.slug 取得文章
    console.log('文章 ID:', this.id);
    console.log('文章 Slug:', this.slug);
  }
}
</script>

<style scoped>
/* 可加入未來文章排版樣式 */
</style>
