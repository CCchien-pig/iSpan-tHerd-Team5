<!-- SortingSelect.vue -->
<template>
  <div class="d-flex align-items-center">
    <label class="me-2 text-muted small">排序方式</label>
    <select
      v-model="selected"
      class="form-select form-select-sm"
      style="width: auto"
      @change="emitChange"
    >
      <option
        v-for="option in options"
        :key="option.value"
        :value="option.value"
      >
        {{ option.label }}
      </option>
    </select>
  </div>
</template>

<script setup>
import { ref, watch, onMounted } from "vue"

const props = defineProps({
  sortBy: { type: String, default: "newest" },
  sortDesc: { type: Boolean, default: true },
})

const emit = defineEmits(["update:sortBy", "update:sortDesc", "change"])

// 🔹 前端選單對應後端 SQL 欄位
const options = [
  { label: "最新上架", value: "newest-desc", sortBy: "newest", sortDesc: true },
  { label: "價格：低 → 高", value: "price-asc", sortBy: "price", sortDesc: false },
  { label: "價格：高 → 低", value: "price-desc", sortBy: "price", sortDesc: true },
  { label: "商品名稱 A→Z", value: "name-asc", sortBy: "name", sortDesc: false },
  { label: "商品名稱 Z→A", value: "name-desc", sortBy: "name", sortDesc: true },
  { label: "品牌名稱 A→Z", value: "brand-asc", sortBy: "brand", sortDesc: false },
  { label: "品牌名稱 Z→A", value: "brand-desc", sortBy: "brand", sortDesc: true },
]

const selected = ref("newest-desc")

// 初始化時根據 props 設定
onMounted(() => {
  const current = options.find(
    (o) => o.sortBy === props.sortBy && o.sortDesc === props.sortDesc
  )
  if (current) selected.value = current.value
})

function emitChange() {
  const opt = options.find((o) => o.value === selected.value)
  if (opt) {
    emit("update:sortBy", opt.sortBy)
    emit("update:sortDesc", opt.sortDesc)
    emit("change", { sortBy: opt.sortBy, sortDesc: opt.sortDesc })
  }
}
</script>