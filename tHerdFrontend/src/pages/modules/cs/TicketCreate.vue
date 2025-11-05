<template>
  <div class="center-narrow py-5">
    <h3 class="text-center mb-4 main-color-green-text">聯絡客服</h3>

    <form @submit.prevent="submitTicket" class="card p-4 shadow-sm">
      <!-- 問題分類 -->
      <div class="mb-3">
        <label class="form-label">問題分類</label>
       <select v-model="form.categoryId" class="form-select" :class="{ 'is-invalid': errors.categoryId }">
  <option disabled value="">請選擇</option>
  <option v-for="c in categories" :key="c.categoryId" :value="c.categoryId">
    {{ c.categoryName }}
  </option>
</select>
<div v-if="errors.categoryId" class="invalid-feedback">
  {{ errors.categoryId }}
</div>

      </div>

      <!-- 聯絡信箱 -->
<div class="mb-3">
  <label class="form-label">聯絡信箱 <span class="text-danger">*</span></label>
  <input
  v-model="form.email"
  type="email"
  class="form-control"
  :class="{ 'is-invalid': errors.email }"
  placeholder="請輸入您的電子郵件"
  required
/>
<div class="form-text">客服回覆將寄送至此信箱。</div>
<div v-if="errors.email" class="invalid-feedback">
  {{ errors.email }}
</div>

</div>


      <!-- 主旨 -->
      <div class="mb-3">
        <label class="form-label">主旨</label>
        <input
    v-model="form.subject"
    class="form-control"
    :class="{ 'is-invalid': errors.subject }"
    placeholder="請輸入問題主旨"
  />
  <div v-if="errors.subject" class="invalid-feedback">
    {{ errors.subject }}
  </div>
</div>

<!-- 問題描述 -->
<div class="mb-3">
  <label class="form-label">問題描述</label>
  <textarea
    v-model="form.messageText"
    rows="4"
    class="form-control"
    :class="{ 'is-invalid': errors.messageText }"
  ></textarea>
  <div v-if="errors.messageText" class="invalid-feedback">
    {{ errors.messageText }}
  </div>
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
import { useRouter } from 'vue-router' 
import { getCategories, createTicket } from '@/api/modules/cs/cstickets'
import { useAuthStore } from '@/stores/auth' 

const auth = useAuthStore() //使用 Auth Store
const router = useRouter() 
const loading = ref(false)
const categories = ref([])
const previewUrl = ref('')
const file = ref(null)

const errors = ref({})

const form = ref({
  userId: auth.user?.userNumberId || 0,
  email: '',
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

async function submitTicket() {
  // 🔹 驗證欄位
  errors.value = {}
  if (!form.value.categoryId) errors.value.categoryId = '請選擇問題分類'
  if (!form.value.email || !/\S+@\S+\.\S+/.test(form.value.email))
    errors.value.email = '請輸入有效的電子郵件'
  if (!form.value.subject.trim()) {
  errors.value.subject = '主旨不可為空白'
} else if (form.value.subject.trim().length < 2) {
  errors.value.subject = '主旨至少需2個字以上'
}
 if (!form.value.messageText.trim()) {
  errors.value.messageText = '請輸入問題描述'
} else if (form.value.messageText.trim().length < 5) {
  errors.value.messageText = '問題描述請至少輸入 5 個字'
}
  // 檢查圖片檔案npm install sweetalert2

  if (file.value) {
    if (!file.value.type.startsWith('image/')) {
      alert('附件必須是圖片檔案')
      return
    }
    if (file.value.size > 5 * 1024 * 1024) {
      alert('圖片大小不得超過 5MB')
      return
    }
  }

 
if (Object.keys(errors.value).length > 0) {
  // 不跳 Swal，只是把錯誤顯示在欄位下方
  return
}

  try {
    loading.value = true


    // 使用 FormData 封裝文字 + 檔案
    const formData = new FormData()
    formData.append('userId', form.value.userId)
    formData.append('email', form.value.email) // ✅ 新增這行
    formData.append('categoryId', form.value.categoryId)
    formData.append('subject', form.value.subject)
    formData.append('priority', form.value.priority)
    formData.append('messageText', form.value.messageText)
    if (file.value) formData.append('image', file.value) // ✅ 關鍵：加上圖片

    const res = await createTicket(formData)
    if (res.success) {
router.push('/cs/ticket/success') // ✅ 跳轉到成功頁面
  resetForm()
}
 else {
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
    userId: auth.user?.userNumberId || 0,
    email: '',                   // ✅ 清空 email
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
/* ✳️ 讓紅框顯示更明顯 */
.is-invalid {
  border-color: #dc3545 !important;
  background-color: #fff6f6 !important;
}
.invalid-feedback {
  color: #dc3545;
}
</style>
