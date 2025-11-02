<!-- /src/pages/modules/user/UserTwoStep.vue -->
<template>
  <div class="container py-4" style="max-width:720px">
    <h2 class="mb-2">兩步驗證</h2>
    <p class="text-muted">使用驗證器 App（Google Authenticator / 1Password / Microsoft Authenticator 等）產生 6 位數驗證碼。</p>

    <div class="card p-3 mb-3">
      <h5 class="mb-2">步驟 1：設定驗證器</h5>
      <div v-if="busySetup" class="text-muted">載入中…</div>
      <div v-else>
        <!-- ★ 新增：QR Code 圖片 -->
        <div v-if="qrDataUrl" class="mb-2">
          <img :src="qrDataUrl" alt="Authenticator QR" style="width:220px;height:220px;border:1px solid #eee;border-radius:8px;padding:6px;background:#fff" />
        </div>
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
import { onMounted, ref,nextTick  } from 'vue'
import { http } from '@/api/http'
import QRCode from 'qrcode'

const busySetup = ref(true)
const busyEnable = ref(false)
const busyDisable = ref(false)
const sharedKey = ref('')
const authenticatorUri = ref('')
const issuerHint = 'tHerd' // 與後端 issuer 保持一致
const qrDataUrl = ref('') 
const code = ref('')
const msg = ref('')
const isErr = ref(false)
const recoveryCodes = ref([])

async function genQr () {          // ★ 新增：把 otpauth URI 轉成 QR data URL
  if (!authenticatorUri.value) {
    qrDataUrl.value = ''
    return
  }
  // 等 DOM 稍微穩定（非必要，但較穩），再產圖
  await nextTick()
  qrDataUrl.value = await QRCode.toDataURL(authenticatorUri.value, {
    width: 220,    // 你可以調整
    margin: 1,
    errorCorrectionLevel: 'M'
  })
}

async function refreshMeDetailBadge() {
  try {
    const { data } = await http.get('/user/me/detail')
    // 若你想即時同步到側欄，可以透過全域 store 或 event bus 告知；
    // 簡化作法：當前頁面顯示提示即可，側欄下次進頁或刷新會自己抓。
  } catch {}
}

async function loadSetup() {
  busySetup.value = true
  try {
    const { data } = await http.get('/auth/2fa/setup')
    sharedKey.value = data.sharedKey || ''
    authenticatorUri.value = data.authenticatorUri || ''
    await genQr() //★ 新增：載到 otpauth 後立刻產 QR
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
    await refreshMeDetailBadge() // 可選
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
    await loadSetup()
    await refreshMeDetailBadge() // 可選
  } catch (e) {
    msg.value = e?.response?.data?.error || '停用失敗，請稍後再試'
    isErr.value = true
  } finally {
    busyDisable.value = false
  }
}

onMounted(loadSetup)
</script>
