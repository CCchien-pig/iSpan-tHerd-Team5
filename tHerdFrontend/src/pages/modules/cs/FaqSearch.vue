<template>
  <div class="faq-wrap py-5">
    <!-- 搜尋框（保持不動） -->
    <div class="center-narrow">
      <h3 class="text-center mb-4">常見問題搜尋</h3>
      <div id="faq-searchbox" class="position-relative mb-3">
  <div class="input-group">
    <input v-model.trim="q"
           @input="_debouncedFetch"
           @keydown="onKeydown"
           class="form-control"
           placeholder="請輸入關鍵字"/>
    <button class="btn btn-search" @click="doSearch">
      <span v-if="!loadingSuggest">搜尋</span>
      <span v-else class="spinner-border spinner-border-sm"></span>
    </button>
  </div>

  <!-- 即時建議清單（加上 ARIA + 視覺選中態） -->
<div
  v-if="open"
  role="listbox"
  :aria-activedescendant="active >= 0 ? `sugg-${active}` : null"
  class="position-absolute bg-white border rounded-3 shadow-sm w-100"
  style="z-index:1000; max-height: 320px; overflow:auto;"
>
  <button
    v-for="(s, idx) in suggestions"
    :key="s.id"
    :id="`sugg-${idx}`"
    role="option"
    :aria-selected="active === idx"
    type="button"
    class="suggest-item w-100 text-start px-3 py-2 border-0"
    :class="{ 'is-active': active === idx }"
    @mouseenter="active = idx"
    @mouseleave="active = -1"
    @click="openFeatured(s.id)"
  >
    <div class="fw-semibold">{{ s.title }}</div>
    <small class="text-muted">{{ s.categoryName }}</small>
  </button>

  <!-- 空態/載入（加上適合的 ARIA） -->
  <div
    v-if="!suggestions.length && !loadingSuggest"
    class="px-3 py-2 text-muted"
    role="status"
    aria-live="polite"
  >沒有相關建議</div>

  <div
    v-if="loadingSuggest"
    class="px-3 py-2"
    role="status"
    aria-live="polite"
  >
    <span class="spinner-border spinner-border-sm"></span> 載入中
  </div>
</div>

  </div>
</div>
    </div>

    <!-- ✅ 精選答案卡（點建議後顯示；右上角 X 可關閉） -->
<div v-if="featuredOpen" class="center-narrow" id="featured-ans">
  <div class="featured-card position-relative p-3 p-md-4 mb-4 bg-white border rounded-3 shadow-sm">
    <button class="btn-close position-absolute top-0 end-0 m-2" @click="closeFeatured"></button>
    <div class="small text-muted mb-2" v-if="featuredFaq?.categoryName">{{ featuredFaq.categoryName }}</div>
    <h5 class="mb-3 main-color-green-text">{{ featuredFaq?.title }}</h5>

    <div v-if="featuredLoading" class="text-muted">
      <span class="spinner-border spinner-border-sm"></span> 載入中…
    </div>
    <div v-else class="text-secondary small" v-html="featuredFaq?.answerHtml"></div>
  </div>
</div>

      <!-- ❶ 快捷卡片：移到幫助文章上方 -->
<div class="quick-area">
  <div class="center-narrow">
    <div class="row g-3 justify-content-center text-center">

      <!-- 每張卡片 -->
      <div v-for="a in quickActions" :key="a.text" class="col-6 col-sm-3">

        <!-- 把整張卡包成 RouterLink，不會影響 SVG -->
        <RouterLink :to="a.to" class="qcard text-decoration-none d-block">
          <div class="qicon" v-html="a.svg"></div>
          <div class="fw-semibold" style="color:rgb(0, 112, 131)">
            {{ a.text }}
          </div>
        </RouterLink>

      </div>
    </div>
  </div>
</div>

 <!-- ❷ 幫助文章（分類 + 問題標題 + 內容右上 X） -->
