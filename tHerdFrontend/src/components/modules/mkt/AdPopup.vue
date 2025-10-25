<template>
  <div v-if="visible" class="popup-overlay">
    <div class="popup-content">
      <button class="close-btn" @click="closePopup">✕</button>
      <!-- 圖片 -->
      <img :src="imageUrl" alt="廣告" class="ad-image" />
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'

// 傳入圖片連結
const props = defineProps({
  imageUrl: {
    type: String,
    required: true
  },
  autoShow: {
    type: Boolean,
    default: true
  }
})

const visible = ref(false)

onMounted(() => {
  if (props.autoShow) {
    visible.value = true
  }
})

function closePopup() {
  visible.value = false
}
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
  max-width: 90vw;   /* ✅ 限制最大寬度為螢幕 90% */
  max-height: 90vh;  /* ✅ 限制最大高度為螢幕 90% */
}

.ad-image {
  display: block;
  width: 100%;
  height: auto;
  max-width: 1300px;  /* ✅ 桌機最大寬度 */
  max-height: 90vh;   /* ✅ 高度不超過螢幕 */
  object-fit: contain;
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
    max-width: 95vw;   /* 手機幾乎全寬 */
    max-height: 80vh;  /* 保持高度不超出版面 */
  }

  .close-btn {
    font-size: 18px;  /* 手機縮小按鈕 */
    top: 6px;
    right: 6px;
    padding: 3px 6px;
  }
}

/* 📱 超小螢幕（像 iPhone SE）再縮小 */
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
