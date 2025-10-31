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

onMounted(async () => {
  const token = route.query.token && String(route.query.token)
  const refresh = route.query.refresh && String(route.query.refresh)     // ★ 新增
  const exp = route.query.exp && String(route.query.exp)                 // ★ 新增 (ISO8601)
  const redirect = (route.query.redirect && String(route.query.redirect)) || '/'
  const rememberMe = route.query.rememberMe === '1'

  if (!token) {
    router.replace({ name: 'userlogin' })
    return
  }

  try {
    // 讓你的 auth store 把 token 存起來（localStorage / cookie），並抓 user profile
    await auth.loginWithExternalToken(token, { rememberMe,refreshToken: refresh || null, accessTokenExpiresAt: exp || null  })
    router.replace(redirect)
  } catch (e) {
    console.error(e)
    router.replace({ name: 'userlogin' })
  }
})
</script>