<div class="center-narrow">
  <div class="section-title"><span>幫助文章</span></div>

  <div v-for="cat in categories" :key="cat.categoryId" class="cat-block">
    <!-- 分類列：黑字粗體 + 右側 + / − -->
    <div class="cat-row" @click="toggleCategory(cat.categoryId)">
      <h5 class="mb-0 fw-bold text-dark">{{ cat.categoryName }}</h5>
      <div class="pm-box" :class="{ open: openCategoryId === cat.categoryId }">
        <span></span>
      </div>
    </div>

    <!-- 展開後顯示該分類的問題清單 -->
    <transition name="slide">
      <div v-if="openCategoryId === cat.categoryId" class="cat-panel">
        <ul class="list-unstyled mb-0">
          <li v-for="f in cat.faqs" :key="f.faqId" class="qitem">
            <!-- 問題標題（藍綠字），點擊切換該題答案 -->
            <div class="qtitle main-color-green-text fw-semibold"
                 @click.stop="toggleFaq(f.faqId)">
              {{ f.title }}
            </div>

            <!-- 這題的答案；右上角 X 關閉 -->
            <transition name="fade">
              <div v-if="openFaqId === f.faqId" class="ans-wrap">
                <button class="btn-close ans-close" @click.stop="openFaqId = null"></button>
                <div class="ans-body small text-secondary" v-html="f.answerHtml"></div>
              </div>
            </transition>
          </li>
        </ul>
      </div>
    </transition>
  </div>
</div>
<!-- 聯繫我們區塊 -->
<div class="contact-help-box text-center mt-5">
  <h4 class="fw-bold mb-4">還有其他問題嗎？</h4>

  <div class="contact-card mx-auto">
    <div class="d-flex align-items-center justify-content-center mb-3">
      <div class="contact-icon me-3">
        <svg xmlns="http://www.w3.org/2000/svg" width="28" height="28"
             viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
             stroke-linecap="round" stroke-linejoin="round">
          <path fill-rule="evenodd" clip-rule="evenodd" d="M9.87388 19.219C11.6706 17.7826 13.9025 17.0001 16.2028 17.0001H19C19.5522 17.0001 20 16.5524 20 16.0001V5.00009C20 4.44781 19.5522 4.00009 19 4.00009H4.99995C4.44767 4.00009 3.99995 4.44781 3.99995 5.00009V16.0001C3.99995 16.5524 4.44767 17.0001 4.99995 17.0001H8.99671V19.9203L9.87388 19.219ZM5 19C3.34315 19 2 17.6569 2 16V5C2 3.34315 3.34315 2 5 2H19C20.6569 2 22 3.34315 22 5V16C22 17.6569 20.6569 19 19 19H16.2028C14.3564 19 12.565 19.6281 11.1228 20.7811L9.43342 22.1317C8.78637 22.649 7.84246 22.5438 7.32515 21.8968C7.11256 21.6309 6.99675 21.3006 6.99675 20.9601V19H5ZM6.42647 12.3192C5.97402 12.0025 5.86399 11.379 6.1807 10.9265C6.49741 10.4741 7.12094 10.364 7.57339 10.6808C10.2312 12.5412 13.7687 12.5412 16.4265 10.6808C16.8789 10.364 17.5024 10.4741 17.8192 10.9265C18.1359 11.379 18.0258 12.0025 17.5734 12.3192C14.227 14.6617 9.77291 14.6617 6.42647 12.3192Z" fill="white"></path>
        </svg>
      </div>
      <div class="text-start">
        <div class="fw-semibold fs-5">聊天和 24/7 電子郵件幫助</div>
        <div class="text-muted small"></div>
      </div>
    </div>

    <button class="btn btn-main px-5 py-2" @click="openChat">聯繫我們</button>
  </div>
</div>

</template>
//CsChat.vue  的聊天機器人程式碼
<script setup>
import { onMounted, onUnmounted } from 'vue'

// 同一個 ID（用你那段 script 裡的 id）
const CHATBASE_ID = 'BRCpzNGrd5C1SMBsd6zmU'

