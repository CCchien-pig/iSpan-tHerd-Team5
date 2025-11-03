<template>
  <div class="game-board">
    <div class="card-grid">
      <GameCard
        v-for="(card, index) in cards"
        :key="index"
        :image="card.image"
        :isFlipped="card.isFlipped"
        :isMatched="card.isMatched"
        @flip="onFlip(card)"
      />
    </div>
  </div>
</template>

<script setup>
import GameCard from './GameCard.vue'

/* ✅ 模擬卡片資料（可改為實際遊戲卡組） */
const cards = [
  { image: '/images/Game/back.png', isFlipped: false, isMatched: false },
  { image: '/images/Game/back.png', isFlipped: false, isMatched: false },
  { image: '/images/Game/back.png', isFlipped: false, isMatched: false },
  { image: '/images/Game/back.png', isFlipped: false, isMatched: false },
  { image: '/images/Game/back.png', isFlipped: false, isMatched: false },
  { image: '/images/Game/back.png', isFlipped: false, isMatched: false },
  { image: '/images/Game/back.png', isFlipped: false, isMatched: false },
  { image: '/images/Game/back.png', isFlipped: false, isMatched: false },
  { image: '/images/Game/back.png', isFlipped: false, isMatched: false },
  { image: '/images/Game/back.png', isFlipped: false, isMatched: false },
  { image: '/images/Game/back.png', isFlipped: false, isMatched: false },
  { image: '/images/Game/back.png', isFlipped: false, isMatched: false },
  { image: '/images/Game/back.png', isFlipped: false, isMatched: false },
  { image: '/images/Game/back.png', isFlipped: false, isMatched: false },
  { image: '/images/Game/back.png', isFlipped: false, isMatched: false },
  { image: '/images/Game/back.png', isFlipped: false, isMatched: false }
]

function onFlip(card) {
  console.log('flipped:', card)
}
</script>

<style scoped>
/* ✅ 外層容器：置中棋盤 */
.game-board {
  width: 100%;
  display: flex;
  justify-content: center;
  padding: 20px;
  box-sizing: border-box;
}

/* ✅ 桌機：4 張一排 */
.card-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 3px; /* 🔸固定上下左右間距 */
  justify-items: center;
  align-items: center;
  max-width: 800px;
  width: 100%;
  box-sizing: border-box;
}

/* ✅ 平板（寬度 ≤ 1024px）：3 張一排 */
@media (max-width: 1024px) {
  .card-grid {
    grid-template-columns: repeat(3, 1fr);
    max-width: 600px;
  }
}

/* ✅ 手機（寬度 ≤ 600px）：2 張一排 */
@media (max-width: 600px) {
  .card-grid {
    grid-template-columns: repeat(2, 1fr);
    max-width: 400px;
  }
}
</style>
