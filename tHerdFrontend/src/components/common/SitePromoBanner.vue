<template>
  <div class="site-promo-banner py-2">
    <div class="marquee">
      <div
        class="marquee-content"
        :key="currentIndex"
      >
        <strong>{{ promoList[currentIndex].title }}</strong>
        <span class="ms-3">{{ promoList[currentIndex].description }}</span>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: 'SitePromoBanner',
  data() {
    return {
      currentIndex: 0, // 目前顯示第幾筆內容
      promoList: [
        { title: '"定期自動送貨享優惠"特價品', description: '首次訂購可享20%或更多折扣，後續訂單均享10%折扣。' },
        { title: '🐮 tHerd 會員獨享', description: '全館消費滿 NT$2,000 免運費' },
        { title: '💥 本週限時優惠', description: '買三送一，數量有限售完為止' },
      ],
    };
  },
  mounted() {
    // 每 8 秒換下一筆
    this.startRotation();
  },
  beforeUnmount() {
    clearInterval(this.timer);
  },
  methods: {
    startRotation() {
      this.timer = setInterval(() => {
        this.currentIndex = (this.currentIndex + 1) % this.promoList.length;
      }, 25000); // 這個時間要跟動畫時間對應
    },
  },
};
</script>

<style scoped>
.site-promo-banner {
  background: linear-gradient(135deg, #17a2b8, #20c997);
  color: white;
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
  animation: marquee 25s linear forwards;
  /* forwards 代表播完就停在尾巴，等下個內容 */
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
