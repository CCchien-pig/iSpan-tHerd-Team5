<template>
  <div class="container py-4">
    <!-- 🔖 頁面標題 -->
    <h2 class="mb-4 main-color-green-text">健康文章</h2>

    <!-- 📰 文章列表 -->
    <div v-for="article in articles" :key="article.id" class="row mb-4 align-items-center article-item">
      <!-- 圖片區 -->
      <div class="col-md-4">
        <img
          :src="article.image"
          class="img-fluid rounded"
          alt="文章封面"
        />
      </div>

      <!-- 文字區 -->
      <div class="col-md-8">
        <h4 class="fw-bold">{{ article.title }}</h4>
        <p class="text-muted small mb-2">{{ article.date }}</p>
        <p class="text-secondary">{{ article.summary }}</p>
        <router-link
          :to="`/cnt/article/${article.slug}-${article.id}`"
          class="btn btn-outline-primary btn-sm"
        >
          閱讀更多 →
        </router-link>
      </div>
    </div>

    <!-- ⛔ 無資料狀態 -->
    <div v-if="articles.length === 0" class="text-center text-muted py-5">
      尚無文章內容
    </div>
  </div>
</template>

<script>
export default {
  name: 'ArticleList',
  data() {
    return {
      // 🧪 假資料 - 未來將替換為 API 取得
      articles: [
        {
          id: 1,
          title: '均衡飲食的重要性',
          summary: '了解每日飲食中各項營養素的攝取比例，建立健康生活基礎。',
          date: '2025-01-10',
          slug: 'balanced-diet',
          image: 'https://via.placeholder.com/350x200'
        },
        {
          id: 2,
          title: '維生素與免疫力的關係',
          summary: '探討維生素C、D與B群如何增強人體免疫系統。',
          date: '2025-01-05',
          slug: 'vitamins-and-immunity',
          image: 'https://via.placeholder.com/350x200'
        },
        {
          id: 3,
          title: '提升專注力的食物推薦',
          summary: 'Omega-3脂肪酸與高纖維飲食如何幫助腦部專注與記憶力。',
          date: '2024-12-28',
          slug: 'focus-foods',
          image: 'https://via.placeholder.com/350x200'
        }
      ]
    }
  }
}
</script>

<style scoped>
.article-item img {
  object-fit: cover;
  width: 100%;
  height: 200px;
}
</style>
