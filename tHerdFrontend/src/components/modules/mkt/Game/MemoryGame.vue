<template>
  <div class="memory-game" :class="{ flash: showFlash }">
    <!-- ✅ 若尚未玩過 -->
    <div v-if="!hasPlayed">
      <!-- 開始畫面 -->
      <div v-if="!gameStarted && !showCountdown" class="start-screen">
        <h2 class="main-color-green-text">翻牌記憶遊戲</h2>
        <p class="main-color-green-text">限時 30 秒，配對越多優惠越大！</p>
        <button @click="prepareStart" class="start-btn">開始遊戲</button>
      </div>

      <!-- 倒數畫面 -->
      <div v-else-if="showCountdown" class="countdown">{{ countdownText }}</div>

      <!-- 遊戲主畫面 -->
      <div v-else>
        <div class="header">
          <h2 class="main-color-green-text">翻牌記憶遊戲</h2>
          <p class="timer main-color-green-text">剩餘時間：{{ timeLeft }} 秒</p>
          <p class="score main-color-green-text" :class="{ 'score-animate': scoreAnimate }">
            分數：{{ score }}
          </p>
        </div>

        <div class="grid">
          <GameCard
            v-for="(card, index) in cards"
            :key="index"
            :image="card.image"
            :isFlipped="card.flipped"
            :isMatched="card.matched"
            @flip="flipCard(index)"
          />
        </div>
      </div>

      <!-- ✅ Modal 使用 teleport 掛在 body，避免被覆蓋 -->
      <teleport to="body">
        <div id="modal-debug-anchor"></div> <!-- 🔧 測試用 -->
        <GameResultModal
          v-if="isGameOver"
          :score="score"
          :isClear="isClear"
          @submit="submitScore"
        />
      </teleport>
    </div>

    <!-- ✅ 今日已玩過 -->
    <div v-else class="played-message">
      <h2 class="main-color-green-text">今日已玩過遊戲</h2>
      <p class="main-color-green-text">請明天再來挑戰吧！</p>
    </div>

    <!-- 🎆 彩帶 -->
    <div v-if="showConfetti" class="confetti-container">
      <div v-for="n in 50" :key="n" class="confetti"></div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, nextTick } from 'vue'
import http from '@/api/http' // ✅ 改用全域 http（自動附 JWT）
import GameCard from './GameCard.vue'
import GameResultModal from './GameResultModal.vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth' // ✅ 取得登入資訊

// ✅ 防止重複掛載
if (window.__MEMORY_GAME_ACTIVE__) {
  console.warn('⚠️ MemoryGame 已存在，阻止重複渲染')
  throw new Error('MemoryGame duplicated')
}
window.__MEMORY_GAME_ACTIVE__ = true

const cards = ref([])
const firstCard = ref(null)
const secondCard = ref(null)
const lockBoard = ref(false)
const score = ref(0)
const timeLeft = ref(30)
const isGameOver = ref(false)
const isClear = ref(false)
const showFlash = ref(false)
const showConfetti = ref(false)
const hasPlayed = ref(false)
const timer = ref(null)
const gameStarted = ref(false)
const showCountdown = ref(false)
const countdownText = ref('')
const scoreAnimate = ref(false)
const router = useRouter()
const auth = useAuthStore()

const user = auth.user
const userNumberId = user?.userNumberId || user?.user_number_id // ✅ 從登入資訊取

const images = [
  '/images/Game/game01.png',
  '/images/Game/game02.png',
  '/images/Game/game03.png',
  '/images/Game/game04.png',
  '/images/Game/game05.png',
  '/images/Game/game06.png',
  '/images/Game/game07.png',
  '/images/Game/game08.png',
  '/images/Game/game09.png',
  '/images/Game/game10.png'
]

// ✅ 檢查今日是否已玩過
async function checkTodayPlayed() {
  try {
    if (!userNumberId) {
      console.warn('⚠️ 尚未登入，無法檢查遊戲紀錄')
      return
    }

    const { data } = await http.get(`/mkt/MktGameRecord/${userNumberId}`)
    if (data && data.played === true) {
      hasPlayed.value = true
      console.log('🎮 今日已玩過遊戲')
    } else {
      hasPlayed.value = false
      console.log('🆕 今日尚未遊玩')
    }
  } catch (err) {
    console.error('檢查遊戲狀態失敗', err)
  }
}

function shuffleCards() {
  const doubled = [...images, ...images]
  cards.value = doubled
    .sort(() => Math.random() - 0.5)
    .map(img => ({ image: img, flipped: false, matched: false }))
}

function prepareStart() {
  showCountdown.value = true
  countdownText.value = '3'
  let count = 3
  const countdown = setInterval(() => {
    count--
    countdownText.value = count > 0 ? String(count) : 'GO!'
    if (count < 0) {
      clearInterval(countdown)
      showCountdown.value = false
      startGame()
    }
  }, 1000)
}

