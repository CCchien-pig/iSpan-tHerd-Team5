<template>
  <div class="filter-section mb-4">
    <h6 class="fw-bold">評價</h6>

    <!-- ✅ 多選星等 -->
    <div
      v-for="rate in ratings"
      :key="rate.value"
      class="d-flex align-items-center mb-1 rating-option"
      @click="toggleRating(rate.value)"
    >
      <input
        class="form-check-input me-2"
        type="checkbox"
        :checked="modelValue.includes(rate.value)"
        readonly
      />
      <div class="stars">
        <i
          v-for="i in 5"
          :key="i"
          class="bi"
          :class="i <= rate.value ? 'bi-star-fill text-warning' : 'bi-star text-warning'"
        ></i>
      </div>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  ratings: {
    type: Array,
    default: () => [
      { value: 5 },
      { value: 4 },
      { value: 3 },
      { value: 2 },
      { value: 1 }
    ]
  },
  modelValue: {
    type: Array,
    default: () => []
  }
})

const emit = defineEmits(['update:modelValue'])

// ✅ 切換選取狀態（支援多選）
function toggleRating(val) {
  const current = [...props.modelValue]
  const index = current.indexOf(val)

  if (index > -1) {
    current.splice(index, 1) // 取消選取
  } else {
    current.push(val) // 新增選取
  }

  emit('update:modelValue', current)
}
</script>

<style scoped>
.rating-option {
  cursor: pointer;
  user-select: none;
  transition: background 0.2s;
  padding: 2px 4px;
  border-radius: 4px;
}
.rating-option:hover {
  background: #f8f9fa;
}
.stars i {
  font-size: 1rem;
}
</style>
