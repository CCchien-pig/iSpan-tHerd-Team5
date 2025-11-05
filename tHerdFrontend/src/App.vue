<script setup lang="ts">
import { RouterView } from 'vue-router'

// [新增] 匯入 Notiwind 元件
import { NotificationGroup, Notification } from 'notiwind'
</script>

<template>
  <RouterView />

  <!-- 提示樣式 -->
  <!-- Notiwind 接收器 -->
  <NotificationGroup group="bottom-center">
    <div
      class="fixed inset-x-0 bottom-0 z-50 flex items-start justify-center p-4 pointer-events-none"
    >
      <div class="w-full max-w-sm">
        <Notification
          v-slot="{ notifications }"
          enter="transform ease-out duration-300 transition"
          enter-from="translate-y-2 opacity-0 sm:translate-y-0 sm:translate-x-2"
          enter-to="translate-y-0 opacity-100 sm:translate-x-0"
          leave="transition ease-in duration-100"
          leave-from="opacity-100"
          leave-to="opacity-0"
          move="transition duration-500"
          move-delay="delay-300"
        >
          <div
            v-for="notification in notifications"
            :key="notification.id"
            class="flex w-full max-w-sm mx-auto mt-4 overflow-hidden bg-white rounded-lg shadow-md pointer-events-auto ring-1 ring-black ring-opacity-5"
          >
            <!-- 左側圖示區塊，背景色依通知類型變化 -->
            <div
              class="flex items-center justify-center w-12 rounded-l-lg"
              :class="{
                'bg-main-color-green': notification.type === 'success',
                'bg-yellow-400': notification.type === 'error',
                'bg-blue-400': notification.type === 'info',
              }"
            >
              <template v-if="notification.type === 'success'">
                <svg
                  class="w-6 h-6 text-white"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M5 13l4 4L19 7"
                  />
                </svg>
              </template>
              <template v-else-if="notification.type === 'error'">
                <svg
                  class="w-6 h-6 text-white"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M10.29 3.86L1.82 18a1 1 0 00.86 1.5h18.64a1 1 0 00.86-1.5L13.71 3.86a1 1 0 00-1.42 0zM12 9v4m0 4h.01"
                  />
                </svg>
              </template>
              <template v-else>
                <svg
                  class="w-6 h-6 text-white"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                  />
                </svg>
              </template>
            </div>

            <!-- 通知內容區 -->
            <div class="px-4 py-3 -mx-3">
              <div class="mx-3">
                <span
                  class="font-semibold"
                  :class="{
                    'main-color-green-text': notification.type === 'success',
                    'text-yellow-500': notification.type === 'error',
                    'text-blue-500': notification.type === 'info',
                  }"
                >
                  {{
                    notification.type === 'success'
                      ? '成功'
                      : notification.type === 'error'
                        ? 'Oops'
                        : '提示'
                  }}
                </span>
                <p class="text-sm text-gray-600">
                  {{ notification.text }}
                </p>
              </div>
            </div>
          </div>
        </Notification>
      </div>
    </div>
  </NotificationGroup>
</template>

<style>
.main-color-green-bg {
  background-color: rgb(0, 112, 131) !important;
}

.main-color-white-text {
  color: rgb(248, 249, 250) !important;
}

.main-color-green-text {
  color: rgb(0, 112, 131) !important;
}
</style>
