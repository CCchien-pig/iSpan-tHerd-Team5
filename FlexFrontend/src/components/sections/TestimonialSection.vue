<!--
  TestimonialSection.vue - 客戶評價展示組件
  功能：展示客戶評價和反饋，建立社會證明和信任度
  特色：星級評分、客戶頭像、卡片式布局
  用途：用於首頁、產品頁面等需要展示用戶反饋的區域
-->
<template>
  <!-- 客戶評價區塊容器 -->
  <section class="testimonials-section py-5 bg-light">
    <div class="container">
      <!-- 區塊標題 -->
      <h2 class="text-center mb-5">{{ title }}</h2>
      <!-- 評價卡片網格 -->
      <div class="row g-4">
        <!-- 遍歷評價數據，生成評價卡片 -->
        <div
          v-for="testimonial in testimonials"
          :key="testimonial.id"
          class="col-lg-4"
        >
          <!-- 評價卡片 -->
          <div class="testimonial-card h-100">
            <!-- 評價內容區域 -->
            <div class="testimonial-content">
              <!-- 星級評分 -->
              <div class="stars mb-3">
                <i
                  class="bi bi-star-fill text-warning"
                  v-for="n in 5"
                  :key="n"
                ></i>
              </div>
              <!-- 評價文字 -->
              <p class="testimonial-text">"{{ testimonial.text }}"</p>
            </div>
            <!-- 評價作者信息 -->
            <div class="testimonial-author">
              <!-- 客戶頭像 -->
              <img
                :src="testimonial.avatar"
                :alt="testimonial.name"
                class="author-avatar"
              />
              <!-- 客戶信息 -->
              <div class="author-info">
                <h6 class="author-name">{{ testimonial.name }}</h6>
                <p class="author-title">{{ testimonial.title }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script>
/**
 * TestimonialSection.vue 組件配置
 * 功能：可重用的客戶評價展示組件
 * 特色：支持動態評價數據、星級評分、客戶信息展示
 */
export default {
  name: 'TestimonialSection', // 組件名稱

  /**
   * Props定義 - 組件的可配置屬性
   */
  props: {
    // 區塊標題
    title: {
      type: String,
      default: '客戶評價',
    },
    // 評價數據數組
    testimonials: {
      type: Array,
      required: true, // 必須提供評價數據
      default: () => [], // 默認為空數組
    },
  },
};
</script>

<style scoped>
.testimonial-card {
  background: white;
  border-radius: 1rem;
  padding: 2rem;
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.08);
}

.testimonial-text {
  font-style: italic;
  color: #666;
  margin-bottom: 1.5rem;
}

.testimonial-author {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.author-avatar {
  width: 50px;
  height: 50px;
  border-radius: 50%;
}

.author-name {
  margin-bottom: 0;
  font-weight: 600;
}

.author-title {
  margin-bottom: 0;
  color: #6c757d;
  font-size: 0.9rem;
}
</style>
