<!-- /src/pages/modules/user/ForgotPassword.vue -->
<template>
  <div class="container py-4" style="max-width:540px">
    <h3 class="mb-3">忘記密碼</h3>
    <p class="text-muted">輸入您的電子郵件，我們會寄送 6 位數驗證碼到您的信箱。</p>

    <div class="card p-3">
      <!-- Step 1: 填 Email -->
      <div v-if="step===1">
        <label class="form-label">電子郵件</label>
        <input v-model.trim="email" type="email" class="form-control" placeholder="you@example.com" :disabled="busy"/>
        <div class="d-grid gap-2 mt-3">
          <button class="btn btn-primary" :disabled="busy || !email" @click="requestCode">
            {{ busy ? '寄送中…' : '寄送驗證碼' }}
          </button>
        </div>
        <div v-if="msg" class="alert alert-info mt-3">{{ msg }}</div>
        <div v-if="err" class="alert alert-danger mt-3">{{ err }}</div>
      </div>

      <!-- Step 2: 驗證碼 -->
      <div v-else>
        <div class="mb-2 small text-muted">驗證碼已寄至：<strong>{{ email }}</strong></div>
        <label class="form-label">輸入 6 位數驗證碼</label>
        <input v-model.trim="code" maxlength="6" class="form-control" placeholder="例如：123456" :disabled="busy"/>
        <div class="d-grid gap-2 mt-3">
          <button class="btn btn-warning" :disabled="busy || code.length!==6" @click="verifyCode">
            {{ busy ? '驗證中…' : '驗證並寄送臨時密碼' }}
          </button>
          <button class="btn btn-outline-secondary" :disabled="busy || coolDown>0" @click="resendCode">
            重新寄送驗證碼<span v-if="coolDown>0">（{{ coolDown }}s）</span>
          </button>
        </div>
        <div v-if="msg" class="alert alert-info mt-3">{{ msg }}</div>
        <div v-if="err" class="alert alert-danger mt-3">{{ err }}</div>
      </div>
    </div>

    <div class="mt-3">
      <router-link :to="{ name: 'userlogin' }">返回登入</router-link>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { http } from '@/api/http'
import { useRouter } from 'vue-router'

const router = useRouter()
const step = ref(1)
const email = ref('')
const code = ref('')
const busy = ref(false)
const msg = ref('')
const err = ref('')
const coolDown = ref(0)
let timer = null

function startCooldown(sec=60){
  clearInterval(timer)
  coolDown.value = sec
  timer = setInterval(()=>{
    coolDown.value--
    if(coolDown.value<=0) clearInterval(timer)
  }, 1000)
}

async function requestCode(){
  err.value = ''; msg.value = ''
  if (!email.value) { err.value = '請輸入 Email'; return }
  busy.value = true
  try {
    await http.post('/auth/forgot', { email: email.value })
    msg.value = '驗證碼已寄出，請查看您的收件匣或垃圾信件匣。'
    step.value = 2
    startCooldown()
  } catch (e){
    err.value = e?.response?.data?.error || '寄送失敗，請稍後再試'
  } finally {
    busy.value = false
  }
}

async function resendCode(){
  err.value=''; msg.value=''
  if (!email.value) return
  busy.value = true
  try{
    await http.post('/auth/forgot', { email: email.value }) // 後端同一路徑可重寄（有節流）
    msg.value = '已重新寄送驗證碼。'
    startCooldown()
  }catch(e){
    err.value = e?.response?.data?.error || '重寄失敗'
  }finally{
    busy.value = false
  }
}

async function verifyCode(){
  err.value=''; msg.value=''
  if (!email.value || code.value.length!==6) return
  busy.value = true
  try{
    await http.post('/auth/forgot/verify', { email: email.value, code: code.value })
    msg.value = '驗證成功！臨時密碼已寄到您的信箱，請使用臨時密碼登入並盡快修改。'
    setTimeout(()=> router.push({ name: 'userlogin', query: { forgot: 1 } }), 1500)
  }catch(e){
    err.value = e?.response?.data?.error || '驗證失敗，請確認驗證碼'
  }finally{
    busy.value = false
  }
}
</script>
