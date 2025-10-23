<template>
  <div class="faq-wrap py-5">
    <!-- 搜尋框（保持不動） -->
    <div class="center-narrow">
      <h3 class="text-center mb-4">常見問題搜尋</h3>
      <div class="input-group mb-5">
        <input v-model.trim="q" @keyup.enter="doSearch" class="form-control"
               placeholder="請輸入關鍵字（例：退款、取消訂單、付款失敗）"/>
        <button class="btn btn-search" @click="doSearch">搜尋</button>
      </div>
    </div>

    <!-- 搜尋結果（保持不動） -->
    <div class="center-narrow" v-if="q && searched">
      <!-- ... -->
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
import { getFaqList, searchFaq /*, suggestFaq*/ } from './api/csfaq'
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
      quickActions: [
  {
    text: '追蹤我的訂單',
    to: '/orders/track',
    svg: `<svg width="52" height="52" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path d="M3 7h13l3 5v5h-3"></path>
            <circle cx="7.5" cy="18.5" r="1.5"></circle>
            <circle cx="17.5" cy="18.5" r="1.5"></circle>
          </svg>`
  },
  {
    text: '修改或取消訂單',
    to: '/orders/manage',
    svg: `<svg width="52" height="52" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <rect x="3" y="3" width="18" height="14" rx="2"></rect>
            <path d="M3 9h18"></path>
          </svg>`
  },
  {
    text: '忘記密碼',
    to: '/account/forgot',
    svg: `<svg width="52" height="52" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <rect x="3" y="11" width="18" height="10" rx="2"></rect>
            <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
          </svg>`
  },
  {
    text: '客服服務',
    to: '/cs/faq',
    svg: `<svg width="52" height="52" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path d="M21 15a4 4 0 0 1-4 4h-3l-4 3v-3H7a4 4 0 0 1-4-4V9a4 4 0 0 1 4-4h10a4 4 0 0 1 4 4z"></path>
          </svg>`
  }
]

    }
  },
  mounted() { this.loadCategories() },
  methods: {
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
    if (!this.q?.trim()) return
    this.loading = true
    try {
    const list = await searchFaq(this.q.trim())
    this.results = list
    this.searched = true
    } finally {
      this.loading = false
    }
  },
  hl(text) {
    if (!this.q || !text) return text
    const esc = this.q.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
    return String(text).replace(new RegExp(esc, 'gi'), m => `<mark>${m}</mark>`)
  }
}
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

</style>
