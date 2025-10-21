<template>
  <div class="container py-5">
    <!-- 🔖 頁面標題 -->
    <h2 class="mb-4 main-color-green-text">健康文章</h2>

    <!-- ⌛ Loading 狀態 -->
    <div v-if="loading" class="text-center py-5">
      <p>載入中...</p>
    </div>

    <!-- 🧾 無資料 -->
    <div v-else-if="articles.length === 0" class="text-center text-muted py-5">
      尚無文章內容
    </div>

    <!-- 📰 文章卡片列表 -->
    <div class="row g-4" v-else>
      <div
        class="col-md-4"
        v-for="article in articles"
        :key="article.pageId"
      >
        <div class="article-card shadow-sm h-100">
          <!-- 圖片 -->
          <div class="article-image-wrapper">
            <img
              :src="article.coverImage"
              class="article-image"
              alt="文章封面"
            />
          </div>

          <!-- 內容 -->
          <div class="p-3">
            <h5 class="fw-bold article-title">{{ article.title }}</h5>
            <p class="text-muted small mb-1">
              {{ new Date(article.publishedDate).toLocaleDateString() }}
            </p>
            <p class="text-secondary article-excerpt">
              {{ article.excerpt }}
            </p>
            <router-link
              :to="`/cnt/article/${article.pageId}`"
              class="btn btn-outline-primary btn-sm mt-auto"
            >
              閱讀更多 →
            </router-link>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { getArticleList } from './api/cntService'

export default {
  name: 'ArticleList',
  data() {
    return {
      articles: [],
      loading: true,
    }
  },
  async mounted() {
    try {
      const res = await getArticleList(1, 12)
      // console.log('API 回應資料：', res)
      this.articles = res.items
    } catch (err) {
      console.error('取得文章列表失敗：', err)
    } finally {
      this.loading = false
    }
  }
}
</script>

<style scoped>
.article-card {
  border-radius: 8px;
  overflow: hidden;
  background: #fff;
  display: flex;
  flex-direction: column;
}

.article-image-wrapper {
  width: 100%;
  height: 180px;
  overflow: hidden;
}

.article-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform .3s;
}

.article-card:hover .article-image {
  transform: scale(1.05);
}

.article-title {
  color: #2c3e50;
  font-size: 1.1rem;
}

.article-excerpt {
  font-size: 0.9rem;
  height: 40px;
  overflow: hidden;
}
</style>
