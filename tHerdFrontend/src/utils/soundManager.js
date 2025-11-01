// src/utils/soundManager.js

let sounds = {}

export function initSounds() {
  if (Object.keys(sounds).length > 0) return // 防止重複建立

  sounds = {
    flip: new Audio('/images/Game/flip.mp3'),
    match: new Audio('/images/Game/match.mp3'),
    wrong: new Audio('/images/Game/wrong.mp3'),
    clear: new Audio('/images/Game/clear.mp3'),
    gameover: new Audio('/images/Game/gameover.mp3'),
  }

  console.log('🎧 SoundManager initialized')
}

export function playSound(name, volume = 1) {
  const sound = sounds[name]
  if (sound) {
    sound.currentTime = 0
    sound.volume = volume
    sound.play().catch(() => {})
  }
}

export function stopAllSounds() {
  Object.values(sounds).forEach((s) => {
    s.pause()
    s.currentTime = 0
  })
}

// ✅ HMR 熱重載清理
if (import.meta.hot) {
  import.meta.hot.dispose(() => {
    stopAllSounds()
    Object.values(sounds).forEach((s) => (s.src = ''))
    sounds = {}
    console.log('🧹 SoundManager 已清除音效實例')
  })
}
