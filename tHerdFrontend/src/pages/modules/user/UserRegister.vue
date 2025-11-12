<template>
  <div class="register-page">
    <!-- 頁首 Hero -->
    <header class="hero">
      <div class="hero-inner">
        <h1 class="hero-title">建立帳戶</h1>
        <p class="hero-subtitle">
          輕鬆管理訂單、快速結帳、取得專屬優惠。已經有帳戶了嗎？
          <router-link :to="{ name: 'userlogin' }">前往登入</router-link>
        </p>
      </div>
    </header>

    <div class="container">
      <div class="grid">
        <!-- 左：註冊表單 -->
        <section class="card form-card">
          <!-- 表單主標題 -->
          <h2 class="form-title">註冊帳戶</h2>
          <p class="form-subtitle">請填寫以下資料以建立您的帳戶。</p>

          <!-- 「或」分隔（保留空間，未接社群登入也可用） -->
          <div class="or-sep" aria-hidden="true">
            <span class="line"></span>
            <!-- <span class="label">或</span>
            <span class="line"></span> -->
          </div>

          <!-- 表單欄位（保留你所有 inputs 與 v-model） -->
          <div class="form-body">
            <!-- 姓名 -->
            <div class="row row-2">
              <div class="field">
                <!-- <label class="label">姓氏 <span class="req">*</span></label> -->
                <label class="label">姓氏 <span class="req">*</span></label>
                <input v-model.trim="lastName" class="input"
                @input="touch('lastName')" @blur="touch('lastName')"
                :class="{ 'is-invalid': touched.lastName && lastNameError }" />
                <div class="field-error" v-if="touched.lastName && lastNameError">{{ lastNameError }}</div>
              </div>
              <div class="field">
                <label class="label">名字 <span class="req">*</span></label>
                <input v-model.trim="firstName" class="input"
                 @input="touch('firstName')" @blur="touch('firstName')"
                  :class="{ 'is-invalid': touched.firstName && firstNameError }" />
                  <div class="field-error" v-if="touched.firstName && firstNameError">{{ firstNameError }}</div>
              </div>
            </div>

            <!-- Email -->
            <div class="row">
              <div class="field">
                <label class="label">Email <span class="req">*</span></label>
                <input v-model.trim="email" type="email" class="input" placeholder="you@example.com"
               @input="touch('email')" @blur="touch('email')"
                :class="{ 'is-invalid': touched.email && emailError }" />
                <div class="field-error" v-if="touched.email && emailError">{{ emailError }}</div>
              </div>
            </div>

            <!-- 密碼 -->
            <div class="row">
              <div class="field">
                <label class="label">密碼 <span class="req">*</span></label>
<input v-model="password" type="password" class="input"
       placeholder="至少 8 碼，需含大小寫、數字、符號"
       autocomplete="new-password"
       @input="touch('password')" @blur="touch('password')"
       :class="{ 'is-invalid': touched.password && passwordError }" />
<div class="field-error" v-if="touched.password && passwordError">{{ passwordError }}</div>
<!-- 可選：即時規則清單（打勾/打叉） -->
<ul class="pw-hints">
  <li :class="{ ok: password.length >= 8 }">至少 8 碼</li>
  <li :class="{ ok: /[A-Z]/.test(password) }">包含大寫</li>
  <li :class="{ ok: /[a-z]/.test(password) }">包含小寫</li>
  <li :class="{ ok: /\d/.test(password) }">包含數字</li>
  <li :class="{ ok: /[^A-Za-z0-9]/.test(password) }">包含符號</li>
</ul>
              </div>
            </div>

            <!-- 手機 + 推薦碼 -->
            <div class="row row-2">
              <div class="field">
                <label class="label">手機號碼 <span class="req">*</span></label>
<input type="text" 
       pattern="[0-9]*" 
       maxlength="10" v-model.trim="phoneNumber" class="input" placeholder="0900000000" inputmode="numeric"
       @input="touch('phoneNumber')" @blur="touch('phoneNumber')"
       :class="{ 'is-invalid': touched.phoneNumber && phoneError }" />
