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
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'

// ✅ 組件設定（是否自動顯示）
const props = defineProps({
  autoShow: {
    type: Boolean,
    default: true
  }
})

const visible = ref(false)
const ad = ref(null) // 儲存彈出式廣告資料

// === 🧭 關閉廣告 ===
function closePopup() {
  visible.value = false
}

// === 🔗 點擊圖片開啟連結 ===
function goToLink(link) {
  if (link) window.location.href = link
}

// === 📡 從後端載入廣告資料 ===
async function loadPopupAd() {
  try {
    const res = await axios.get('/api/mkt/ad/PopupList')
    const list = res.data || []

    if (list.length > 0) {
      // ✅ 這裡可以選擇第一筆或隨機一筆
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

/* 🪄 RWD 手機調整 */
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

/* 📱 超小螢幕 */
@media (max-width: 480px) {
  .ad-image {
    max-width: 95vw;
    max-height: 70vh;
  }
  .close-btn {
    font-size: 16px;
    padding: 2px 5px;
  }
}
</style>
