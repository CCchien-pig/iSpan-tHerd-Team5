<template>
  <section class="hero-section">
    <div class="hero-inner container-fluid h-100">
      <div class="row h-100 m-0">
        <!-- 🔸 左側文字區 -->
        <div class="col-lg-6 col-12 px-5 hero-text-container">
          <transition name="fade" mode="out-in">
            <div
              :key="currentSlide.id"
              class="text-content text-start"
              :style="{ color: textColor }"
            >
              <h1 class="display-3 fw-bold mb-4">
                {{ currentSlide.title }}
              </h1>
              <p class="lead mb-4">
                {{ currentSlide.description }}
              </p>
              <button
                class="btn btn-lg px-4 py-2"
                :style="{
                  backgroundColor: textColor,
                  borderColor: textColor,
                  color: '#fff'
                }"
                @click="goToShop(currentSlide.link)"
              >
                <i class="bi bi-cart3 me-2"></i>
                {{ currentSlide.buttonText }}
              </button>
            </div>
          </transition>

          <!-- 🔸 輪播按鈕移到左邊底部 -->
          <div class="carousel-indicators">
            <button
              v-for="(item, index) in slides"
              :key="item.id"
              @click="setSlide(index)"
              :class="['indicator', { active: index === currentIndex }]"
            ></button>
          </div>
        </div>

        <!-- 🔸 右側圖片區 -->
        <div class="col-lg-6 col-12 p-0 hero-image-container">
          <transition name="fade" mode="out-in">
            <img
              :key="currentSlide.image"
              :src="currentSlide.image"
              :alt="currentSlide.title"
              class="hero-img"
            />
          </transition>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'

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

const currentIndex = ref(0)
const intervalTime = 4000
let timer = null

const currentSlide = computed(() => slides.value[currentIndex.value])

/** 🎨 文字與按鈕顏色邏輯 */
const textColor = computed(() => {
  const name = currentSlide.value.title
  if (name.includes('生日') || name.includes('節慶') || name.includes('聖誕')) {
    return 'rgb(178, 34, 34)'
  } else if (name.includes('新客') || name.includes('首購')) {
    return 'rgb(242, 140, 40)'
  } else if (name.includes('免運') || name.includes('運費')) {
    return 'rgb(242, 201, 76)'
  } else if (name.toUpperCase().includes('中秋') || name.includes('專屬')) {
    return 'rgb(123, 92, 168)'
  } else if (name.includes('限時') || name.includes('活動')) {
    return 'rgb(27, 42, 73)'
  } else {
    return 'rgb(0, 112, 131)'
  }
})

function nextSlide() {
  currentIndex.value = (currentIndex.value + 1) % slides.value.length
}
function setSlide(index) {
  currentIndex.value = index
}
function goToShop(link) {
  window.location.href = link
}

onMounted(() => {
  timer = setInterval(nextSlide, intervalTime)
})
onBeforeUnmount(() => {
  clearInterval(timer)
})
</script>

<style scoped>
.hero-section {
  position: relative;
  min-height: clamp(50vh, 70vh, 90vh); /* 跟著視窗縮放 */
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  background-color: #fff;
  padding: 0;
}

/* 💡 限制左右寬度產生留白 */
.hero-inner {
  max-width: 1400px; /* 控制最大寬度 */
  width: 100%;
}

/* 📝 左側文字區置中 */
.hero-text-container {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  position: relative;
}

.text-content {
  max-width: 500px;
  text-align: left;
}

/* 🖼 右側圖片只填右半邊 */
.hero-image-container {
  padding: 0;
  display: flex;
  justify-content: center;
  align-items: center;
}

.hero-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

/* 📍 輪播小圓點移到左邊文字區底部 */
.carousel-indicators {
  position: absolute;
  bottom: 20px;
  left: 50%;
  transform: translateX(-50%);
  display: flex;
  justify-content: center;
  gap: 0.8rem;
}

.indicator {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  background-color: #ccc;
  border: none;
  cursor: pointer;
  transition: background-color 0.3s ease;
}

.indicator.active {
  background-color: #007083;
}

/* ✨ 淡入淡出動畫 */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.5s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

/* 📱 RWD */
@media (max-width: 992px) {
  .hero-section {
    flex-direction: column;
  }

  .hero-image-container {
    height: 40vh;
  }

  .hero-img {
    height: 100%;
  }

  .carousel-indicators {
    position: relative;
    bottom: auto;
    margin-top: 1rem;
    transform: none;
  }
}
</style>
