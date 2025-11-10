<template>
  <div class="filter-section mb-4">
    <h6 class="fw-bold">品牌</h6>

    <!-- 🔍 搜尋框 -->
    <div class="position-relative mb-2">
      <input
        type="text"
        v-model="keyword"
        class="form-control form-control-sm"
        placeholder="搜尋品牌..."
        :disabled="loading || error"
      />
      <div
        v-if="loading"
        class="spinner-border spinner-border-sm text-success position-absolute top-50 end-0 me-2 translate-middle-y"
      ></div>
    </div>

    <!-- ✅ 多選 Checkbox -->
    <div class="brand-list border rounded p-2" style="max-height: 200px; overflow-y: auto;">
      <!-- 🔸 全選 -->
      <div class="form-check small mb-1 border-bottom pb-1">
        <input
          class="form-check-input"
          type="checkbox"
          id="brand-select-all"
          :checked="isAllSelected"
          :indeterminate.prop="isIndeterminate"
          @change="toggleSelectAll"
        />
        <label class="form-check-label fw-bold" for="brand-select-all">
          全選
        </label>
      </div>

      <!-- 🔸 品牌列表 -->
      <div
        v-for="b in filteredBrands"
        :key="b.brandId"
        class="form-check small"
      >
        <input
          class="form-check-input"
          type="checkbox"
          :id="`brand-${b.brandId}`"
          :value="b.brandId"
          v-model="selectedBrands"
          @change="emitChange"
        />
        <label class="form-check-label" :for="`brand-${b.brandId}`">
          {{ b.brandName }}
        </label>
      </div>

      <div v-if="!filteredBrands.length && !loading" class="text-muted small text-center py-2">
        無符合的品牌
      </div>
    </div>

    <div v-if="error" class="text-danger small mt-2">⚠️ 無法載入品牌資料</div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import ProductsApi from '@/api/modules/prod/ProductsApi'

// 🔹 props / emit
const emit = defineEmits(['update:modelValue'])
const props = defineProps({
  modelValue: {
    type: Array,
    default: () => []
  }
})

// 🔹 狀態
const selectedBrands = ref([...props.modelValue])
const keyword = ref('')
const brands = ref([])
const loading = ref(false)
const error = ref(false)

// ===================== 初始化 =====================
onMounted(async () => {
  await loadBrands()
})

// ===================== 載入品牌清單 =====================
async function loadBrands() {
  loading.value = true
  error.value = false
  try {
    const res = await ProductsApi.getBrandList()
    brands.value = (res || []).sort((a, b) =>
      a.brandName.localeCompare(b.brandName, 'zh-Hant')
    )
  } catch (err) {
    console.error('❌ 載入品牌失敗', err)
    error.value = true
  } finally {
    loading.value = false
  }
}

// ===================== 前端搜尋（開頭比對） =====================
const filteredBrands = computed(() => {
  const kw = keyword.value.trim().toLowerCase()
  if (!kw) return brands.value
  return brands.value.filter(b => b.brandName.toLowerCase().startsWith(kw))
})

// ✅ 監聽搜尋關鍵字變化 → 清空選取
watch(keyword, (val) => {
  if (val.length > 0) {
    selectedBrands.value = []       // 清空選取
    emit('update:modelValue', [])   // 通知父層同步清空
  }
})

// ===================== 全選控制 =====================
const isAllSelected = computed(() =>
  filteredBrands.value.length > 0 &&
  filteredBrands.value.every(b => selectedBrands.value.includes(b.brandId))
)

const isIndeterminate = computed(() => {
  const selectedCount = filteredBrands.value.filter(b => selectedBrands.value.includes(b.brandId)).length
  return selectedCount > 0 && selectedCount < filteredBrands.value.length
})

function toggleSelectAll(e) {
  const checked = e.target.checked
  if (checked) {
    const allVisibleIds = filteredBrands.value.map(b => b.brandId)
    selectedBrands.value = Array.from(new Set([...selectedBrands.value, ...allVisibleIds]))
  } else {
    const visibleIds = filteredBrands.value.map(b => b.brandId)
    selectedBrands.value = selectedBrands.value.filter(id => !visibleIds.includes(id))
  }
  emitChange()
}

// ===================== 雙向綁定 =====================
watch(selectedBrands, (val) => emit('update:modelValue', val))
function emitChange() {
  emit('update:modelValue', selectedBrands.value)
}
</script>

<style scoped>
.filter-section {
  font-size: 0.9rem;
}
.brand-list::-webkit-scrollbar {
  width: 6px;
}
.brand-list::-webkit-scrollbar-thumb {
  background: #ccc;
  border-radius: 4px;
}
.form-check-input:indeterminate {
  background-color: #ffc107;
  border-color: #ffc107;
}
</style>
