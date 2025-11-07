<template>
  <div class="filter-section mb-4">
    <h6 class="fw-bold">品牌</h6>

    <!-- 🔍 搜尋框 -->
    <div class="input-group mb-2">
      <input
        type="text"
        v-model="searchKeyword"
        class="form-control form-control-sm"
        placeholder="搜尋品牌"
      />
      <button class="btn btn-outline-secondary btn-sm">
        <i class="bi bi-search"></i>
      </button>
    </div>

    <!-- ✅ 品牌清單 -->
    <div v-for="(brand, index) in visibleBrands" :key="brand.id" class="form-check small">
      <input
        class="form-check-input"
        type="checkbox"
        :id="'brand-' + brand.id"
        v-model="selectedBrands"
        :value="brand.id"
        @change="emitChange"
      />
      <label class="form-check-label" :for="'brand-' + brand.id">
        {{ brand.name }} ({{ brand.count }})
      </label>
    </div>

    <!-- 顯示更多 -->
    <div v-if="filteredBrands.length > maxVisible" class="text-primary mt-1 small" role="button" @click="toggleExpand">
      {{ expanded ? '收起' : `+ 顯示 ${filteredBrands.length - maxVisible} 更多` }}
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'

const props = defineProps({
  brands: { type: Array, required: true }
})
const emit = defineEmits(['update:brands'])

const searchKeyword = ref('')
const expanded = ref(false)
const maxVisible = 5
const selectedBrands = ref([])

// 🔍 篩選品牌
const filteredBrands = computed(() =>
  props.brands.filter(b => b.name.toLowerCase().includes(searchKeyword.value.toLowerCase()))
)
const visibleBrands = computed(() =>
  expanded.value ? filteredBrands.value : filteredBrands.value.slice(0, maxVisible)
)

const toggleExpand = () => (expanded.value = !expanded.value)
const emitChange = () => emit('update:brands', selectedBrands.value)
</script>