<div class="field-error" v-if="touched.phoneNumber && phoneError">{{ phoneError }}</div>

              </div>
              <div class="field">
                <label class="label">使用推薦碼</label>
                <input v-model.trim="usedReferralCode" class="input" />
              </div>
            </div>

            <!-- 性別 -->
            <div class="row">
              <div class="field">
                <label class="label d-block">性別 <span class="req">*</span></label>
                <div class="segmented">
  <input type="radio" class="segmented-input" name="gender" id="gender-male" value="男"
         v-model="gender" autocomplete="off" @change="touch('gender')" />
  <label class="segmented-btn" for="gender-male">男</label>

  <input type="radio" class="segmented-input" name="gender" id="gender-female" value="女"
         v-model="gender" autocomplete="off" @change="touch('gender')" />
  <label class="segmented-btn" for="gender-female">女</label>
</div>
<div class="field-error" v-if="touched.gender && genderError">{{ genderError }}</div>
              </div>
            </div>

            <!-- 訊息區（完全保留你的顯示邏輯） -->
            <div v-if="msg" :class="['alert', isError ? 'alert-danger' : 'alert-success']">
              {{ msg }}
              <template v-if="!isError && emailVerifyHint">
                <br />
                <small class="text-muted">{{ emailVerifyHint }}</small>
              </template>
            </div>
            <div v-if="resendMsg" class="alert alert-info">{{ resendMsg }}</div>

            <!-- 動作區：主註冊 + 前往登入 + 重寄驗證信 -->
            <div class="actions">
              <button class="btn btn-primary" :disabled="busy || !formValid" @click="doRegister" @event="doRegister">{{ busy ? '送出中…' : '註冊' }}</button>

              <router-link class="btn btn-ghost" :to="{ name: 'userlogin' }">
                已有帳號？去登入
              </router-link>

              <button
                v-if="!isError && emailVerifyHint"
                class="btn btn-success"
                @click="goLogin"
              >
                前往登入
              </button>

              <button
                v-if="canResend"
                class="btn btn-outline"
                :disabled="resendBusy"
                @click="resendConfirmEmail"
              >
                {{ resendBusy ? '重寄中…' : '重寄驗證信' }}
              </button>
            </div>

            <!-- 無障礙與聲明（iHerb 常見資訊區） -->
            <div class="footnote">
              點擊「註冊」即表示您同意本網站之
              <a href="#" @click.prevent>服務條款</a>與
              <a href="#" @click.prevent>隱私權政策</a>。
              <a href="#" id="accessibility-link" @click.prevent>無障礙說明</a>
            </div>
          </div>
        </section>

        <!-- 右：品牌/安心購買/為什麼選擇我們（靜態資訊） -->
        <aside class="card promo-card">
          <h3 class="promo-title">為什麼選擇我們</h3>
          <ul class="promo-list">
            <li>嚴選正貨｜廠商直送</li>
            <li>會員專屬｜生日禮金</li>
            <li>快速結帳｜多元支付</li>
            <li>訂單追蹤｜即時客服</li>
          </ul>
          <!-- 一鍵註冊 CTA -->
<div class="promo-cta">
  <button
    class="btn btn-primary w-100"
    :disabled="busy"
    @click="quickRegister"
  >
    一鍵註冊
  </button>
</div>
          <div class="promo-badge">
            <div class="stars" aria-label="5 星評價">★★★★★</div>
            <div class="badge-text">超過 100,000 則好評</div>
          </div>
        </aside>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed,nextTick  } from 'vue'
import { useRouter } from 'vue-router'
import { http } from '@/api/http'

/** ====== 你的原本狀態（完整保留） ====== */
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
const emailVerifyHint = ref('')
const canResend = ref(false)
const resendBusy = ref(false)
const resendMsg = ref('')
// ===== 即時驗證狀態（純 JS） =====
const touched = ref({
  lastName: false,
  firstName: false,
  email: false,
  password: false,
  phoneNumber: false,
  gender: false,
})

const emailRegex = /^\S+@\S+\.\S+$/
const phoneRegex = /^09\d{8}$/

const hasUpper = (s) => /[A-Z]/.test(s)
const hasLower = (s) => /[a-z]/.test(s)
const hasDigit = (s) => /\d/.test(s)
const hasSymbol = (s) => /[^A-Za-z0-9]/.test(s)

