<!-- /src/pages/modules/user/Login.vue -->
<template>
  <div class="container py-4">
    <!-- 標題與副標題（iHerb 風格） -->
    <div class="mb-3 text-center">
      <h2 class="mb-1">登入或建立帳戶</h2>
      <p class="text-muted mb-0">
        請輸入您的電子郵件以開始使用。如果您已有帳戶，我們將為您找到。
      </p>
      <a
        id="accessibility-link"
        href="https://www.iherb.com/info/accessibility"
        target="_blank"
        class="small d-inline-block mt-2"
      >
        點擊以閱讀我們的無障礙聲明
      </a>
    </div>

    <div class="card p-3 p-md-4">
      <!-- 帳號（email） -->
      <div class="mb-3">
        <label class="form-label" for="username-input">電子郵件</label>
        <input
          v-model.trim="email"
          id="username-input"
          name="username"
          autocomplete="username"
          type="email"
          class="form-control"
          placeholder="you@example.com"
          :disabled="busy"
        />
        <div class="form-text">請使用有效的電子郵件地址</div>
      </div>

      <!-- 密碼 -->
      <div class="mb-3">
        <label class="form-label" for="password-input">密碼</label>
        <div class="input-group">
          <input
            v-model="password"
            id="password-input"
            :type="showPassword ? 'text' : 'password'"
            class="form-control"
            placeholder="請輸入密碼"
            autocomplete="current-password"
            :disabled="busy"
          />
          <button
            class="btn btn-outline-secondary"
            type="button"
            @click="showPassword = !showPassword"
            :aria-label="showPassword ? '隱藏密碼' : '顯示密碼'"
          >
            <i :class="showPassword ? 'bi bi-eye-slash' : 'bi bi-eye'"></i>
          </button>
        </div>
        <div class="form-text">至少 8 個字元，建議包含大小寫與數字</div>
      </div>

      <!-- 保持登錄狀態 -->
      <div class="mb-3 d-flex align-items-center gap-2">
        <input
          id="keep-signed-in"
          type="checkbox"
          class="form-check-input"
          v-model="rememberMe"
          :disabled="busy"
        />
        <label for="keep-signed-in" class="form-check-label">保持登錄狀態</label>
        <button
          type="button"
          class="btn btn-sm btn-link text-muted ms-1 p-0"
          :aria-label="KEEP_SIGNED_IN_TIP"
          @click="toast(KEEP_SIGNED_IN_TIP)"
        >
          <i class="bi bi-info-circle"></i>
        </button>
      </div>

      <!-- reCAPTCHA v2 Checkbox -->
      <div class="mb-3">
        <label class="form-label d-block">人機驗證</label>
        <div ref="recaptchaBox"></div>
        <div v-if="recaptchaErr" class="text-danger small mt-2">{{ recaptchaErr }}</div>
      </div>

      <!-- 錯誤訊息 -->
      <div v-if="errMsg" class="alert alert-danger py-2">
  {{ errMsg }}
  <div v-if="unlockAtText" class="small text-muted mt-1">{{ unlockAtText }}</div>
</div>

<!-- ✅ 未驗證信箱時的重寄提示 -->
<div v-if="canResend" class="alert alert-info py-2">
  尚未收到驗證信？您可以
  <button class="btn btn-sm btn-outline-secondary ms-1" :disabled="resendBusy" @click="resendConfirmEmail">
    {{ resendBusy ? '重寄中…' : '重新寄送驗證信' }}
  </button>
  <div v-if="resendMsg" class="small text-muted mt-1">{{ resendMsg }}</div>
