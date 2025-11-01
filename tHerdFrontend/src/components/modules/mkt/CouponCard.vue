<template>
  <div 
    class="ticket-coupon"
    :class="{ received: coupon.isReceived }"
  >
    <!-- 左側內容 -->
    <div class="ticket-info">
      <h5 class="fw-bold mb-1">{{ coupon.couponName }}</h5>
      <p class="mb-1 text-muted">{{ coupon.couponCode }}</p>
      <small class="text-secondary">
        有效期限：{{ formatDate(coupon.endDate) }}
      </small>
    </div>

    <!-- 右側折扣：背景色＋字色 -->
    <div 
      class="ticket-price" 
      :style="{ backgroundColor: couponColor, color: textColor }"
    >
      <div class="amount">NT$ {{ coupon.discountAmount }}</div>
      <button
        class="use-btn"
        :disabled="coupon.isReceived"
        @click="$emit('receive', coupon)"
      >
        {{ coupon.isReceived ? '已領取' : '領取' }}
      </button>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  coupon: Object
})

function formatDate(dateStr) {
  if (!dateStr) return '無期限'
  const d = new Date(dateStr)
  return `${d.getFullYear()}/${(d.getMonth()+1).toString().padStart(2,'0')}/${d.getDate().toString().padStart(2,'0')}`
}

// 🎨 根據 couponName 判斷背景色
const couponColor = computed(() => {
  const name = props.coupon.couponName || ''
  if (name.includes('生日') || name.includes('節慶') || name.includes('聖誕')) {
    return 'rgb(178, 34, 34)'        // 紅
  } else if (name.includes('新客') || name.includes('首購')) {
    return 'rgb(242, 140, 40)'       // 橘
  } else if (name.includes('免運') || name.includes('運費')) {
    return 'rgb(242, 201, 76)'       // 黃
  } else if (name.includes('中秋') || name.includes('專屬')) {
    return 'rgb(123, 92, 168)'       // 紫
  } else if (name.includes('限時') || name.includes('活動')) {
    return 'rgb(27, 42, 73)'         // 深藍
  } else {
    return 'rgb(0, 112, 131)'        // 主色
  }
})

// 🖤 黃底字體變黑，其他維持白色
const textColor = computed(() => {
  return couponColor.value === 'rgb(242, 201, 76)' ? 'black' : 'white'
})
</script>

<style scoped>
.ticket-coupon {
  display: flex;
  justify-content: space-between;
  align-items: stretch;
  background: #fff;
  border-radius: 16px;
  position: relative;
  box-shadow: 0 2px 10px rgba(0,0,0,0.08);
  overflow: hidden;
  transition: transform 0.2s, box-shadow 0.2s;
}

.ticket-coupon:hover {
  transform: scale(1.02);
  box-shadow: 0 4px 16px rgba(0,0,0,0.12);
}

.ticket-coupon.received {
  opacity: 0.6;
  filter: grayscale(0.6);
}

.ticket-info {
  flex: 1;
  padding: 16px 20px;
  border-right: 2px dashed #ccc;
}

.ticket-price {
  min-width: 140px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 12px;
}

.amount {
  font-size: 1.8rem;
  font-weight: bold;
  margin-bottom: 8px;
}

.use-btn {
  background: white;
  color: rgb(0, 0, 0);
  border: none;
  border-radius: 8px;
  padding: 6px 12px;
  font-weight: bold;
  cursor: pointer;
  transition: all 0.2s;
}

.use-btn:hover:enabled {
  background: #e6f7f9;
}

.use-btn:disabled {
  background: #ccc;
  color: #666;
  cursor: not-allowed;
}

/* 券邊圓形 */
.ticket-coupon::before,
.ticket-coupon::after {
  content: '';
  position: absolute;
  top: 50%;
  width: 20px;
  height: 20px;
  background: white;
  border-radius: 50%;
  transform: translateY(-50%);
}

.ticket-coupon::before { left: -10px; }
.ticket-coupon::after { right: -10px; }

/* 📱 RWD 手機版 */
@media (max-width: 768px) {
  .ticket-coupon {
    flex-direction: column;
    align-items: stretch;
  }

  .ticket-info {
    border-right: none;
    border-bottom: 2px dashed #ccc;
    padding: 12px 16px;
    text-align: center;
  }

  .ticket-price {
    width: 100%;
    padding: 16px;
  }

  .amount {
    font-size: 1.6rem;
  }

  .use-btn {
    margin-top: 8px;
    width: 100%;
    max-width: 280px;
  }

  .ticket-coupon::before,
  .ticket-coupon::after {
    display: none;
  }
}
</style>
