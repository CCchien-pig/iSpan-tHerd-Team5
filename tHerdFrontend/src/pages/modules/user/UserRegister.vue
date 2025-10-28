<!-- /src/pages/auth/Register.vue -->
<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
// import { http } from '@/api/http' // 後端完成註冊 API 再解開

const router = useRouter()
const email = ref('')
const password = ref('')
const lastName = ref('')
const firstName = ref('')
const busy = ref(false)
const msg = ref('')

async function doRegister() {
  busy.value = true
  msg.value = '尚未接上後端註冊 API，請先用測試帳號或找後端開 /api/auth/register'
  // 例：await http.post('/auth/register', { email: email.value, password: password.value, lastName: lastName.value, firstName: firstName.value })
  // router.replace({ name: 'login' })
  busy.value = false
}
</script>

<template>
  <div class="container py-4">
    <h2 class="mb-3">註冊</h2>
    <div class="card p-3">
      <div class="row">
        <div class="col-md-6 mb-3">
          <label class="form-label">姓氏</label>
          <input v-model="lastName" class="form-control" />
        </div>
        <div class="col-md-6 mb-3">
          <label class="form-label">名字</label>
          <input v-model="firstName" class="form-control" />
        </div>
      </div>
      <div class="mb-3">
        <label class="form-label">Email</label>
        <input v-model="email" type="email" class="form-control" placeholder="you@example.com" />
      </div>
      <div class="mb-3">
        <label class="form-label">密碼</label>
        <input v-model="password" type="password" class="form-control" placeholder="至少 8 碼含大小寫與數字" />
      </div>

      <button class="btn btn-primary" :disabled="busy" @click="doRegister">
        {{ busy ? '送出中…' : '註冊' }}
      </button>
      <p class="text-muted mt-2">{{ msg }}</p>
    </div>
  </div>
</template>