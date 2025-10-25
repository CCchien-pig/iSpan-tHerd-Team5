<template>
  <div class="ticket-coupon" :class="{ received: coupon.isReceived }">
    <!-- 左側內容 -->
    <div class="ticket-info">
      <h5 class="fw-bold mb-1">{{ coupon.couponName }}</h5>
      <p class="mb-1 text-muted">{{ coupon.couponCode }}</p>
      <small class="text-secondary">
        有效期限：
        {{ formatDate(coupon.endDate) }}
      </small>
    </div>

    <!-- 右側折扣 -->
    <div class="ticket-price">
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
defineProps({
  coupon: Object
})

function formatDate(dateStr) {
  if (!dateStr) return '無期限'
  const d = new Date(dateStr)
  return `${d.getFullYear()}/${(d.getMonth()+1).toString().padStart(2,'0')}/${d.getDate().toString().padStart(2,'0')}`
}
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

/* ✅ 領取後整張券變灰 */
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
  background: rgb(0,112,131);
  color: white;
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
  color: rgb(0,112,131);
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

.ticket-coupon::before {
  left: -10px;
}

.ticket-coupon::after {
  right: -10px;
}
</style>
