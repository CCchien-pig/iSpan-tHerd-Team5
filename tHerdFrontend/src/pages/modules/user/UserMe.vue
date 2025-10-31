<template>
  <div class="container py-4">
    <h2 class="mb-3">我的帳戶</h2>
    <div v-if="!me" class="alert alert-warning">尚未登入</div>

    <div v-else class="card p-3">
      <p><b>名稱：</b>{{ me.name }}</p>
      <p><b>Email：</b>{{ me.email }}</p>
      <p><b>會員編號：</b>#{{ me.userNumberId }}</p>
      <p><b>角色：</b>{{ me.roles?.join(', ') }}</p>

      <div class="mt-3">
        <router-link class="btn btn-outline-secondary me-2" :to="{ name: 'home' }">回首頁</router-link>
        <button class="btn btn-danger" @click="doLogout">登出</button>
      </div>
    </div>
  </div>
</template>

<!-- /src/pages/account/Me.vue -->
<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useRouter } from 'vue-router'

const auth = useAuthStore()
const router = useRouter()

const me = computed(() => auth.user)

async function doLogout() {
  await auth.logout()
  router.replace({ name: 'home' })
}
</script>