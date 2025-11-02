<!-- /src/pages/modules/user/UserTwoStep.vue -->
<template>
  <div class="container py-4" style="max-width:720px">
    <h2 class="mb-2">兩步驗證</h2>
    <p class="text-muted">使用驗證器 App（Google Authenticator / 1Password / Microsoft Authenticator 等）產生 6 位數驗證碼。</p>

    <div class="card p-3 mb-3">
      <h5 class="mb-2">步驟 1：設定驗證器</h5>
      <div v-if="busySetup" class="text-muted">載入中…</div>
      <div v-else>
        <div class="mb-2">
          <div class="small text-muted">金鑰（如無法掃 QR 可手動輸入）：</div>
          <code class="fs-5">{{ sharedKey || '—' }}</code>
        </div>
        <div class="mb-2" v-if="authenticatorUri">
          <a :href="authenticatorUri" target="_blank">以支援的驗證器開啟（otpauth URI）</a>
        </div>
        <div class="small text-muted">Issuer：{{ issuerHint }}</div>
      </div>
    </div>

    <div class="card p-3 mb-3">
      <h5 class="mb-2">步驟 2：輸入 6 位數驗證碼以啟用</h5>
      <div class="d-flex gap-2">
        <input class="form-control" v-model.trim="code" maxlength="6" placeholder="000000" style="max-width:160px" />
        <button class="btn btn-primary" :disabled="busyEnable || code.length !== 6" @click="enable2fa">
          {{ busyEnable ? '驗證中…' : '啟用 2FA' }}
        </button>
        <button class="btn btn-outline-danger ms-auto" :disabled="busyDisable" @click="disable2fa">停用 2FA</button>
      </div>
      <div v-if="msg" :class="['alert', isErr ? 'alert-danger' : 'alert-success', 'mt-3', 'py-2']">{{ msg }}</div>
    </div>

    <div class="card p-3" v-if="recoveryCodes.length">
      <h5>復原碼（請妥善保存）</h5>
      <p class="text-muted">當無法使用驗證器時，可用任一組復原碼通過驗證（每組只能用一次）。</p>
      <ul class="mb-0">
        <li v-for="c in recoveryCodes" :key="c"><code>{{ c }}</code></li>
      </ul>
    </div>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { http } from '@/api/http'

const busySetup = ref(true)
const busyEnable = ref(false)
const busyDisable = ref(false)
const sharedKey = ref('')
const authenticatorUri = ref('')
const issuerHint = 'tHerd' // 與後端 issuer 保持一致
const code = ref('')
const msg = ref('')
const isErr = ref(false)
const recoveryCodes = ref([])

async function loadSetup() {
  busySetup.value = true
  try {
    const { data } = await http.get('/auth/2fa/setup')
    sharedKey.value = data.sharedKey || ''
    authenticatorUri.value = data.authenticatorUri || ''
  } catch (e) {
    msg.value = '讀取設定資料失敗，請稍後再試'
    isErr.value = true
  } finally {
    busySetup.value = false
  }
}
async function enable2fa() {
  msg.value = ''
  isErr.value = false
  recoveryCodes.value = []
  busyEnable.value = true
  try {
    const { data } = await http.post('/auth/2fa/enable', { code: code.value })
    recoveryCodes.value = data.recoveryCodes || []
    msg.value = '已成功啟用 2FA！請妥善保存復原碼。'
  } catch (e) {
    msg.value = e?.response?.data?.error || '啟用失敗，請確認驗證碼或稍後再試'
    isErr.value = true
  } finally {
    busyEnable.value = false
  }
}
async function disable2fa() {
  msg.value = ''
  isErr.value = false
  busyDisable.value = true
  try {
    await http.post('/auth/2fa/disable')
    msg.value = '已停用 2FA。'
    // 重新取一次 setup，方便重新啟用
    await loadSetup()
  } catch (e) {
    msg.value = e?.response?.data?.error || '停用失敗，請稍後再試'
    isErr.value = true
  } finally {
    busyDisable.value = false
  }
}

onMounted(loadSetup)
</script>
