<template>
  <div class="banner-wrapper">
    <!-- 🖼️ Banner -->
    <img
      v-if="url"
      :src="url"
      :alt="alt"
      class="brand-banner"
      :class="{ clickable: hasLink }"
      @click="handleClick"
      @mouseenter="showTip = hasLink"
      @mouseleave="showTip = false"
    />

    <!-- 🔗 外部連結圖示 -->
    <div v-if="hasLink && isExternal" class="link-indicator">↗</div>

    <!-- 💬 hover 提示框 -->
    <div v-if="showTip && hasLink" class="hover-tip" :style="tipStyle">
      » 點擊可前往{{ isExternal ? '外部網站' : '本站頁面' }}
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import Swal from 'sweetalert2'

// === Props ===
const props = defineProps({
  url: String,
  alt: String,
  linkUrl: String,
  mainColor: {
    // ⬅️ 新增：傳入品牌主色
    type: Object,
    default: () => ({ r: 0, g: 147, b: 171 }),
  },
})

// === 狀態 ===
const showTip = ref(false)
const hasLink = computed(() => !!props.linkUrl)
const isExternal = computed(() => {
  if (!props.linkUrl) return false
  const origin = window.location.origin
  return !props.linkUrl.startsWith(origin)
})

// === 動態 Tooltip 顏色 ===
function getLuma({ r, g, b }) {
  return 0.2126 * r + 0.7152 * g + 0.0722 * b
}

const tipStyle = computed(() => {
  const { r, g, b } = props.mainColor
  const luma = getLuma({ r, g, b })
  const isLight = luma > 180
  return {
    backgroundColor: isLight
      ? 'rgba(50, 50, 50, 0.85)' // 深灰
      : `rgba(${r}, ${g}, ${b}, 0.9)`,
    color: '#fff',
  }
})

// === 點擊事件 ===
function handleClick() {
  console.log('🟢 [BrandBanner] Clicked, linkUrl =', props.linkUrl)
  if (!props.linkUrl) return

  if (isExternal.value) {
    Swal.fire({
      title: `<span style="color:#007083;font-weight:700;">您正要離開 tHerd</span>`,
      html: `
        <div style="text-align:left;line-height:1.6;">
          <p style="margin-bottom:8px;">您即將前往外部網站：</p>
          <p>
            <a href="${props.linkUrl}" target="_blank" rel="noopener"
              style="color:#007083;text-decoration:underline;font-weight:600;">
              ${props.linkUrl}
            </a>
          </p>
          <p style="margin-top:10px;font-size:0.9rem;color:#444;">
            外部網站的隱私政策與安全性可能不同於 tHerd。<br />
            若您確認前往，將在新分頁開啟連結。
          </p>
        </div>
      `,
      icon: 'info',
      showCancelButton: true,
      confirmButtonText: '前往外部網站',
      cancelButtonText: '取消',
      confirmButtonColor: '#007083',
      cancelButtonColor: '#adb5bd',
      background: '#fff',
      color: '#333',
      reverseButtons: true,
    }).then((result) => {
      if (result.isConfirmed) window.open(props.linkUrl, '_blank')
    })
  } else {
    window.location.href = props.linkUrl
  }
}
</script>

<style scoped>
.banner-wrapper {
  position: relative;
  display: flex;
  justify-content: center;
  align-items: center;
  max-height: 350px;
  overflow: hidden;
  border-radius: 0.25rem;
  background-color: #fff;
}

/* 無連結狀態 */
.brand-banner {
  width: 100%;
  height: auto;
  object-fit: cover;
  object-position: center;
  border-radius: 0.25rem;
  transition:
    transform 0.3s ease,
    box-shadow 0.3s ease;
  cursor: default;
}

/* 有連結狀態 */
.brand-banner.clickable {
  cursor: pointer;
}
.brand-banner.clickable:hover {
  transform: scale(1.02);
  box-shadow: 0 4px 16px rgba(0, 112, 131, 0.25);
}

/* 外部連結 ↗ 圖示 */
.link-indicator {
  position: absolute;
  top: 10px;
  right: 12px;
  background-color: rgba(255, 255, 255, 0.9);
  color: #007083;
  font-size: 1rem;
  font-weight: 600;
  border-radius: 50%;
  width: 24px;
  height: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.15);
  pointer-events: none;
}

/* Tooltip */
.hover-tip {
  position: absolute;
  bottom: 12px;
  right: 16px;
  padding: 4px 10px;
  border-radius: 6px;
  font-size: 0.85rem;
  pointer-events: none;
  opacity: 0;
  animation: fadeIn 0.2s ease forwards;
  white-space: nowrap;
  box-shadow: 0 3px 10px rgba(0, 0, 0, 0.2);
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(4px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
