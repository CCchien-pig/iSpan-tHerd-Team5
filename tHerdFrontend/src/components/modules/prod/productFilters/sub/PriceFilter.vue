<template>
  <div class="filter-section mb-4">
    <h6 class="fw-bold">價格</h6>

    <div class="d-flex align-items-center mt-2">
      <input
        type="number"
        class="form-control form-control-sm me-1"
        placeholder="最低價"
        v-model.number="modelValue.min"
        min="0"
      />
      <span>–</span>
      <input
        type="number"
        class="form-control form-control-sm ms-1"
        placeholder="最高價"
        v-model.number="modelValue.max"
        min="0"
      />
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  modelValue: { type: Object, default: () => ({ min: null, max: null }) }
})
const emit = defineEmits(['update:modelValue'])

// 監聽雙向綁定
function updatePrice(key, value) {
  const updated = { ...props.modelValue, [key]: value }
  // 防呆檢查
  if (updated.max && updated.min && updated.max < updated.min) return
  emit('update:modelValue', updated)
}
</script>
