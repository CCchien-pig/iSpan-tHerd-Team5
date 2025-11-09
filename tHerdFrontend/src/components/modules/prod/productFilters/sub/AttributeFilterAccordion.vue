<template>
  <div class="accordion">
    <div
      v-for="attr in attributes"
      :key="attr.attributeId"
      class="accordion-item border rounded mb-2"
    >
      <h6
        class="accordion-header bg-light p-2 fw-bold d-flex justify-content-between align-items-center"
        @click="toggle(attr.attributeId)"
        style="cursor: pointer;"
      >
        {{ attr.attributeName }}
        <i class="bi" :class="isOpen(attr.attributeId) ? 'bi-chevron-up' : 'bi-chevron-down'"></i>
      </h6>

      <transition name="fade">
        <div v-show="isOpen(attr.attributeId)" class="p-2">
          <div class="d-flex flex-wrap gap-2">
            <button
              v-for="opt in attr.options"
              :key="opt.optionName"
              class="btn btn-sm"
              :class="{
                'btn-success': isSelected(attr.attributeId, opt.optionName),
                'btn-outline-secondary': !isSelected(attr.attributeId, opt.optionName)
              }"
              @click.stop="toggleOption(attr.attributeId, opt.optionName)"
            >
              {{ opt.optionName }}
            </button>
          </div>
        </div>
      </transition>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'

const props = defineProps({
  attributes: { type: Array, required: true },
  modelValue: { type: Array, required: true }
})
const emit = defineEmits(['update:modelValue'])

const openIds = ref([])
const localFilters = ref([...props.modelValue])

function toggle(id) {
  if (openIds.value.includes(id))
    openIds.value = openIds.value.filter(x => x !== id)
  else
    openIds.value.push(id)
}
function isOpen(id) {
  return openIds.value.includes(id)
}

function isSelected(attributeId, optionName) {
  const f = localFilters.value.find(f => f.attributeId === attributeId)
  return f && f.valueNames.includes(optionName)
}

function toggleOption(attributeId, optionName) {
  const arr = [...localFilters.value]
  let f = arr.find(f => f.attributeId === attributeId)
  if (!f) f = { attributeId, valueNames: [] }

  const idx = f.valueNames.indexOf(optionName)
  if (idx > -1) f.valueNames.splice(idx, 1)
  else f.valueNames.push(optionName)

  const newList = arr.filter(f => f.valueNames.length > 0)
  if (!arr.find(f => f.attributeId === attributeId)) newList.push(f)

  localFilters.value = newList
  emit('update:modelValue', newList)
}

// 🧹 🔥 關鍵：監聽父層清空
watch(
  () => props.modelValue,
  (newVal) => {
    if (Array.isArray(newVal) && newVal.length === 0) {
      localFilters.value = []
      openIds.value = []
    }
  },
  { deep: true }
)
</script>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: all 0.25s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
  max-height: 0;
}
</style>
