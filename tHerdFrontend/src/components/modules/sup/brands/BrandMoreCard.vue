<!-- 接收 accordions（已以 Title 分組）與 imagesRight，左側渲染 h3 + ul/li，右側渲染所有圖片 -->

<!-- BrandMoreCard.vue -->
<template>
  <div class="card bg-soft">
    <!-- 在 body 上加左右 padding，桌面稍大、手機較小 -->
    <div class="card-body px-3 px-lg-4">
      <div class="row g-4">
        <div :class="imagesRight?.length ? 'col-12 col-lg-8' : 'col-12'">
          <section v-for="grp in groups" :key="grp.contentKey" class="mb-3">
            <h4 class="mb-2">{{ grp.contentKey }}</h4>
            <ul class="list-unstyled m-0">
              <li v-for="item in grp.items" :key="`${grp.contentKey}-${item.order}`" class="li-row">
                <svg class="li-dot" viewBox="0 0 16 16" aria-hidden="true">
                  <circle cx="8" cy="8" r="6" :fill="accentColor" />
                </svg>
                <span class="li-text" v-html="item.body"></span>
              </li>
            </ul>
          </section>
        </div>

        <!-- 右：圖片（有圖才渲染），保持最大高度與下緣留白 -->
        <div
          v-if="imagesRight?.length"
          class="col-12 col-lg-4 d-flex flex-column align-items-center"
        >
          <img
            v-for="(url, idx) in imagesRight"
            :key="idx"
            :src="url"
            :alt="altText"
            class="img-fluid rounded brand-side-image"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  groups: { type: Array, default: () => [] }, // [{ contentKey, items:[{ title, body, order }] }]
  imagesRight: { type: Array, default: () => [] }, // string[]
  accentRgb: { type: Object, default: () => ({ r: 0, g: 147, b: 171 }) },
  altText: { type: String, default: '' },
})

// ✅ reactive 計算顏色（會隨父層 vm.mainColor 更新）
const accentColor = computed(() => {
  const { r, g, b } = props.accentRgb
  return `rgb(${r}, ${g}, ${b})`
})
</script>

<style scoped>
.bg-soft {
  background-color: #f5f5f5;
}
/* 列表項：圖示與文字對齊 */
.li-row {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  padding: 0.25rem 0;
  line-height: 1.6;
}
.li-dot {
  width: 14px;
  height: 14px;
  flex: 0 0 14px;
  margin-top: 0.35rem; /* 微調，讓圓點與文字第一行對齊 */
}
.li-text {
  flex: 1 1 auto;
}

/* 右側圖片：參考頁面行為 */
.brand-side-image {
  max-height: 500px;
  margin-bottom: 30px !important;
  width: 100%;
  height: auto;
  object-fit: contain;
}
/* 大畫面時，讓內容左右更舒適 */
@media (min-width: 1200px) {
  .card-body {
    padding-left: 2rem;
    padding-right: 2rem;
  }
}
</style>
