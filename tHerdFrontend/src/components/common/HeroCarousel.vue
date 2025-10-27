<template>
  <section class="hero-carousel">
    <div id="heroCarousel" class="carousel slide" data-bs-ride="carousel" data-bs-interval="4000">
      <!-- 🔸 輪播指示點 -->
      <div class="carousel-indicators">
        <button
          v-for="(item, index) in slides"
          :key="index"
          type="button"
          data-bs-target="#heroCarousel"
          :data-bs-slide-to="index"
          :class="{ active: index === 0 }"
        ></button>
      </div>

      <!-- 🔸 輪播內容 -->
      <div class="carousel-inner">
        <div
          v-for="(item, index) in slides"
          :key="item.id"
          :class="['carousel-item', { active: index === 0 }]"
        >
          <div class="row align-items-center">
            <!-- 左側文字區 -->
            <div class="col-lg-6 col-12 text-center text-lg-start px-5 py-4">
              <h1 class="display-4 fw-bold text-success">{{ item.title }}</h1>
              <p class="lead text-dark mb-4">{{ item.description }}</p>
              <button class="btn btn-success btn-lg px-4 py-2" @click="goToShop(item.link)">
                <i class="bi bi-cart3 me-2"></i>
                {{ item.buttonText }}
              </button>
            </div>
            <!-- 右側圖片區 -->
            <div class="col-lg-6 col-12">
              <img
                :src="item.image"
                :alt="item.title"
                class="d-block w-100 img-fluid rounded shadow"
              />
            </div>
          </div>
        </div>
      </div>

      <!-- 🔸 控制箭頭 -->
      <button class="carousel-control-prev" type="button" data-bs-target="#heroCarousel" data-bs-slide="prev">
        <span class="carousel-control-prev-icon"></span>
        <span class="visually-hidden">Previous</span>
      </button>
      <button class="carousel-control-next" type="button" data-bs-target="#heroCarousel" data-bs-slide="next">
        <span class="carousel-control-next-icon"></span>
        <span class="visually-hidden">Next</span>
      </button>
    </div>
  </section>
</template>

<script setup>
import { ref } from 'vue'

const slides = ref([
  {
    id: 1,
    title: '會員生日禮遇',
    description: '專屬壽星優惠，生日當月下單享特別折扣！',
    buttonText: '領取生日禮',
    image: '/images/Ad/Ad1000-Birthday.png',
    link: '/shop',
  },
  {
    id: 2,
    title: '新客專屬優惠',
    description: '第一次購物享限時折扣，立即成為 tHerd 會員！',
    buttonText: '立即註冊',
    image: '/images/Ad/Ad1001-NewCustomers.png',
    link: '/sport',
  },
  {
    id: 3,
    title: '新年歡慶活動',
    description: '迎新年，全館超殺優惠中，限時搶購不停！',
    buttonText: '逛逛活動',
    image: '/images/Ad/Ad1003-NewYear.png',
    link: '/beauty',
  },
  {
    id: 4,
    title: '滿額免運活動',
    description: '全館消費滿千享免運，立即享受輕鬆購物！',
    buttonText: '了解詳情',
    image: '/images/Ad/Ad1099-FreeFee.png',
    link: '/shop',
  },
])

function goToShop(link) {
  window.location.href = link
}
</script>

<style scoped>
.hero-carousel {
  background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
  padding: 2rem 0;
}
.carousel-item img {
  object-fit: cover;
  max-height: 450px;
  border-radius: 12px;
}
.carousel-item {
  min-height: 450px;
}
@media (max-width: 768px) {
  .carousel-item img {
    max-height: 300px;
  }
}
</style>
