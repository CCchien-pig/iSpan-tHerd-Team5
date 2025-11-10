<template>
  <section class="brands-section py-5">
    <div class="container">
      <h2 class="text-center mb-5">{{ title }}</h2>

      <!-- 可見視窗 -->
      <div class="overflow-hidden">
        <!-- 走馬燈軌道 -->
        <div
          class="track d-flex align-items-center"
          :style="trackStyle"
          @mouseenter="pause()"
          @mouseleave="play()"
        >
          <!-- 將資料複製一份接在後面，達到無縫循環 -->
          <div
            v-for="(brand, idx) in duplicated"
            :key="idx + '-' + (brand.brandId || brand.name)"
            class="brand-slot px-3"
          >
            <div class="brand-item text-center">
              <img
                :src="brand.logo || brand.logoUrl"
                :alt="brand.brandName || brand.name"
                class="img-fluid"
                @click="handleBrandClick(brand)"
              />
            </div>
          </div>
        </div>
      </div>

      <!-- 可選的左右控制 -->
      <div class="controls mt-3 text-center" v-if="showControls">
        <button class="btn btn-outline-secondary btn-sm me-2" @click="prev()">〈</button>
        <button class="btn btn-outline-secondary btn-sm" @click="next()">〉</button>
      </div>
    </div>
  </section>
</template>

<script>
export default {
  name: 'BrandSection',
  props: {
    title: { type: String, default: '熱門品牌' },
    brands: { type: Array, default: () => [] },
    // 一次顯示幾個
    visible: { type: Number, default: 6 },
    // 每張的寬度（px），配合 slot padding 決定整體視覺
    itemWidth: { type: Number, default: 160 },
    // 自動播放間隔（毫秒）
    interval: { type: Number, default: 2500 },
    // 是否顯示左右控制
    showControls: { type: Boolean, default: false },
  },
  data() {
    return {
      offset: 0,
      timer: null,
    }
  },
  computed: {
    // 複製一份以利無縫滾動
    duplicated() {
      return [...this.brands, ...this.brands]
    },
    totalWidth() {
      return this.duplicated.length * this.itemWidth
    },
    trackStyle() {
      return {
        width: this.totalWidth + 'px',
        transform: `translateX(-${this.offset}px)`,
        transition: 'transform .6s ease',
      }
    },
  },
  watch: {
    brands: {
      immediate: true,
      handler() {
        this.reset()
      },
    },
  },
  mounted() {
    this.play()
  },
  beforeUnmount() {
    this.clear()
  },
  methods: {
    handleBrandClick(brand) {
      this.$emit('brand-click', brand)
    },
    play() {
      this.clear()
      if (!this.brands?.length) return
      this.timer = setInterval(() => this.next(), this.interval)
    },
    pause() {
      this.clear()
    },
    clear() {
      if (this.timer) {
        clearInterval(this.timer)
        this.timer = null
      }
    },
    reset() {
      this.offset = 0
      this.play()
    },
    next() {
      const maxOffset = this.brands.length * this.itemWidth // 到一輪末端
      this.offset += this.itemWidth
      if (this.offset >= maxOffset) {
        // 抵達第一輪末端，瞬移回起點（利用 duplicated 無縫）
        this.offset = 0
      }
    },
    prev() {
      const maxOffset = this.brands.length * this.itemWidth
      this.offset -= this.itemWidth
      if (this.offset < 0) {
        this.offset = maxOffset - this.itemWidth
      }
    },
  },
}
</script>

<style scoped>
.brand-item img {
  max-height: 60px;
  filter: grayscale(100%);
  transition:
    filter 0.3s ease,
    transform 0.15s ease;
  cursor: pointer;
}
.brand-item:hover img {
  filter: grayscale(0%);
  transform: translateY(-1px);
}
.track {
  will-change: transform;
}
.brand-slot {
  width: 160px; /* 與 itemWidth 一致 */
}
@media (max-width: 768px) {
  .brand-slot {
    width: 120px;
  }
}
</style>
