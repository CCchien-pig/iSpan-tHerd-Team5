<template>
  <section class="hero-section">
    <div class="hero-inner container-fluid h-100">
      <div class="row h-100 m-0">
        <!-- 🔸 左側文字區 -->
        <div class="col-lg-6 col-12 px-5 hero-text-container">
          <transition name="fade" mode="out-in">
            <div
              v-if="currentSlide"
              :key="currentSlide.id"
              class="text-content text-start"
              :style="{ color: textColor }"
            >
              <h1 class="display-3 fw-bold mb-4">{{ currentSlide.title }}</h1>
              <p class="lead mb-4">{{ currentSlide.description }}</p>
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

          <!-- 🔸 輪播按鈕 -->
          <div class="carousel-indicators" v-if="slides.length > 1">
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
              v-if="currentSlide"
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
import axios from 'axios'

const slides = ref([])
const currentIndex = ref(0)
const intervalTime = 4000
let timer = null

// 🧠 自動切換輪播
const currentSlide = computed(() => slides.value[currentIndex.value] || null)
const textColor = computed(() => {
  if (!currentSlide.value) return 'rgb(0,112,131)'
  const name = currentSlide.value.title || ''
  if (name.includes('生日') || name.includes('節慶') || name.includes('聖誕')) {
    return 'rgb(178,34,34)'
  } else if (name.includes('新客') || name.includes('首購')) {
    return 'rgb(242,140,40)'
  } else if (name.includes('免運') || name.includes('運費')) {
    return 'rgb(242,201,76)'
  } else if (name.toUpperCase().includes('中秋') || name.includes('專屬')) {
    return 'rgb(123,92,168)'
  } else if (name.includes('限時') || name.includes('活動')) {
    return 'rgb(27,42,73)'
  } else {
    return 'rgb(0,112,131)'
  }
})

// 🧭 方法
function nextSlide() {
  if (slides.value.length > 0)
    currentIndex.value = (currentIndex.value + 1) % slides.value.length
}
function setSlide(index) {
  currentIndex.value = index
}
function goToShop(link) {
  if (link) window.location.href = link
}

// 📡 從後端抓輪播圖
async function loadCarouselAds() {
  try {
    const res = await axios.get('/api/mkt/ad/CarouselList')
    slides.value = res.data || []
    if (slides.value.length > 0) {
      startAutoSlide()
    }
  } catch (err) {
    console.error('載入輪播圖失敗：', err)
  }
}

// ⏱ 自動播放控制
function startAutoSlide() {
  stopAutoSlide()
  timer = setInterval(nextSlide, intervalTime)
}
function stopAutoSlide() {
  if (timer) clearInterval(timer)
}

onMounted(() => {
  loadCarouselAds()
})
onBeforeUnmount(() => {
  stopAutoSlide()
})
</script>

<style scoped>
/* 📋 與原樣式完全相同 */
.hero-section {
  position: relative;
  min-height: clamp(50vh, 70vh, 90vh);
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  background-color: #fff;
  padding: 0;
}

.hero-inner {
  max-width: 1400px;
  width: 100%;
}

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
