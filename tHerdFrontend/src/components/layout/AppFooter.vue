<template>
  <!-- 主Footer容器 -->
  <footer class="main-footer main-color-darkwhite text-light py-5">
    <div class="container">
      <div class="row">
        <!-- 關於我們區塊 -->
        <div class="col-lg-3 col-md-6 mb-4">
          <h5 class="mb-3 main-color-white-text">關於 tHerd</h5>
          <ul class="list-unstyled">
            <li>
              <RouterLink to="/cs/ticket" class="text-light text-decoration-none"
                >聯絡我們</RouterLink
              >
            </li>
            <li>
              <RouterLink to="/cs/faq" class="text-light text-decoration-none">常見問題</RouterLink>
            </li>

            <!-- <li><a href="#" class="text-light text-decoration-none">職業機會</a></li>
            <li><a href="#" class="text-light text-decoration-none">新聞中心</a></li>
            <li><a href="#" class="text-light text-decoration-none">投資者關係</a></li> -->
          </ul>
        </div>

        <!-- 客戶服務 -->
        <div class="col-lg-3 col-md-6 mb-4">
          <h5 class="mb-3 main-color-white-text">客戶服務</h5>
          <ul class="list-unstyled">
            <li>
              <RouterLink to="/cs/faq" class="text-light text-decoration-none">聯絡我們</RouterLink>
            </li>
            <li>
              <router-link
                :to="{ path: '/sup/logistics-info', query: { tab: 'info' } }"
                class="text-light text-decoration-none"
                >配送資訊</router-link
              >
            </li>
            <li><a href="#" class="text-light text-decoration-none">退貨政策</a></li>
            <!-- <li><a href="#" class="text-light text-decoration-none">追蹤訂單</a></li> -->
          </ul>
        </div>

        <!-- 購物指南 -->
        <div class="col-lg-3 col-md-6 mb-4">
          <h5 class="mb-3 main-color-white-text">購物指南</h5>
          <ul class="list-unstyled">
            <!-- <li><a href="#" class="text-light text-decoration-none">如何訂購</a></li>
            <li><a href="#" class="text-light text-decoration-none">付款方式</a></li> -->
            <li>
              <router-link
                :to="{ path: '/sup/logistics-info', query: { tab: 'fee' } }"
                class="text-light text-decoration-none"
                >運費計算</router-link
              >
            </li>
            <li><a href="#" class="text-light text-decoration-none">會員制度</a></li>
            <!-- <li><a href="#" class="text-light text-decoration-none">優惠券使用</a></li> -->
          </ul>
        </div>

        <!-- 社群媒體 + 電子報訂閱 -->
        <div class="col-lg-3 col-md-6 mb-4">
          <h5 class="main-color-white-text mb-3">關注我們</h5>
          <div class="social-links mb-3">
            <a href="#" class="text-light me-3"><i class="bi bi-facebook fs-4"></i></a>
            <a href="#" class="text-light me-3"><i class="bi bi-instagram fs-4"></i></a>
            <a href="#" class="text-light me-3"><i class="bi bi-youtube fs-4"></i></a>
            <a href="#" class="text-light me-3"><i class="bi bi-twitter fs-4"></i></a>
          </div>

          <!-- ✅ 電子報訂閱 -->
          <div class="newsletter">
            <h6 class="main-color-white-text mb-2">訂閱活動電子報</h6>
            <div class="input-group" style="max-width: 300px">
              <input
                type="email"
                class="form-control"
                placeholder="輸入您的電子郵件"
                v-model="email"
              />
              <button class="btn silver-reflect-button" type="button" @click="subscribeNewsletter">
                訂閱
              </button>
            </div>
            <div v-if="message" class="small mt-2 main-color-white-text">
              {{ message }}
            </div>
          </div>
        </div>
      </div>

      <hr class="my-4" />

      <!-- 底部資訊 -->
      <div class="row align-items-center">
        <div class="col-md-6">
          <p class="mb-0 text-muted main-color-white-text">© 2025 tHerd, LLC. 版權所有。</p>
        </div>
        <div class="col-md-6 text-md-end">
          <div class="payment-methods">
            <span class="main-color-white-text me-3">付款方式:</span>
            <i class="bi bi-credit-card me-2 main-color-white-text"></i>
            <i class="bi bi-paypal me-2 main-color-white-text"></i>
            <i class="bi bi-apple me-2 main-color-white-text"></i>
            <i class="bi bi-google-pay me-2 main-color-white-text"></i>
          </div>
        </div>
      </div>

      <!-- 法律條款 -->
      <div class="row mt-3">
        <div class="col-12">
          <div class="legal-links text-center">
            <a href="#" class="text-muted text-decoration-none me-3">隱私政策</a>
            <a href="#" class="text-muted text-decoration-none me-3">服務條款</a>
            <a href="#" class="text-muted text-decoration-none me-3">Cookie政策</a>
            <a href="#" class="text-muted text-decoration-none">網站地圖</a>
          </div>
        </div>
      </div>
    </div>
  </footer>
</template>

<script>
import http from '@/api/http'

export default {
  name: 'AppFooter',

  data() {
    return {
      email: '',
      message: '',
      success: false,
    }
  },

  methods: {
    async subscribeNewsletter() {
      if (!this.email.trim()) {
        this.message = '請輸入有效的電子郵件地址'
        this.success = false
        return
      }

      try {
        const { data } = await http.post('/mkt/newsletter/subscribe', { email: this.email })
        this.message = data.message || '訂閱成功！請查收信箱'
        this.success = data.ok
        this.email = ''
      } catch (err) {
        console.error('❌ 訂閱電子報失敗', err)
        this.message = '訂閱失敗，請稍後再試'
        this.success = false
      }
    },
  },
}
</script>

<style scoped>
.main-footer {
  background: rgb(15, 120, 135);
}

.social-links a {
  transition: color 0.3s ease;
}
.social-links a:hover {
  color: rgb(118, 201, 212) !important;
}
.payment-methods i {
  font-size: 1.5rem;
}
.legal-links a:hover {
  color: rgb(77, 180, 193) !important;
}

@media (max-width: 768px) {
  .main-footer .col-lg-3 {
    margin-bottom: 2rem;
  }
  .newsletter .input-group {
    max-width: 100%;
  }
  .payment-methods {
    text-align: center;
    margin-top: 1rem;
  }
}
.text-muted {
  color: rgb(248, 249, 250) !important;
}
</style>
