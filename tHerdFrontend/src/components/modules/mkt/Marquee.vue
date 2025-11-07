<template>
  <div class="site-promo-banner py-2">
    <!-- ✅ 有資料時跑馬燈 -->
    <div class="marquee" v-if="promoList.length > 0">
      <div
        class="marquee-content"
        :key="currentIndex"
        :style="{ color: currentColor }"
      >
        <strong>{{ promoList[currentIndex].title }}</strong>
        <span class="ms-3">{{ promoList[currentIndex].description }}</span>
      </div>
    </div>

    <!-- 🚀 載入中提示 -->
    <div v-else class="text-center text-white small">
      🚀 載入公告中…🐛
    </div>
  </div>
</template>

<script>
import axios from 'axios'

export default {
  name: 'Marquee',
  data() {
    return {
      promoList: [],
      currentIndex: 0,
      timer: null, // 控制輪播
      fetchTimer: null, // 控制自動更新
      currentColor: '#ffffff'
    }
  },
  async mounted() {
    await this.fetchMarqueeData()
    this.startRotation()
    this.startAutoRefresh()
  },
  beforeUnmount() {
    clearInterval(this.timer)
    clearInterval(this.fetchTimer)
  },
  methods: {
    // 📡 從後端撈跑馬燈資料
    async fetchMarqueeData() {
      try {
        const res = await axios.get('/api/mkt/ad/MarqueeList')
        if (Array.isArray(res.data) && res.data.length > 0) {
          this.promoList = res.data.map(item => ({
            title: item.title || '(未命名公告)',
            description: item.description || ''
          }))
        } else {
          this.promoList = []
        }
      } catch (err) {
        console.error('❌ 無法取得跑馬燈資料', err)
      }
    },

    // 🎨 顏色邏輯：柔和色系搭配白字背景
    getRandomColor() {
      const palette = [
        '#ffffff',
      ]
      return palette[Math.floor(Math.random() * palette.length)]
    },

    // 🔁 輪播公告
    startRotation() {
      if (this.promoList.length === 0) return
      this.currentColor = this.getRandomColor()
      clearInterval(this.timer)
      this.timer = setInterval(() => {
        this.currentIndex = (this.currentIndex + 1) % this.promoList.length
        this.currentColor = this.getRandomColor()
      }, 15000)
    },

    // ⏱ 每 60 秒自動重新抓取資料
    startAutoRefresh() {
      clearInterval(this.fetchTimer)
      this.fetchTimer = setInterval(async () => {
        const oldData = JSON.stringify(this.promoList)
        await this.fetchMarqueeData()
        const newData = JSON.stringify(this.promoList)
        // 若資料有變動，重新播放輪播
        if (oldData !== newData) {
          this.currentIndex = 0
          this.startRotation()
        }
      }, 60000) // ✅ 每 1 分鐘更新
    }
  }
}
</script>

<style scoped>
.site-promo-banner {
  background: linear-gradient(135deg, #17a2b8, #20c997);
  overflow: hidden;
}

.marquee {
  white-space: nowrap;
  overflow: hidden;
  box-sizing: border-box;
}

.marquee-content {
  display: inline-block;
  padding-left: 100%;
  animation: marquee 10s linear infinite;
  transition: color 0.5s ease;
  text-shadow: 0 1px 3px rgba(0, 0, 0, 0.3);
}

.marquee-content:hover {
  animation-play-state: paused;
}

@keyframes marquee {
  0% {
    transform: translateX(0%);
  }
  100% {
    transform: translateX(-100%);
  }
}
</style>