const lastNameError = computed(() => (!lastName.value ? '請輸入姓氏' : ''))
const firstNameError = computed(() => (!firstName.value ? '請輸入名字' : ''))
const emailError = computed(() => {
  if (!email.value) return '請輸入 Email'
  if (!emailRegex.test(email.value)) return 'Email 格式不正確'
  return ''
})
const passwordError = computed(() => {
  const p = password.value ?? ''
  if (p.length < 8) return '密碼至少 8 碼'
  if (!hasUpper(p)) return '需包含英文字母大寫'
  if (!hasLower(p)) return '需包含英文字母小寫'
  if (!hasDigit(p)) return '需包含數字'
  if (!hasSymbol(p)) return '需包含符號（如 !@#$%）'
  return ''
})
const phoneError = computed(() => {
  if (!phoneNumber.value) return '請輸入手機號碼'
  if (!phoneRegex.test(phoneNumber.value)) return '手機號碼格式不正確（例：0900000000）'
  return ''
})
const genderError = computed(() => (!gender.value ? '請選擇性別' : ''))

const formValid = computed(() =>
  !lastNameError.value &&
  !firstNameError.value &&
  !emailError.value &&
  !passwordError.value &&
  !phoneError.value &&
  !genderError.value
)


const router = useRouter()

// function validate() {
//   if (!lastName.value) return '請輸入姓氏'
//   if (!firstName.value) return '請輸入名字'
//   if (!email.value) return '請輸入 Email'
//   if (!/^\S+@\S+\.\S+$/.test(email.value)) return 'Email 格式不正確'
//   if (!password.value || password.value.length < 8) return '密碼請至少 8 碼'
//   if (!phoneNumber.value) return '請輸入手機號碼'
//   if (!/^09\d{8}$/.test(phoneNumber.value)) return '手機號碼格式不正確（例：0900000000）'
//   if (!gender.value) return '請選擇性別'
//   return null
// }

// 小工具：標記 touched（純 JS 字串參數）
function touch(field) {
  if (field && Object.prototype.hasOwnProperty.call(touched.value, field)) {
    touched.value[field] = true
  }
}

