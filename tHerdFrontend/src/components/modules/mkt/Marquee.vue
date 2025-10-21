<template>
  <div class="site-promo-banner py-2">
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
    <div v-else class="text-center text-white small">
      🚀 載入促銷資訊中…🐛
    </div>
  </div>
</template>

<script>
import api from '@/components/modules/mkt/api'

export default {
  name: 'Marquee',
  data() {
    return {
      promoList: [],
      currentIndex: 0,
      timer: null,
      currentColor: '#ffffff' // ✅ 新增一個目前字體顏色
    }
  },
  async mounted() {
    await this.fetchCampaignData()
    this.startRotation()
  },
  beforeUnmount() {
    clearInterval(this.timer)
  },
  methods: {
    async fetchCampaignData() {
      try {
        const res = await api.get('/api/mkt/campaign/active')
        if (Array.isArray(res.data.data) && res.data.data.length > 0) {
          this.promoList = res.data.data
            .filter(item => item.campaignDescription)
            .map(item => ({
              title: item.campaignName || '(未命名活動)',
              description: item.campaignDescription || '(無描述)'
            }))
        }
      } catch (err) {
        console.error('❌ 無法取得活動資料', err)
      }
    },

    // ✅ 產生隨機顏色的方法
    getRandomColor() {
  const palette = ['#FFFFFF', // 亮白
    '#E8E8E8', // 銀灰
    '#F8F8F8', // 奶白
    '#FFD580', // 香檳金
    '#FFC77D', // 粉橘
    '#FFF5CC', // 米黃
    '#FFE2B3', // 淡杏橙
    '#F0F0F0',  // 珍珠白
    '#FFFFFF', 
    '#FFB347'
    ]
  return palette[Math.floor(Math.random() * palette.length)]
},



    startRotation() {
      if (this.promoList.length === 0) return

      // 初始化第一次顏色
      this.currentColor = this.getRandomColor()

      this.timer = setInterval(() => {
        this.currentIndex = (this.currentIndex + 1) % this.promoList.length
        this.currentColor = this.getRandomColor() // ✅ 每次換文字時換顏色
      }, 15000)
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
  animation: marquee 15s linear forwards;
  transition: color 0.5s ease; /* ✅ 顏色變化更柔順 */
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
