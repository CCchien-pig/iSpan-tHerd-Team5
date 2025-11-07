<template>
  <div class="filter-section mb-4">
    <h6 class="fw-bold">評級</h6>
    <div v-for="rating in ratings" :key="rating.value" class="form-check small">
      <input
        class="form-check-input"
        type="checkbox"
        :id="'rating-' + rating.value"
        v-model="selectedRatings"
        :value="rating.value"
        @change="emitChange"
      />
      <label class="form-check-label" :for="'rating-' + rating.value">
        <span v-for="i in 5" :key="i" class="text-warning">
          <i :class="i <= rating.value ? 'bi bi-star-fill' : 'bi bi-star'"></i>
        </span>
        ({{ rating.count }})
      </label>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
const props = defineProps({
  ratings: { type: Array, required: true }
})
const emit = defineEmits(['update:ratings'])
const selectedRatings = ref([])

const emitChange = () => emit('update:ratings', selectedRatings.value)
</script>