function loadWidget () {
  if (document.getElementById(CHATBASE_ID)) return
  const s = document.createElement('script')
  s.src = 'https://www.chatbase.co/embed.min.js'
  s.id = CHATBASE_ID
  s.defer = true
  document.body.appendChild(s)
}

function cleanupWidget () {
  // 如果你只想讓 FAQ 頁才顯示，離開頁面就清掉
  document.querySelectorAll(`#${CHATBASE_ID}, .cb-bubble, .cb-chat-container`)
    .forEach(el => el.remove())
}

onMounted(loadWidget)
onUnmounted(cleanupWidget)
</script>


<script>
import { getFaqList, searchFaq , suggestFaq, getFaqDetail } from '@/api/modules/cs/csfaq'
export default {
  name: 'FaqSearch',
  data() {
    return {
      q: '',
      results: [],
      searched: false,
      loading: false,
     openCategoryId: null, // 目前展開的分類
    openFaqId: null,      // 目前展開的那一題
      categories: [],
      suggestions: [],    // 建議清單
open: false,        // 是否開啟下拉
active: -1,         // 目前選中的索引（用鍵盤上下移動）
loadingSuggest: false,
timer: null,
featuredOpen: false,
    featuredFaq: null, // {faqId,title,categoryName, answerHtml }
    featuredLoading: false,
      quickActions: [
  {
    text: '追蹤我的訂單',
    to: '/orders',
    svg: `<svg width="52" height="52" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path d="M3 7h13l3 5v5h-3"></path>
            <circle cx="7.5" cy="18.5" r="1.5"></circle>
            <circle cx="17.5" cy="18.5" r="1.5"></circle>
          </svg>`
  },
  {
    text: '修改或取消訂單',
    to: '/orders',
    svg: `<svg width="52" height="52" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <rect x="3" y="3" width="18" height="14" rx="2"></rect>
            <path d="M3 9h18"></path>
          </svg>`
  },
  {
    text: '忘記密碼',
    to: '/user/forgot',
    svg: `<svg width="52" height="52" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <rect x="3" y="11" width="18" height="10" rx="2"></rect>
            <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
          </svg>`
  },
  {
    text: '客服服務',
    to: '/cs/ticket',
    svg: `<svg width="52" height="52" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path d="M21 15a4 4 0 0 1-4 4h-3l-4 3v-3H7a4 4 0 0 1-4-4V9a4 4 0 0 1 4-4h10a4 4 0 0 1 4 4z"></path>
          </svg>`
  }
]

    }
  },
mounted() {
  this.loadCategories()
  document.addEventListener('click', this._onDocClick)
},
beforeUnmount() {
  document.removeEventListener('click', this._onDocClick)
// 徹底清理：script、氣泡、面板、iframe
  document.querySelectorAll([
    '#chatbase-script',
    '.cb-bubble',
    '.cb-chat-container',
    'iframe[src*="chatbase"]'
  ].join(',')).forEach(el => el.remove())

  if (window.chatbaseConfig) delete window.chatbaseConfig
  if (window.chatbase) delete window.chatbase
},

  methods: {
    // ---------------- 即時建議 START ----------------
async fetchSuggest() {
  const kw = this.q.trim()
  if (!kw) { this.suggestions = []; this.open = false; return }
  this.loadingSuggest = true
  try {
    // 改這行：使用 suggestFaq()
    const list = await suggestFaq(kw)
    this.suggestions = (list || []).slice(0, 6).map(x => ({
      id: x.faqId, title: x.title, categoryName: x.categoryName
    }))
    this.open = true // 只要有輸入就打開，交給 template 判斷空清單
    this.active = -1
  } finally {
    this.loadingSuggest = false
  }
},
_debouncedFetch() {
  clearTimeout(this.timer)
  this.timer = setTimeout(this.fetchSuggest, 200) // 防抖
},
onKeydown(e) {
  if (e.key === 'ArrowDown') {
    e.preventDefault()
    if (!this.open) { this.open = this.suggestions.length > 0; return }
    if (!this.suggestions.length) return
    this.active = (this.active + 1) % this.suggestions.length
    return
  }
  if (e.key === 'ArrowUp') {
    e.preventDefault()
    if (!this.open) { this.open = this.suggestions.length > 0; return }
    if (!this.suggestions.length) return
    this.active = (this.active - 1 + this.suggestions.length) % this.suggestions.length
    return
  }
  if (e.key === 'Enter') {
    e.preventDefault()
    if (this.open && this.suggestions.length) {
      // 沒移動過就選第一個
      if (this.active < 0) this.active = 0
      this.select(this.suggestions[this.active]) // 內部會呼叫 openFeatured()
      return
    }
    this.doSearch()
    return
  }
  if (e.key === 'Escape') {
    if (this.open) { this.open = false; e.preventDefault() }
  }
},

select(item) {
  this.q = item.title
 this.openFeatured(item.id)   // ← 直接開精選答案卡
},
_onDocClick(e) {
  const box = document.getElementById('faq-searchbox')
  if (box && !box.contains(e.target)) this.open = false
},
// ---------------- 即時建議 END ----------------

  async loadCategories() {
    const data = await getFaqList()
    this.categories = data
  },
   toggleCategory(id) {
     // 切換分類；若切到別的分類，要把開著的答案關掉
    this.openCategoryId = this.openCategoryId === id ? null : id
    this.openFaqId = null  
  },
  toggleFaq(fid) {
    this.openFaqId = this.openFaqId === fid ? null : fid
  },
 async doSearch() {
  const kw = this.q?.trim()
  if (!kw) return
  this.loading = true
  this.featuredOpen = false // 搜尋時關閉精選答案卡
  try {
    const list = await searchFaq(kw)
    this.results = Array.isArray(list) ? list : (list?.data ?? [])
  } catch (e) {
    console.error(e)
    this.results = [] // 失敗也顯示空態，而不是卡住
  } finally {
    this.loading = false
    this.searched = true
    this.open = false // 關閉下拉
    this.q = '' // ✅ 清空搜尋框
  }
},

  hl(text) {
    if (!this.q || !text) return text
    const esc = this.q.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
    return String(text).replace(new RegExp(esc, 'gi'), m => `<mark>${m}</mark>`)
  },
async openFeatured(faqId) {
  try {
    this.featuredLoading = true
    this.featuredOpen = true
    this.open = false        // ✅ 點下建議時立刻關閉建議下拉
    this.searched = false    // ✅ 若想同時清空舊搜尋結果
    this.q = '' 

    // 正確取 data
    const res = await getFaqDetail(faqId)
    // 若後端包 ApiResponse<T>，就要取 res.data
    this.featuredFaq = res.data ? res.data : res  // 自動判斷格式

    // 捲動到卡片
   this.$nextTick(() => {
  const el = document.getElementById('featured-ans')
  if (!el) return
  // 換成你站上的 header 選擇器（例如 .navbar、#site-header、header）
  const header = document.querySelector('.navbar, #site-header, header')
  const offset = (header?.offsetHeight || 80) + 12 // 80 是預設高度，+12 做一點間距
  const y = el.getBoundingClientRect().top + window.scrollY - offset
  window.scrollTo({ top: y, behavior: 'smooth' })
})

  } catch (err) {
    console.error('openFeatured error:', err)
  } finally {
    this.featuredLoading = false
  }
},

closeFeatured() {
  this.featuredOpen = false
  this.featuredFaq = null
  this.$nextTick(() => window.scrollTo({ top: 0, behavior: 'smooth' }))
},
 openChat() {
  if (window.chatbase && typeof window.chatbase.open === 'function') {
    window.chatbase.open()
    return
  }
  if (!document.getElementById('chatbase-script')) {
    if (!window.chatbaseConfig) window.chatbaseConfig = {}
    window.chatbaseConfig.chatbotId = 'BRCpzNGrd5C1SMBsd6zmU'

    const s = document.createElement('script')
    s.id = 'chatbase-script'
    s.src = 'https://www.chatbase.co/embed.min.js'
    s.defer = true
    s.onload = () => window.chatbase?.open?.()
    document.head.appendChild(s)
  }
},
_cleanupChatbase() {
  try {
    const sel = [
      '#chatbase-script',
      '.cb-bubble',
      '.cb-chat-container',
      'iframe[src*="chatbase"]'
    ].join(',')

    document.querySelectorAll(sel).forEach(el => {
      try {
        el.remove()
      } catch (err) {
        console.warn('移除節點失敗：', err)
      }
    })

    if (window.chatbase) window.chatbase = undefined
    if (window.chatbaseConfig) window.chatbaseConfig = undefined
  } catch (err) {
    console.warn('清理 Chatbase 失敗：', err)
  }
},

  },
_cleanupChatbase() {
  try {
    const sel = [
      '#chatbase-script',
      '.cb-bubble',
      '.cb-chat-container',
      'iframe[src*="chatbase"]'
    ].join(',')

    document.querySelectorAll(sel).forEach(el => {
      try {
        el.remove()
      } catch (err) {
        console.warn('移除節點失敗：', err)
      }
    })

    if (window.chatbase) window.chatbase = undefined
    if (window.chatbaseConfig) window.chatbaseConfig = undefined
  } catch (err) {
    console.warn('清理 Chatbase 失敗：', err)
  }
},
  quickFill(text) {
  this.q = text
  this.doSearch()
},
}
</script>

