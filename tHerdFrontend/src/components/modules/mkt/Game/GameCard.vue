<template>
  <div
    class="card"
    :class="{ flipped: isFlipped || isMatched }"
    @click="handleFlip"
  >
    <div class="inner">
      <div class="front">
        <img src="/images/Game/back.png" alt="back" />
      </div>
      <div class="back">
        <img :src="image" alt="card" />
      </div>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  image: String,
  isFlipped: Boolean,
  isMatched: Boolean
})
const emit = defineEmits(['flip'])
function handleFlip() {
  if (!props.isFlipped && !props.isMatched) emit('flip')
}
</script>

<style scoped>
.card {
  width: 100%;
  aspect-ratio: 1 / 1.4; /* 固定比例 */
  perspective: 1000px;
  cursor: pointer;
  transition: transform 0.2s ease;
}
.card:hover {
  transform: scale(1.04);
}
.inner {
  width: 100%;
  height: 100%;
  position: relative;
  transform-style: preserve-3d;
  transition: transform 0.6s;
}
.card.flipped .inner {
  transform: rotateY(180deg);
}
.front,
.back {
  position: absolute;
  inset: 0;
  border-radius: 10px;
  backface-visibility: hidden;
  overflow: hidden;
  border: 2px solid #007083;
  display: flex;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
}
.front {
  background: rgb(248, 249, 250);
}
.front img {
  width: 70%;
  height: auto;
  object-fit: contain;
}
.back {
  transform: rotateY(180deg);
  background: #ffffff;
}
.back img {
  width: 70%;
  height: auto;
  object-fit: contain;
}
</style>
