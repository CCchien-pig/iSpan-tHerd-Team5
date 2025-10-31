<!-- /src/pages/auth/UserLogin.vue -->

<!-- <template>
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

      <router-link class="btn btn-link" :to="{ name: 'userregister' }">沒有帳號？去註冊</router-link>
    </div>
  </div>
</template>

<script setup>
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
  } catch (e) {
    // e 可能是 AxiosError，也可能是一般錯誤；先盡量取回後端訊息
    const msg =
      (e && e.response && e.response.data && (e.response.data.error || e.response.data.message)) ||
      (e && e.message) ||
      '登入失敗，請確認帳號或密碼'
    errMsg.value = msg
  } finally {
    busy.value = false
  }
}
</script>
 -->
<!--模仿iherb架構--> 
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
      <div v-if="errMsg" class="alert alert-danger py-2">{{ errMsg }}</div>

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
            :href="`/api/auth/ExternalLogin?provider=Google&rememberMe=${rememberMe}&redirect=/user/me`"
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

/**
 * 🔐 reCAPTCHA v2 Checkbox 設定
 * - 請在 .env 設定 VITE_RECAPTCHA_V2_SITE_KEY=你的_site_key
 * - 這裡採「顯式渲染」（explicit），用 grecaptcha.render 顯示核取方塊。
 */
const RECAPTCHA_SITE_KEY = document.querySelector('meta[name="recaptcha-site-key"]')?.getAttribute('content') ?? '';
const RECAPTCHA_SRC =
  'https://www.recaptcha.net/recaptcha/api.js?onload=onRecaptchaApiLoaded&render=explicit'

const KEEP_SIGNED_IN_TIP =
  '保持登錄狀態以加快操作。若為共用裝置，請勿勾選此選項。'

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

const recaptchaBox = ref(null)
let recaptchaWidgetId = null
const recaptchaToken = ref('') // 由 v2 核取方塊回傳

const canSubmit = computed(() => {
  return (
    email.value.length > 3 &&
    password.value.length >= 8 &&
    !!recaptchaToken.value && // 必須已通過人機驗證
    !busy.value
  )
})

/** 動態載入 v2 api.js（只載一次） */
function loadRecaptchaV2() {
  return new Promise((resolve, reject) => {
    if (window.grecaptcha && window.grecaptcha.render) return resolve(true)
    if (!RECAPTCHA_SITE_KEY) {
      return reject(new Error('reCAPTCHA v2 site key 未設定（VITE_RECAPTCHA_V2_SITE_KEY）'))
    }

    // 若已存在同 src 的 script，掛上事件即可
    const existed = document.querySelector(`script[src^="${RECAPTCHA_SRC}"]`)
    if (existed) {
      existed.addEventListener('load', () => resolve(true))
      existed.addEventListener('error', reject)
    } else {
      // 先把全域 onload callback 掛上
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

/** 建立 v2 Checkbox 小工具 */
function renderRecaptcha() {
  if (!window.grecaptcha || !recaptchaBox.value || recaptchaWidgetId !== null) return
  recaptchaWidgetId = window.grecaptcha.render(recaptchaBox.value, {
    sitekey: RECAPTCHA_SITE_KEY,
    theme: 'light',
    size: 'normal', // 可改 'compact'
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

/** 失敗或想重來時重置 Checkbox */
function resetRecaptcha() {
  if (window.grecaptcha && recaptchaWidgetId !== null) {
    window.grecaptcha.reset(recaptchaWidgetId)
  }
  recaptchaToken.value = ''
}

function toast(msg) {
  alert(msg)
}

async function doLogin() {
  errMsg.value = ''
  recaptchaErr.value = ''

  if (!recaptchaToken.value) {
    recaptchaErr.value = '請先勾選「我不是機器人」。'
    return
  }

  busy.value = true
  try {
    // 將 recaptchaToken 一併送到後端驗證（v2 驗證端點）
    await auth.login(email.value, password.value, {
      rememberMe: rememberMe.value,
      recaptchaToken: recaptchaToken.value,
      recaptchaVersion: 'v2'
    })

    const back = (route.query.redirect && String(route.query.redirect)) || '/'
    router.replace(back)
  } catch (e) {
    const msg =
      (e && e.response && e.response.data && (e.response.data.error || e.response.data.message)) ||
      (e && e.message) ||
      '登入失敗，請確認帳號或密碼'
    errMsg.value = msg

    // 失敗時重置 reCAPTCHA，避免舊 token 重用
    resetRecaptcha()
  } finally {
    busy.value = false
  }
}

onMounted(async () => {
  try {
    await loadRecaptchaV2()
    renderRecaptcha()
  } catch (e) {
    recaptchaErr.value = e?.message || 'reCAPTCHA 載入失敗'
  }
})

onBeforeUnmount(() => {
  // 清掉全域 onload（避免多次掛上）
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