<style scoped>
.btn-search {
  background-color: rgb(0, 112, 131);       /* 主色：藍綠 */
  color: rgb(248, 249, 250);               /* 字色：白 */
  border: none;
  transition: background-color 0.2s ease;
}

.btn-search:hover {
  background-color: rgb(77, 180, 193);     /* 輔色：淺藍綠 */
  color: rgb(248, 249, 250);
}

/* 整體寬度控制：中間置中 */
.center-narrow { max-width: 880px; margin: 0 auto; }
/* 區塊標題（水平置中、小分隔線） */
.section-title { display:flex; align-items:center; gap:.75rem; margin: 8px 0 14px; }
.section-title::before, .section-title::after {
  content:""; flex:1; height:1px; background:#e6e6e6;
}
.section-title > span { white-space:nowrap; color:#333; font-weight:600; }

/* 快捷卡片（iHerb 圓形圖示 + 文案） */
.quick-area { background: #f8faf8; padding: 28px 0 48px; margin-top: 36px; }
.qcard {
  background:#fff; border:1px solid #e8ece8; border-radius:16px;
  padding:18px 8px; position:relative; transition:transform .12s ease, box-shadow .12s ease;
}
.qcard:hover{ transform: translateY(-2px); box-shadow:0 6px 16px rgba(0,0,0,.06); }
.qicon{
  width:72px; height:72px; margin:0 auto 8px; border:1px solid #e8ece8; border-radius:50%;
  display:flex; align-items:center; justify-content:center; color:rgb(0, 112, 131);
}
.qcard a{ display:block; font-weight:600; color:rgb(0, 112, 131); text-decoration:none; }
.qcard a:hover{ text-decoration:underline; }
/* FAQ 問題項目 hover 效果 */
.faq-item {
  background: #fff;
  transition: box-shadow 0.15s ease, border-color 0.15s ease;
  cursor: pointer;
}
.faq-item:hover {
  border-color: rgb(0, 112, 131);
  box-shadow: 0 4px 10px rgba(0, 112, 131, 0.08);
}
/* 分類外框 */
.cat-block { border-bottom: 1px solid #eee; }

/* 分類列：左標題 + 右 +/− 方框 */
.cat-row {
  display:flex; align-items:center; justify-content:space-between;
  padding:16px 4px; cursor:pointer;
}
.pm-box {
  width:40px; height:28px; border:1px solid #ddd; border-radius:6px;
  display:flex; align-items:center; justify-content:center; position:relative;
}
.pm-box span,
.pm-box::after {
  content:""; position:absolute; background:#555; transition:transform .2s ease, opacity .2s ease;
}
.pm-box span { width:14px; height:2px; }         /* 橫線 */
.pm-box::after { width:2px; height:14px; }       /* 直線（+） */
.pm-box.open::after { opacity:0; transform:scaleY(0); } /* 展開時變成 − */

/* 展開的分類面板 */
.cat-panel { padding: 10px 4px 16px; }

/* 問題列（藍綠字） */
.qitem + .qitem { margin-top: 10px; }
.qtitle { cursor:pointer; }

/* 答案框：右上角 X，白底陰影 */
.ans-wrap {
  position: relative;
  background:#fff; border:1px solid #e6e6e6; border-radius:10px;
  padding: 16px 16px 12px; margin-top:10px;
  box-shadow: 0 6px 16px rgba(0,0,0,.06);
}
.ans-close { position:absolute; right:8px; top:8px; }

/* 動畫 */
.fade-enter-active, .fade-leave-active { transition: all .2s ease; }
.fade-enter-from, .fade-leave-to { opacity:0; transform: translateY(-4px); }

.slide-enter-active, .slide-leave-active { transition: all .25s ease; }
.slide-enter-from, .slide-leave-to { opacity:0; transform: translateY(-8px); }

/* 建議項目 hover 效果 */
.suggest-item {
  background-color: #fff;
  transition: background-color 0.15s ease;
}

.suggest-item:hover {
  background-color: #4DB4C1;
}
/* === 聯繫我們區塊 === */
.contact-help-box {
  background-color: #f8faf8;
  padding: 60px 20px 80px;
}

.contact-card {
  background: #fff;
  border: 1px solid #e8ece8;
  border-radius: 16px;
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.06);
  padding: 32px 24px;
  max-width: 420px;
}

.contact-icon {
  background-color: rgb(0, 112, 131); /* 主色 */
  color: #fff;
  border-radius: 50%;
  width: 56px;
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: center;
}

/* 主色按鈕 */
.btn-main {
  background-color: rgb(0, 112, 131);
  color: #fff;
  border: none;
  border-radius: 6px;
  font-weight: 600;
  transition: background-color 0.2s ease, transform 0.1s ease;
}

.btn-main:hover {
  background-color: rgb(77, 180, 193);
  transform: translateY(-1px);
}
/* 手機優化（≤576px） */
@media (max-width: 576px) {
  .center-narrow { padding: 0 16px; }             /* 兩側安全邊距 */
  .quick-area { padding: 20px 0 28px; margin-top: 20px; }
  .qicon { width: 56px; height: 56px; }           /* 圖示縮一階 */
  .qcard { padding: 14px 8px; }
  .pm-box { width: 36px; height: 24px; }          /* +／− 小方塊 */
  .cat-row { padding: 12px 0; }
  .ans-wrap { padding: 12px; }
  .contact-help-box { padding: 40px 16px 56px; }
  .contact-card { padding: 24px 16px; max-width: 100%; }
  /* 建議清單高度用視窗比例，避免超出 */
  #faq-searchbox > .position-absolute { max-height: 60vh; }
}

/* 中尺寸（≥768px）可以把快捷卡行距拉開一點，看起來更呼吸 */
@media (min-width: 768px) {
  .quick-area .row { --bs-gutter-x: 1.25rem; --bs-gutter-y: 1.25rem; }
}
/* 鍵盤選中態（與滑鼠 hover 視覺一致） */
.suggest-item.is-active,
.suggest-item:hover {
  background-color: #4DB4C1;
  color: #fff;
}

/* 讓內部小字在選中時也變亮（scoped 下要用 :deep） */
:deep(.suggest-item.is-active .text-muted),
:deep(.suggest-item:hover .text-muted) {
  color: #fff !important;
}
</style>

