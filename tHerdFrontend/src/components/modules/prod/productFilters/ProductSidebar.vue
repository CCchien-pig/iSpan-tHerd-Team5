<template>
  <aside class="product-sidebar p-3 border-end bg-light">
    <h5 class="fw-bold mb-3">篩選條件</h5>

    <!-- 子組件 -->
    <BrandFilter v-model:selected="filters.brandIds" />
    <PriceFilter v-model:range="filters.priceRange" />
    <RatingFilter v-model:rating="filters.rating" />

    <hr />
    <!-- 商品屬性篩選：動態生成 -->
    <div v-for="attr in attributes" :key="attr.attributeId" class="mb-3">
      <h6 class="fw-bold">{{ attr.attributeName }}</h6>
      <div class="d-flex flex-wrap gap-2">
        <button
          v-for="opt in attr.options"
          :key="opt.optionName"
          class="btn btn-sm"
          :class="{
            'btn-success': isSelected(attr.attributeName, opt.optionName),
            'btn-outline-secondary': !isSelected(attr.attributeName, opt.optionName)
          }"
          @click="toggleOption(attr.attributeName, opt.optionName)"
        >
          {{ opt.optionName }}
        </button>
      </div>
    </div>

    <div class="mt-4 d-flex justify-content-between">
      <button class="btn btn-outline-secondary btn-sm" @click="resetFilters">重設</button>
      <button class="btn btn-success btn-sm" @click="$emit('filter-change', filters)">套用</button>
    </div>
  </aside>
</template>

<script setup>
import { ref } from 'vue'
import BrandFilter from './sub/BrandFilter.vue'
import PriceFilter from './sub/PriceFilter.vue'
import RatingFilter from './sub/RatingFilter.vue'

// 模擬後端載入屬性清單 (可改成 API)
const attributes = ref([
  {
    attributeId: 1000,
    attributeName: '功效',
    options: [{ optionName: '保濕' }, { optionName: '抗老' }, { optionName: '美白' }]
  },
  {
    attributeId: 1001,
    attributeName: '性別',
    options: [{ optionName: '女性' }, { optionName: '男性' }, { optionName: '不限' }]
  },
  {
    attributeId: 1002,
    attributeName: '年齡層',
    options: [{ optionName: '18–25歲' }, { optionName: '26–40歲' }, { optionName: '41–60歲' }]
  }
])

const filters = ref({
  brandIds: [],
  priceRange: { min: 0, max: 0 },
  rating: 0,
  attributeFilters: []
})

function isSelected(attrName, optionName) {
  const f = filters.value.attributeFilters.find(f => f.name === attrName)
  return f && f.optionNames.includes(optionName)
}

function toggleOption(attrName, optionName) {
  let f = filters.value.attributeFilters.find(f => f.name === attrName)
  if (!f) {
    f = { name: attrName, optionNames: [] }
    filters.value.attributeFilters.push(f)
  }
  const idx = f.optionNames.indexOf(optionName)
  if (idx > -1) f.optionNames.splice(idx, 1)
  else f.optionNames.push(optionName)
}

function resetFilters() {
  filters.value = { brandIds: [], priceRange: { min: 0, max: 0 }, rating: 0, attributeFilters: [] }
}
</script>

<style scoped>
.product-sidebar {
  width: 260px;
  min-height: 100%;
}
.btn-sm {
  border-radius: 20px;
}
</style>