</div>

      <!-- 登入按鈕 -->
      <div class="d-grid gap-2">
        <button
          id="auth-continue-button"
          class="btn btn-success"
          :disabled="busy || !canSubmit"
          @click="doLogin"
        >
          {{ busy ? '登入中…' : '登入' }}
        </button>

        <router-link class="btn btn-link" :to="{ name: 'userregister' }">
          沒有帳號？去註冊
        </router-link>
      </div>

      <!-- 需要幫助？ -->
      <div class="text-center my-3">
        <i class="bi bi-question-circle me-1"></i>
        <a
          href="https://information.iherb.com/hc/zh-tw/sections/360004028091"
          target="_blank"
          class="link-secondary"
        >需要幫助？</a>
      </div>

      <!-- 分隔線：或 -->
      <div class="position-relative text-center my-3">
        <hr />
        <span class="position-absolute top-50 start-50 translate-middle px-3 bg-white text-muted">或</span>
      </div>

      <!-- 社群登入 -->
      <div class="row g-2">
        <div class="col-12">
          <a
            class="btn w-100 btn-outline-secondary d-flex align-items-center justify-content-center"
            :href="`https://localhost:7103/api/auth/ExternalLogin?provider=Google&rememberMe=${rememberMe}&redirect=/user/me`"
          >
            <i class="bi bi-google me-2"></i> 用google帳號登入
          </a>
        </div>
      </div>

      <!-- 條款與隱私 -->
      <p class="small text-muted mt-3">
        如果繼續操作，則說明您已經閱讀並同意我們的
        <a href="/info/terms-of-use" target="_blank">條款和條件</a> 以及
        <a href="/info/privacy" target="_blank">隱私政策</a>。
      </p>
    </div>

    <!-- 版權（可移到全站 Footer） -->
    <div class="text-center text-muted small mt-4">
      © Copyright 1997-2025 iHerb, LLC. All rights reserved.
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const RECAPTCHA_SITE_KEY = document.querySelector('meta[name="recaptcha-site-key"]')?.getAttribute('content') ?? ''
const RECAPTCHA_SRC = 'https://www.recaptcha.net/recaptcha/api.js?onload=onRecaptchaApiLoaded&render=explicit'
const KEEP_SIGNED_IN_TIP = '保持登錄狀態以加快操作。若為共用裝置，請勿勾選此選項。'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()

const email = ref('')
const password = ref('')
const rememberMe = ref(true)
const showPassword = ref(false)
const busy = ref(false)

const errMsg = ref('')
const recaptchaErr = ref('')

// ✅ 針對 email 未驗證與鎖定情境的 UI 控制
const canResend = ref(false)
const resendBusy = ref(false)
const resendMsg = ref('')
const unlockAtText = ref('') // 顯示鎖定解除時間（本地）

// reCAPTCHA v2
const recaptchaBox = ref(null)
let recaptchaWidgetId = null
const recaptchaToken = ref('')

const canSubmit = computed(() => {
  return (
    email.value.length > 3 &&
    password.value.length >= 8 &&
    !!recaptchaToken.value &&
    !busy.value
  )
})

function loadRecaptchaV2() {
  return new Promise((resolve, reject) => {
    if (window.grecaptcha && window.grecaptcha.render) return resolve(true)
    if (!RECAPTCHA_SITE_KEY) return reject(new Error('reCAPTCHA v2 site key 未設定'))

    const existed = document.querySelector(`script[src^="${RECAPTCHA_SRC}"]`)
    if (existed) {
      existed.addEventListener('load', () => resolve(true))
      existed.addEventListener('error', reject)
    } else {
      window.onRecaptchaApiLoaded = () => resolve(true)
      const s = document.createElement('script')
      s.src = RECAPTCHA_SRC
      s.async = true
      s.defer = true
      s.onerror = () => reject(new Error('reCAPTCHA 載入失敗'))
      document.head.appendChild(s)
    }
  })
}

function renderRecaptcha() {
  if (!window.grecaptcha || !recaptchaBox.value || recaptchaWidgetId !== null) return
  recaptchaWidgetId = window.grecaptcha.render(recaptchaBox.value, {
    sitekey: RECAPTCHA_SITE_KEY,
    theme: 'light',
    size: 'normal',
    callback: (token) => {
      recaptchaToken.value = token
      recaptchaErr.value = ''
    },
    'expired-callback': () => {
      recaptchaToken.value = ''
      recaptchaErr.value = '驗證已過期，請重新勾選「我不是機器人」。'
    },
    'error-callback': () => {
      recaptchaToken.value = ''
      recaptchaErr.value = 'reCAPTCHA 載入或驗證發生錯誤，請重試。'
    }
  })
}

