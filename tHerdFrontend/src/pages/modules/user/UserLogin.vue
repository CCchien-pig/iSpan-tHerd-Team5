<!-- /src/pages/auth/Login.vue -->

<template>
  <div class="container py-4">
    <h2 class="mb-3">登入</h2>
    <div class="card p-3">
      <div class="mb-3">
        <label class="form-label">Email</label>
        <input v-model="email" type="email" class="form-control" placeholder="you@example.com" />
      </div>
      <div class="mb-3">
        <label class="form-label">密碼</label>
        <input v-model="password" type="password" class="form-control" placeholder="••••••••" />
      </div>
      <div v-if="errMsg" class="alert alert-danger py-2">{{ errMsg }}</div>
      <button class="btn btn-success" :disabled="busy" @click="doLogin">
        {{ busy ? '登入中…' : '登入' }}
      </button>

      <router-link class="btn btn-link" :to="{ name: 'register' }">沒有帳號？去註冊</router-link>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()

const email = ref('')
const password = ref('')
const busy = ref(false)
const errMsg = ref('')

async function doLogin() {
  errMsg.value = ''
  busy.value = true
  try {
    await auth.login(email.value.trim(), password.value)
    const back = (route.query.redirect && String(route.query.redirect)) || '/'
    router.replace(back)
  } catch (e: any) {
    errMsg.value = e?.response?.data?.error || '登入失敗，請確認帳號或密碼'
  } finally {
    busy.value = false
  }
}
</script>

