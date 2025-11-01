<template>
  <div class="not-found d-flex flex-column justify-content-center align-items-center vh-100 text-center bg-light">
    <!-- 圖示區 -->
    <div class="icon-wrapper mb-4">
      <i class="bi bi-exclamation-triangle main-color-green-text display-1"></i>
    </div>

    <!-- 文字內容 -->
    <h1 class="fw-bold text-dark mb-3">404 - 找不到頁面</h1>
    <p class="text-muted mb-4">
      抱歉，您輸入的網址不存在或已被移除。<br />
      您可以返回首頁或檢查網址是否正確。
    </p>

    <!-- 按鈕 -->
    <router-link to="/" class="btn btn-md dropdown-toggle main-color-green">
      返回首頁
    </router-link>
  </div>
</template>

<style scoped>
.not-found {
  background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
}

.icon-wrapper {
  animation: float 3s ease-in-out infinite;
}

@keyframes float {
  0%, 100% {
    transform: translateY(0);
  }
  50% {
    transform: translateY(-8px);
  }
}

.btn-primary {
  background: linear-gradient(135deg, #6a11cb 0%, #2575fc 100%);
  border: none;
  transition: all 0.3s ease;
}

.btn-primary:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 10px rgba(37, 117, 252, 0.3);
}
</style>
