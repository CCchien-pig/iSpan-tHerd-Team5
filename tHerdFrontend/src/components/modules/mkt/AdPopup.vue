<template>
  <div v-if="visible && ad" class="popup-overlay">
    <div class="popup-content">
      <button class="close-btn" @click="closePopup">✕</button>

      <!-- 圖片 -->
      <img
        :src="ad.imageUrl"
        alt="廣告"
        class="ad-image"
        @click="goToLink(ad.link)"
      />

      <!-- 下方功能列 -->
      <div class="popup-footer">
        <button class="dismiss-btn" @click="dismissToday">今日不再顯示</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'

// ✅ 組件設定
const props = defineProps({
  autoShow: {
    type: Boolean,
    default: true
  }
})

const visible = ref(false)
const ad = ref(null)

// === 🧭 關閉廣告 ===
function closePopup() {
  visible.value = false
}

// === 🚫 今日不再顯示 ===
function dismissToday() {
  const today = new Date().toISOString().split('T')[0]
  localStorage.setItem('popupDismissDate', today)
  visible.value = false
}

// === 🔗 點擊圖片開啟連結 ===
function goToLink(link) {
  if (link) window.location.href = link
}

// === 📡 從後端載入廣告資料 ===
async function loadPopupAd() {
  try {
    const today = new Date().toISOString().split('T')[0]
    const lastDismiss = localStorage.getItem('popupDismissDate')

    // ✅ 若今日已選擇「不再顯示」則直接 return
    if (lastDismiss === today) {
      console.log('今日已關閉廣告，不再顯示')
      return
    }

    const res = await axios.get('/api/mkt/ad/PopupList')
    const list = res.data || []

    if (list.length > 0) {
      ad.value = list[Math.floor(Math.random() * list.length)]
      if (props.autoShow) visible.value = true
    }
  } catch (err) {
    console.error('載入彈出式廣告失敗：', err)
  }
}

onMounted(() => {
  loadPopupAd()
})
</script>

<style scoped>
.popup-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 3000;
}

.popup-content {
  position: relative;
  background: transparent;
  border-radius: 0;
  overflow: visible;
  box-shadow: none;
  max-width: 90vw;
  max-height: 90vh;
}

.ad-image {
  display: block;
  width: 100%;
  height: auto;
  max-width: 1300px;
  max-height: 90vh;
  object-fit: contain;
  cursor: pointer;
}

.close-btn {
  position: absolute;
  top: 10px;
  right: 10px;
  background: rgba(0, 0, 0, 0.5);
  color: white;
  border: none;
  font-size: 22px;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 50%;
  z-index: 10;
  transition: background 0.2s;
}
.close-btn:hover {
  background: rgba(0, 0, 0, 0.8);
}

/* 📅 今日不再顯示按鈕 */
.popup-footer {
  text-align: center;
  margin-top: 8px;
}
.dismiss-btn {
  background: rgba(255, 255, 255, 0.9);
  color: #007083;
  border: 1px solid #007083;
  border-radius: 20px;
  padding: 4px 12px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s ease;
}
.dismiss-btn:hover {
  background: #007083;
  color: #fff;
}

@media (max-width: 768px) {
  .ad-image {
    max-width: 95vw;
    max-height: 80vh;
  }
  .close-btn {
    font-size: 18px;
    top: 6px;
    right: 6px;
    padding: 3px 6px;
  }
}
</style>
