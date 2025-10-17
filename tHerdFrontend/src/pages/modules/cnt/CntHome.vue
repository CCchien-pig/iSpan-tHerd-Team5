<template>
  <div>

    <!-- 🟦 Hero 區：平台品牌定位 -->
    <section class="main-color-green text-white text-center py-5">
      <div class="container">
        <h1 class="display-5 fw-bold">健康 × 資訊分析平台</h1>
        <p class="lead mb-4">
          探索健康知識，打造科學飲食生活
        </p>

        <!-- 專題介紹（高階敘述，適合期末報告） -->
        <p class="mx-auto" style="max-width: 600px;">
          本平台整合健康文章與營養資料，
          以視覺化圖表與知識內容，
          幫助使用者理解食材營養，建立更智慧的飲食選擇。
        </p>

        <div class="mt-4">
          <router-link to="/cnt/articles" class="btn teal-reflect-button me-3">
            開始閱讀文章
          </router-link>
          <router-link to="/cnt/nutrition" class="btn silver-reflect-button">
            進入營養分析
          </router-link>
        </div>
      </div>
    </section>

    <!-- 🧱 功能入口卡片區 -->
    <section class="container py-5">
      <div class="row g-4">

        <!-- 📰 健康文章 -->
        <div class="col-md-6">
          <div class="card h-100 shadow-sm">
            <div class="card-body">
              <h3 class="card-title main-color-green-text">健康文章</h3>
              <p class="card-text text-muted">
                收錄健康知識、飲食建議與生活指南。
              </p>
              <router-link to="/cnt/articles" class="btn btn-outline-primary">
                查看文章
              </router-link>
            </div>
          </div>
        </div>

        <!-- 🥗 營養資料庫 -->
        <div class="col-md-6">
          <div class="card h-100 shadow-sm">
            <div class="card-body">
              <h3 class="card-title main-color-green-text">營養分析</h3>
              <p class="card-text text-muted">
                探索不同食材營養成分，支援多項比較與圖表分析。
              </p>
              <router-link to="/cnt/nutrition" class="btn btn-outline-primary">
                進入分析
              </router-link>
            </div>
          </div>
        </div>

      </div>
    </section>
    <router-view></router-view>
  </div>
</template>

<script>
export default {
  name: 'CntHome'
}
</script>

<style scoped>
/* 可加入針對 Hero 的漸層或背景圖 */
/* 例如: background: linear-gradient(...); */
</style>
