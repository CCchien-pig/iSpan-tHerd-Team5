<template>
  <div class="success-wrap d-flex flex-column align-items-center justify-content-center py-5">
    <div class="card text-center shadow-sm p-4 border-0">
      <div class="circle-check mb-3">
        <i class="bi bi-check-circle-fill"></i>
      </div>
      <h3 class="main-color-green-text mb-3">工單已成功送出！</h3>
      <p class="text-muted mb-4">
        我們已收到您的問題，客服人員將盡快與您聯繫。<br>
        您可以在「我的工單」中追蹤處理進度。
      </p>
      <div class="d-flex gap-3 justify-content-center">
        <router-link to="/cs/ticket" class="btn btn-outline-primary px-4">
          再送一張
        </router-link>
        <router-link to="/cs/tickets" class="btn btn-primary px-4">
          查看我的工單
        </router-link>
      </div>
    </div>
  </div>
</template>

<style scoped>
.success-wrap {
  background: rgb(248, 249, 250);
  min-height: 70vh;
}

.card {
  border-radius: 1rem;
  max-width: 480px;
}

.circle-check i {
  font-size: 4rem;
  color: rgb(0, 112, 131);
  animation: pop 0.4s ease-out;
}

@keyframes pop {
  0% {
    transform: scale(0.5);
    opacity: 0;
  }
  100% {
    transform: scale(1);
    opacity: 1;
  }
}
</style>
