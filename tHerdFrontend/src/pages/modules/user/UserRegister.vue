<!-- /src/pages/modules/user/UserRegister.vue -->
<template>
  <div class="container py-4">
    <h2 class="mb-3">註冊</h2>

    <div class="card p-3">
      <!-- 姓名 -->
      <div class="row">
        <div class="col-md-6 mb-3">
          <label class="form-label">姓氏 <span class="text-danger">*</span></label>
          <input v-model.trim="lastName" class="form-control" />
        </div>
        <div class="col-md-6 mb-3">
          <label class="form-label">名字 <span class="text-danger">*</span></label>
          <input v-model.trim="firstName" class="form-control" />
        </div>
      </div>

      <!-- Email -->
      <div class="mb-3">
        <label class="form-label">Email <span class="text-danger">*</span></label>
        <input
          v-model.trim="email"
          type="email"
          class="form-control"
          placeholder="you@example.com"
        />
      </div>

      <!-- 密碼 -->
      <div class="mb-3">
        <label class="form-label">密碼 <span class="text-danger">*</span></label>
        <input
          v-model="password"
          type="password"
          class="form-control"
          placeholder="至少 8 碼，建議含大小寫與數字"
        />
      </div>

      <!-- 手機 + 使用推薦碼 -->
      <div class="row">
        <div class="col-md-6 mb-3">
          <label class="form-label">手機號碼 <span class="text-danger">*</span></label>
          <input
            v-model.trim="phoneNumber"
            type="tel"
            class="form-control"
            placeholder="0900000000"
          />
        </div>
        <div class="col-md-6 mb-3">
          <label class="form-label">使用推薦碼</label>
          <input v-model.trim="usedReferralCode" class="form-control" />
        </div>
      </div>

      <!-- 性別 -->
      <div class="mb-3">
        <label class="form-label d-block">性別 <span class="text-danger">*</span></label>
        <div class="btn-group" role="group" aria-label="Gender">
          <input
            type="radio"
            class="btn-check"
            name="gender"
            id="gender-male"
            value="男"
            v-model="gender"
            autocomplete="off"
          />
          <label class="btn btn-outline-secondary" for="gender-male">男</label>

          <input
            type="radio"
            class="btn-check"
            name="gender"
            id="gender-female"
            value="女"
            v-model="gender"
            autocomplete="off"
          />
          <label class="btn btn-outline-secondary" for="gender-female">女</label>
        </div>
      </div>

      <!-- 錯誤/訊息 -->
      <div v-if="msg" :class="['alert', msgTypeClass, 'py-2']">
  {{ msg }}
  <template v-if="!isError && emailVerifyHint">
    <br />
    <small class="text-muted">{{ emailVerifyHint }}</small>
  </template>
</div>
<div v-if="resendMsg" class="alert alert-info py-2">{{ resendMsg }}</div>


      <!-- 動作 -->
      <div class="d-flex gap-2">
  <button class="btn btn-primary" :disabled="busy" @click="doRegister">
    {{ busy ? '送出中…' : '註冊' }}
  </button>

  <router-link class="btn btn-outline-secondary" :to="{ name: 'userlogin' }">
    已有帳號？去登入
  </router-link>

  <!-- ✅ 註冊成功後提供兩個友善動作 -->
  <button v-if="!isError && emailVerifyHint" class="btn btn-success" @click="goLogin">
    前往登入
  </button>
  <button v-if="canResend" class="btn btn-outline-secondary" :disabled="resendBusy" @click="resendConfirmEmail">
    {{ resendBusy ? '重寄中…' : '重寄驗證信' }}
  </button>
</div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { http } from '@/api/http'

// 欄位
const email = ref('')
const password = ref('')
const lastName = ref('')
const firstName = ref('')
const phoneNumber = ref('')
const gender = ref('男')
const usedReferralCode = ref('')

// UI 狀態
const busy = ref(false)
const msg = ref('')
const isError = ref(false)
const msgTypeClass = computed(() => (isError.value ? 'alert-danger' : 'alert-success'))
const emailVerifyHint = ref('')       // ✅ 註冊成功後顯示「已寄出驗證信至...」
const canResend = ref(false)          // ✅ 顯示重寄驗證信按鈕
const resendBusy = ref(false)
const resendMsg = ref('')             // ✅ 重寄狀態訊息

const router = useRouter()

function validate() {
  if (!lastName.value) return '請輸入姓氏'
  if (!firstName.value) return '請輸入名字'
  if (!email.value) return '請輸入 Email'
  if (!/^\S+@\S+\.\S+$/.test(email.value)) return 'Email 格式不正確'
  if (!password.value || password.value.length < 8) return '密碼請至少 8 碼'
  if (!phoneNumber.value) return '請輸入手機號碼'
  if (!/^09\d{8}$/.test(phoneNumber.value)) return '手機號碼格式不正確（例：0900000000）'
  if (!gender.value) return '請選擇性別'
  return null
}

async function doRegister() {
  msg.value = ''
  emailVerifyHint.value = ''
  resendMsg.value = ''
  canResend.value = false
  isError.value = false

  const v = validate()
  if (v) {
    msg.value = v
    isError.value = true
    return
  }

  busy.value = true
  try {
    const payload = {
      email: email.value,
      password: password.value,
      lastName: lastName.value,
      firstName: firstName.value,
      phoneNumber: phoneNumber.value,
      gender: gender.value,
      usedReferralCode: usedReferralCode.value,
    }
    await http.post('/auth/register', payload)

    // ✅ 後端已寄出驗證信 → 給使用者明確提示
    msg.value = '註冊成功！我們已寄出驗證信。'
    isError.value = false
    emailVerifyHint.value = `請至 ${email.value} 收信並點擊驗證連結完成啟用。`
    canResend.value = true
  } catch (e) {
    isError.value = true
    msg.value = e?.response?.data?.error || '註冊失敗，請稍後再試'
    console.error('[register failed]', e?.response?.data || e)
  } finally {
    busy.value = false
  }
}

async function resendConfirmEmail() {
  if (!email.value) return
  resendBusy.value = true
  resendMsg.value = ''
  try {
    await http.post('/auth/resend-confirm', { email: email.value })
    resendMsg.value = '已重新寄出驗證信，請稍候並再次查看收件匣／垃圾信件匣。'
  } catch (e) {
    resendMsg.value = e?.response?.data?.message || '重寄失敗，請稍後再試'
  } finally {
    resendBusy.value = false
  }
}

function goLogin() {
  router.replace({ name: 'userlogin', query: { email: email.value } })
}
</script>

<style scoped>
/* 可依設計微調間距 */
.btn-group .btn + .btn {
  margin-left: 0.5rem;
}
</style>