function startGame() {
  gameStarted.value = true
  shuffleCards()
  score.value = 0
  timeLeft.value = 30
  isGameOver.value = false
  isClear.value = false
  startTimer()
}

function startTimer() {
  if (timer.value) clearInterval(timer.value)
  timer.value = setInterval(() => {
    timeLeft.value--
    if (timeLeft.value <= 0) endGame(false)
  }, 1000)
}

async function flipCard(index) {
  if (lockBoard.value || cards.value[index].flipped || isGameOver.value) return
  cards.value[index].flipped = true

  if (firstCard.value === null) {
    firstCard.value = index
    const currentIndex = index
    setTimeout(() => {
      if (firstCard.value === currentIndex && !cards.value[currentIndex].matched && !secondCard.value) {
        cards.value[currentIndex].flipped = false
        firstCard.value = null
      }
    }, 3000)
    return
  }

  secondCard.value = index
  lockBoard.value = true

  const first = cards.value[firstCard.value]
  const second = cards.value[secondCard.value]

  if (first.image === second.image) {
    score.value++
    scoreAnimate.value = true
    setTimeout(() => (scoreAnimate.value = false), 800)
    first.matched = true
    second.matched = true
    resetBoard()

    await nextTick()
    if (cards.value.every(c => c.matched)) endGame(true)
  } else {
    setTimeout(() => {
      first.flipped = false
      second.flipped = false
      resetBoard()
    }, 800)
  }
}

function resetBoard() {
  firstCard.value = null
  secondCard.value = null
  lockBoard.value = false
}

function endGame(isPerfect) {
  clearInterval(timer.value)
  timer.value = null
  setTimeout(() => {
    isGameOver.value = true
    isClear.value = isPerfect
    console.log('🎯 結算觸發，isClear=', isPerfect)
  }, 200)
}

// ✅ 遊戲結果送出
async function submitScore() {
  if (!userNumberId) {
    alert('⚠️ 尚未登入會員，無法上傳遊戲結果')
    return
  }

  const dto = {
    userNumberId,
    score: score.value,
    couponAmount: score.value * 10,
    playedDate: new Date(),
    createdDate: new Date()
  }

  try {
    const res = await http.post('/mkt/MktGameRecord', dto)
    console.log('🎯 遊戲結果送出成功：', res.data)

    hasPlayed.value = true
    isGameOver.value = false
    gameStarted.value = false

    // ✅ 暫存分數 + 通知優惠券頁更新
    localStorage.setItem('gameScore', score.value)
    localStorage.setItem('refreshCoupons', 'true')

    setTimeout(() => {
      router.push('/') // ✅ 回首頁
    }, 1000)
  } catch (err) {
    console.error('紀錄失敗', err)
  }
}

onMounted(checkTodayPlayed)
onUnmounted(() => {
  if (timer.value) clearInterval(timer.value)
  delete window.__MEMORY_GAME_ACTIVE__
})
</script>

<style scoped>
.memory-game {
  text-align: center;
  padding: 20px;
  overflow: visible;
}

.start-screen {
  margin-top: 100px;
}

.start-btn {
  margin-top: 20px;
  padding: 10px 20px;
  background: #007083;
  color: #fff;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 18px;
}

.countdown {
  font-size: 80px;
  color: #007083;
  font-weight: bold;
  animation: zoom 1s ease-in-out infinite;
}

@keyframes zoom {
  0%,100% { transform: scale(1); opacity: 1; }
  50% { transform: scale(1.4); opacity: 0.7; }
}

.grid {
  display: grid;
  grid-template-columns: repeat(5, 110px);
  grid-template-rows: repeat(2, 150px);
  gap: 12px;
  justify-content: center;
}

/* ✨ 更深色金屬反光文字效果（自動播放） */
h2.main-color-green-text {
  font-size: 2.8rem;
  font-weight: 700;
  text-transform: none;
  position: relative;
  color: rgb(0, 112, 131);
  background: linear-gradient(
    120deg,
    rgb(0, 90, 110) 0%,
    rgb(0, 135, 150) 30%,
    rgb(0, 180, 190) 45%,
    rgb(0, 112, 131) 60%,
    rgb(0, 90, 110) 100%
  );
  background-size: 300% 300%;

  /* ✅ 建議這樣寫：先標準屬性，再加前綴 */
  background-clip: text;
  -webkit-background-clip: text;

  -webkit-text-fill-color: transparent;
  animation: tealReflect 2.2s linear infinite;
  text-shadow:
    0 0 6px rgba(0, 112, 131, 0.5),
    0 0 12px rgba(0, 112, 131, 0.3);
}


/* 💫 自動播放的掃光動畫 */
@keyframes tealReflect {
  0% {
    background-position: 200% 0;
  }
  100% {
    background-position: -200% 0;
  }
}



/* ✅ 其他段落與標題放大 */
p.main-color-green-text {
  font-size: 1.4rem;
  color: rgb(0, 112, 131);
}

</style>
