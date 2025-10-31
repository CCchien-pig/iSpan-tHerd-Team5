<template>
  <div class="center-narrow py-5">
    <h3 class="text-center mb-4 main-color-green-text">聯絡客服</h3>

    <form @submit.prevent="submitTicket" class="card p-4 shadow-sm">
      <!-- 問題分類 -->
      <div class="mb-3">
        <label class="form-label">問題分類</label>
        <select v-model="form.categoryId" class="form-select">
          <option disabled value="">請選擇</option>
          <option v-for="c in categories" :key="c.categoryId" :value="c.categoryId">
            {{ c.categoryName }}
          </option>
        </select>
      </div>

      <!-- 主旨 -->
      <div class="mb-3">
        <label class="form-label">主旨</label>
        <input v-model="form.subject" class="form-control" placeholder="請輸入問題主旨" />
      </div>

      <!-- 問題描述 -->
      <div class="mb-3">
        <label class="form-label">問題描述</label>
        <textarea v-model="form.messageText" rows="4" class="form-control"></textarea>
      </div>

      <!-- 上傳圖片 -->
      <div class="mb-3">
        <label class="form-label">上傳附件（限 1 張圖片）</label>
        <input type="file" accept="image/*" @change="onFileChange" class="form-control" />
        <div v-if="previewUrl" class="text-center mt-3">
          <img :src="previewUrl" alt="預覽" style="max-width: 200px; border-radius: 8px;" />
        </div>
      </div>

      <div class="text-center">
        <button class="btn btn-success px-4" type="submit" :disabled="loading">
          <span v-if="!loading">送出工單</span>
          <span v-else class="spinner-border spinner-border-sm"></span>
        </button>
      </div>
    </form>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { getCategories, createTicket } from '@/api/modules/cs/cstickets'

const loading = ref(false)
const categories = ref([])
const previewUrl = ref('')
const file = ref(null)

const form = ref({
  userId: 1, // 測試階段可固定
  categoryId: '',
  subject: '',
  priority: 2,
  messageText: ''
})

// 預覽圖片
const onFileChange = (e) => {
  const selected = e.target.files[0]
  if (!selected) return
  file.value = selected
  previewUrl.value = URL.createObjectURL(selected)
}

// 初始化載入 FAQ 分類
onMounted(async () => {
  categories.value = await getCategories()
})

// 提交工單
async function submitTicket() {
  try {
    loading.value = true

    // 使用 FormData 封裝文字 + 檔案
    const formData = new FormData()
    formData.append('userId', form.value.userId)
    formData.append('categoryId', form.value.categoryId)
    formData.append('subject', form.value.subject)
    formData.append('priority', form.value.priority)
    formData.append('messageText', form.value.messageText)
    if (file.value) formData.append('image', file.value) // ✅ 關鍵：加上圖片

    const res = await createTicket(formData)
    if (res.success) {
      alert('工單建立成功！')
      // TODO: 可導到「我的工單」頁
      resetForm()
    } else {
      alert(res.message || '建立失敗')
    }
  } catch (err) {
    console.error(err)
    alert('伺服器錯誤')
  } finally {
    loading.value = false
  }
}

// 重置表單
function resetForm() {
  form.value = {
    userId: 1,
    categoryId: '',
    subject: '',
    priority: 2,
    messageText: ''
  }
  file.value = null
  previewUrl.value = ''
}
</script>

<style scoped>
.center-narrow {
  max-width: 600px;
  margin: auto;
}
</style>
