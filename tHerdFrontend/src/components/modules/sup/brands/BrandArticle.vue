<template>
  <section class="brand-article">
    <div v-if="loading" class="text-muted small">載入中…</div>
    <div v-else-if="error" class="text-danger small">{{ error }}</div>
    <article v-else v-html="content"></article>
  </section>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import axios from 'axios'

const props = defineProps({
  contentId: { type: Number, required: true },
  brandId: { type: Number, required: false },
})

const loading = ref(false)
const error = ref('')
const content = ref('')

async function fetchContent() {
  if (!props.contentId) return
  loading.value = true
  error.value = ''
  try {
    // TODO: 依實際 API 調整
    // 例：/api/cms/articles/{contentId}
    const res = await axios.get(`/api/cms/articles/${props.contentId}`)
    // 假設回傳 { success, data: { html } }
    if (res.data?.success) {
      content.value = res.data.data?.html || ''
    } else {
      error.value = res.data?.message || '讀取失敗'
    }
  } catch (e) {
    error.value = e?.response?.data?.message || '載入失敗'
  } finally {
    loading.value = false
  }
}

onMounted(fetchContent)
watch(() => props.contentId, fetchContent)
</script>

<style scoped>
.brand-article :deep(img) {
  max-width: 100%;
  height: auto;
}
.brand-article {
  margin-bottom: 1rem;
}
</style>
