<!-- src/pages/modules/cs/TicketCreate.vue -->
<template>
  <div class="ticket-create container py-5">
    <!-- 導覽 -->
    <nav aria-label="breadcrumb">
      <ol class="breadcrumb mb-4">
        <li class="breadcrumb-item"><router-link to="/">首頁</router-link></li>
        <li class="breadcrumb-item"><router-link to="/cs">客服中心</router-link></li>
        <li class="breadcrumb-item active" aria-current="page">建立工單</li>
      </ol>
    </nav>

    <h3 class="mb-4 fw-bold">建立客服工單</h3>

    <el-form
      :model="form"
      ref="formRef"
      label-width="120px"
      :rules="rules"
      status-icon
    >
      <el-form-item label="主題" prop="subject">
        <el-input v-model="form.subject" placeholder="請輸入您的問題主題"></el-input>
      </el-form-item>

      <el-form-item label="問題分類" prop="categoryId">
        <el-select v-model="form.categoryId" placeholder="請選擇分類">
          <el-option
            v-for="c in categories"
            :key="c.categoryId"
            :label="c.categoryName"
            :value="c.categoryId"
          />
        </el-select>
      </el-form-item>

      <el-form-item label="問題描述" prop="content">
        <el-input
          type="textarea"
          rows="5"
          v-model="form.content"
          placeholder="請詳細描述您的問題或情況"
        />
      </el-form-item>

      <el-form-item label="附件上傳">
        <el-upload
          class="upload-demo"
          action="#"
          :auto-upload="false"
          :on-change="handleFileChange"
          multiple
        >
          <el-button>選擇檔案</el-button>
          <template #tip>
            <div class="el-upload__tip">（可附上截圖或文件，最多 3 個檔案）</div>
          </template>
        </el-upload>
      </el-form-item>

      <el-form-item label="優先等級">
        <el-radio-group v-model="form.priority">
          <el-radio :label="1">一般</el-radio>
          <el-radio :label="2">中等</el-radio>
          <el-radio :label="3">高</el-radio>
        </el-radio-group>
      </el-form-item>

      <el-form-item>
        <el-button type="primary" :loading="loading" @click="onSubmit">
          送出工單
        </el-button>
        <el-button @click="resetForm">重填</el-button>
      </el-form-item>
    </el-form>

    <el-dialog v-model="dialogVisible" title="提交成功" width="400px" center>
      <p>您的問題已成功送出，我們會盡快處理並通知您結果。</p>
      <template #footer>
        <el-button type="primary" @click="toList">查看我的工單</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { createTicket, getFaqCategories } from '@/api/modules/cs/csticket'

const formRef = ref()
const dialogVisible = ref(false)
const loading = ref(false)
const categories = ref([])

const form = reactive({
  subject: '',
  categoryId: null,
  content: '',
  attachments: [],
  priority: 1,
})

const rules = {
  subject: [{ required: true, message: '請輸入主題', trigger: 'blur' }],
  categoryId: [{ required: true, message: '請選擇分類', trigger: 'change' }],
  content: [{ required: true, message: '請輸入內容', trigger: 'blur' }],
}

const handleFileChange = (file, fileList) => {
  form.attachments = fileList
}

const onSubmit = async () => {
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    loading.value = true
    try {
      const res = await createTicket(form)
      if (res) {
        dialogVisible.value = true
      }
    } catch {
      ElMessage.error('送出失敗，請稍後再試')
    } finally {
      loading.value = false
    }
  })
}

const resetForm = () => formRef.value.resetFields()
const toList = () => {
  dialogVisible.value = false
  window.location.href = '/cs/ticket/list'
}

onMounted(async () => {
  categories.value = await getFaqCategories()
})
</script>

<style scoped>
.ticket-create {
  max-width: 720px;
}
</style>
