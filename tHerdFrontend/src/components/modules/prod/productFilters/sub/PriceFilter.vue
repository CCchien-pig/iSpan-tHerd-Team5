<template>
  <div class="filter-section mb-4">
    <h6 class="fw-bold">價格</h6>
    <div v-for="range in priceRanges" :key="range.label" class="form-check small">
      <input
        class="form-check-input"
        type="checkbox"
        :id="range.label"
        v-model="selectedRanges"
        :value="range"
        @change="emitChange"
      />
      <label class="form-check-label" :for="range.label">
        {{ range.label }} ({{ range.count }})
      </label>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'

const props = defineProps({
  priceRanges: { type: Array, required: true }
})
const emit = defineEmits(['update:prices'])

const selectedRanges = ref([])

const emitChange = () => emit('update:prices', selectedRanges.value)
</script>
