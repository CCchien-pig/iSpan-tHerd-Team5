<!-- /src/components/coupon/TicketCoupon.vue -->
<template>
  <div class="ticket-coupon" :class="{ received: coupon.isReceived }">
    <div class="ticket-info">
      <h5 class="fw-bold mb-1">{{ coupon.couponName }}</h5>
      <p class="mb-1 text-muted">{{ coupon.couponCode }}</p>
      <small class="text-secondary">
        有效期限：{{ formatDate(coupon.endDate) }}
      </small>
    </div>

    <div class="ticket-price" :style="{ backgroundColor: couponColor, color: textColor }">
      <div class="amount">
        <template v-if="coupon.discountAmount">NT$ {{ coupon.discountAmount }}</template>
        <template v-else-if="coupon.discountPercent">{{ coupon.discountPercent }}%</template>
        <template v-else>—</template>
      </div>

      <!-- 預設插槽：不同頁可以放「領取」或「去使用」 -->
      <slot>
        <button
          class="use-btn"
          :disabled="coupon.isReceived"
          @click="$emit('receive', coupon)"
        >
          {{ coupon.isReceived ? '已領取' : '領取' }}
        </button>
      </slot>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
const props = defineProps({ coupon: { type: Object, required: true } })
function formatDate(dateStr) {
  if (!dateStr) return '無期限'
  const d = new Date(dateStr)
  return `${d.getFullYear()}/${(d.getMonth()+1).toString().padStart(2,'0')}/${d.getDate().toString().padStart(2,'0')}`
}

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
const textColor = computed(() => {
  return couponColor.value === 'rgb(242, 201, 76)' ? 'black' : 'white'
})
</script>
<!-- style 同你原本 -->