// 送出前最後檢核（用 formValid 取代原本 validate）
async function doRegister() {
  msg.value = ''
  emailVerifyHint.value = ''
  resendMsg.value = ''
  canResend.value = false
  isError.value = false

  // 全部設為 touched 以顯示所有錯誤
  Object.keys(touched.value).forEach(k => {
    touched.value[k] = true
  })

  if (!formValid.value) {
    isError.value = true
    msg.value = '請修正表單中的錯誤後再送出'
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

// ✅ 自動填入你指定的註冊資料
function quickFillRegister() {
  lastName.value = '簡'
  firstName.value = '郡逸'
  email.value = 'therd9513@gmail.com'
  password.value = 'iSpan0919~'
  phoneNumber.value = '0900000007'
  usedReferralCode.value = 'REF-C68D52B4'
  gender.value = gender.value || '男' // 若沒選就維持預設男

  // 讓所有欄位呈現 touched 狀態，立即顯示驗證結果
  Object.keys(touched.value).forEach(k => (touched.value[k] = true))
}

// ✅ 一鍵註冊：先填、再送出（沿用你的 doRegister 驗證與 API）
async function quickRegister() {
  quickFillRegister()
  await nextTick() // 等待 v-model 同步、computed 驗證更新
  // await doRegister()
}
</script>

<style scoped>
/* ====== 佈局 ====== */
.register-page { background: #fafafa; min-height: 100vh; }
.container { max-width: 1100px; margin: 0 auto; padding: 16px; }
.grid {
  display: grid;
  grid-template-columns: 1.3fr 0.7fr;
  gap: 20px;
}
@media (max-width: 992px) {
  .grid { grid-template-columns: 1fr; }
}

/* ====== Hero ====== */
.hero {
  background: linear-gradient(180deg, #f0f7f4, #ffffff 60%);
  border-bottom: 1px solid #eef2f3;
}
.hero-inner { max-width: 1100px; margin: 0 auto; padding: 28px 16px 8px; }
.hero-title { font-size: 28px; font-weight: 800; margin: 0 0 4px; color: #1f2937; }
.hero-subtitle { color: #6b7280; margin: 0; }
.hero-subtitle a { color: #0d9488; text-decoration: none; }
.hero-subtitle a:hover { text-decoration: underline; }

/* ====== 卡片 ====== */
.card {
  background: #fff; border: 1px solid #e5e7eb; border-radius: 16px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.03);
}
.form-card { padding: 18px 18px 20px; }
.promo-card { padding: 18px; }

/* ====== 表單 ====== */
.form-title { font-size: 20px; font-weight: 700; margin: 4px 0 2px; color: #111827; }
.form-subtitle { color: #6b7280; margin-bottom: 12px; }

.or-sep { display: flex; align-items: center; gap: 10px; margin: 12px 0 8px; }
.or-sep .line { flex: 1; height: 1px; background: #e5e7eb; }
.or-sep .label { font-size: 12px; color: #6b7280; }

.form-body { margin-top: 8px; }

.row { display: grid; gap: 12px; margin-bottom: 14px; }
.row-2 { grid-template-columns: 1fr 1fr; }
@media (max-width: 640px) { .row-2 { grid-template-columns: 1fr; } }

.field {}
.label { display: inline-block; font-weight: 700; margin-bottom: 6px; color: #374151; }
.req { color: #ef4444; margin-left: 2px; }

.input {
  width: 100%; border: 1px solid #d1d5db; border-radius: 10px;
  padding: 10px 12px; font-size: 14px; transition: border-color .12s, box-shadow .12s;
}
.input:focus {
  outline: none; border-color: #0ea5e9; box-shadow: 0 0 0 3px rgba(14,165,233,.15);
}

/* 分段按鈕（性別） */
.segmented { display: inline-flex; background: #f3f4f6; border-radius: 12px; padding: 4px; gap: 4px; }
.segmented-input { position: absolute; opacity: 0; pointer-events: none; }
.segmented-btn {
  padding: 8px 14px; border-radius: 10px; background: transparent; border: 1px solid transparent;
  cursor: pointer; user-select: none; font-size: 14px; color: #374151;
}
.segmented-input:checked + .segmented-btn {
  background: #111827; color: #fff; border-color: #111827;
}
.segmented-btn:hover { background: #e5e7eb; }

/* 訊息 */
.alert {
  border-radius: 10px; padding: 10px 12px; font-size: 14px; margin-top: 4px;
}
.alert-success { background: #ecfdf5; color: #065f46; border: 1px solid #a7f3d0; }
.alert-danger  { background: #fef2f2; color: #991b1b; border: 1px solid #fecaca; }
.alert-info    { background: #eff6ff; color: #1e3a8a; border: 1px solid #bfdbfe; }

/* 動作 */
.actions { display: flex; flex-wrap: wrap; gap: 10px; margin-top: 6px; }
.btn {
  border-radius: 999px; padding: 10px 16px; font-weight: 700; font-size: 14px;
  border: 1px solid transparent; cursor: pointer; transition: transform .08s ease, box-shadow .12s;
}
.btn:active { transform: translateY(1px); }
.btn-primary { background: #0ea5e9; color: #fff; }
.btn-primary:disabled { opacity: .7; cursor: not-allowed; }
.btn-success { background: #10b981; color: #fff; }
.btn-outline { background: #fff; color: #374151; border-color: #d1d5db; }
.btn-ghost { background: transparent; color: #374151; border-color: #e5e7eb; }
.btn:hover { box-shadow: 0 4px 12px rgba(0,0,0,.08); }

/* footnote */
.footnote {
  margin-top: 10px; color: #6b7280; font-size: 12px;
}
.footnote a { color: #0d9488; text-decoration: none; }
.footnote a:hover { text-decoration: underline; }

/* 右側推廣 */
.promo-title { font-size: 18px; font-weight: 800; margin: 0 0 8px; color: #111827; }
.promo-list { margin: 12px 0 14px 18px; color: #374151; }
.promo-badge { background: #f9fafb; border: 1px solid #e5e7eb; border-radius: 12px; padding: 14px; text-align: center; }
.stars { letter-spacing: 2px; font-size: 18px; color: #f59e0b; }
.badge-text { font-size: 12px; color: #6b7280; margin-top: 6px; }

.is-invalid {
  border-color: #ef4444 !important;
  box-shadow: 0 0 0 3px rgba(239, 68, 68, .12);
}
.field-error {
  color: #b91c1c;
  font-size: 12px;
  margin-top: 6px;
}
.pw-hints {
  list-style: none;
  padding: 6px 0 0;
  margin: 0;
  font-size: 12px;
  color: #6b7280;
}
.pw-hints li { margin: 2px 0; }
.pw-hints li.ok { color: #065f46; } /* 達成規則顯示綠色 */
</style>
