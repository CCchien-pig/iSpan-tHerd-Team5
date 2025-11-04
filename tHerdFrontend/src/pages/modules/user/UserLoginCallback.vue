<!-- /src/pages/modules/user/UserLoginCallback.vue -->
<template>
  <div class="container py-4">
    <div class="card p-3 text-center">
      <h4>正在處理登入…</h4>
      <p class="text-muted">請稍候</p>
    </div>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

function safeRedirect(raw) {
  try {
    const decoded = decodeURIComponent(raw || '/')
    // 只允許相對路徑，或同網域（必要時可放寬白名單）
    if (decoded.startsWith('/')) return decoded
    return '/'
  } catch { return '/' }
}

onMounted(async () => {
  const token     = route.query.token && String(route.query.token)
  const refresh   = route.query.refresh && String(route.query.refresh)
  const exp       = route.query.exp && String(route.query.exp)        // ISO 8601
  const remember  = route.query.rememberMe === '1'
  const redirect  = safeRedirect((route.query.redirect && String(route.query.redirect)) || '/')

  if (!token) {
    router.replace({ name: 'userlogin' })
    return
  }

  try {
    await auth.loginWithExternalToken(token, {
      rememberMe: remember,
      refreshToken: refresh || null,
      accessTokenExpiresAt: exp || null
    })
    // 清除 query 再前往目的地，避免 token 留在歷史紀錄
    router.replace({ path: redirect })
  } catch (e) {
    console.error(e)
    router.replace({ name: 'userlogin' })
  }
})
</script>