function resetRecaptcha() {
  if (window.grecaptcha && recaptchaWidgetId !== null) window.grecaptcha.reset(recaptchaWidgetId)
  recaptchaToken.value = ''
}

function toast(msg) { alert(msg) }

// ✅ 重寄驗證信
async function resendConfirmEmail() {
  if (!email.value) return
  resendBusy.value = true
  resendMsg.value = ''
  try {
    // 對應後端 /api/auth/resend-confirm
    await fetch('/api/auth/resend-confirm', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: email.value })
    })
    resendMsg.value = '已重新寄出驗證信，請稍候並再次查看收件匣／垃圾信件匣。'
  } catch (e) {
    resendMsg.value = '重寄失敗，請稍後再試'
  } finally {
    resendBusy.value = false
  }
}

function setFriendlyError(e) {
  const payload = e?.response?.data || {}
  const code = payload.error_code
  const message = payload.message || payload.error

  // 預設訊息
  errMsg.value = message || '登入失敗，請確認帳號或密碼'
  canResend.value = false
  unlockAtText.value = ''

  switch (code) {
    case 'email_unconfirmed':
      errMsg.value = '請先完成信箱驗證。'
      canResend.value = true
      break
    case 'account_locked':
      errMsg.value = '帳號已被鎖定，請稍後再試。'
      if (payload.unlockAt) {
        // 轉成本地時間顯示
        const t = new Date(payload.unlockAt)
        unlockAtText.value = `預計解除時間：${t.toLocaleString()}`
      }
      break
    case 'bad_credentials':
      if (typeof payload.remainingAttempts === 'number') {
        errMsg.value = `帳號或密碼錯誤（剩餘嘗試 ${payload.remainingAttempts} 次）。`
      } else {
        errMsg.value = '帳號或密碼錯誤'
      }
      break
    case 'recaptcha_failed':
      errMsg.value = 'reCAPTCHA 驗證失敗，請重試。'
      break
    default:
      // 沒帶 error_code，保留後端訊息或預設
      break
  }
}

async function doLogin() {
  errMsg.value = ''
  recaptchaErr.value = ''
  canResend.value = false
  resendMsg.value = ''
  unlockAtText.value = ''

  if (!recaptchaToken.value) {
    recaptchaErr.value = '請先勾選「我不是機器人」。'
    return
  }

  busy.value = true
  try {
    await auth.login(email.value, password.value, {
      rememberMe: rememberMe.value,
      recaptchaToken: recaptchaToken.value,
      recaptchaVersion: 'v2'
    })
    const back = (route.query.redirect && String(route.query.redirect)) || '/'
    router.replace(back)
  } catch (e) {
    setFriendlyError(e)
    resetRecaptcha()
  } finally {
    busy.value = false
  }
}

onMounted(async () => {
  // 若上個頁面傳來 email（例如註冊後導到登入頁）
  const preset = route.query.email && String(route.query.email)
  if (preset) email.value = preset

  try {
    await loadRecaptchaV2()
    renderRecaptcha()
  } catch (e) {
    recaptchaErr.value = e?.message || 'reCAPTCHA 載入失敗'
  }
})

onBeforeUnmount(() => {
  if (window.onRecaptchaApiLoaded) {
    try { delete window.onRecaptchaApiLoaded } catch {}
  }
})
</script>



<style scoped>
.container {
  max-width: 540px;
}
#accessibility-link {
  text-decoration: underline;
}
#auth-continue-button {
  background-color: #458500;
  border-color: #458500;
}
#auth-continue-button:disabled {
  opacity: 0.7;
}
.position-relative hr {
  margin: 1.25rem 0;
}
</style>

