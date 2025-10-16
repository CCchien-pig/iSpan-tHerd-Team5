<template>
  <div class="faq-wrap py-5">
    <!-- 搜尋框（保持不動） -->
    <div class="center-narrow">
      <h3 class="text-center mb-4">常見問題搜尋</h3>
      <div class="input-group mb-5">
        <input v-model.trim="q" @keyup.enter="doSearch" class="form-control"
               placeholder="請輸入關鍵字（例：退款、取消訂單、付款失敗）"/>
        <button class="btn btn-primary" @click="doSearch">搜尋</button>
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
          <div v-for="a in quickActions" :key="a.text" class="col-6 col-sm-3">
            <div class="qcard">
              <div class="qicon" v-html="a.svg"></div>
              <a :href="a.href" class="stretched-link">{{ a.text }}</a>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ❷ 幫助文章（右側 + / −） -->
    <div class="center-narrow">
      <div class="section-title"><span>幫助文章</span></div>

      <div class="accordion" id="faqAccordion">
        <div class="accordion-item" v-for="c in categories" :key="c.categoryId">
          <h2 class="accordion-header">
            <button class="accordion-button collapsed cat-btn" type="button"
                    data-bs-toggle="collapse" :data-bs-target="`#cat-${c.categoryId}`">
              {{ c.categoryName }}
              <span class="pm" aria-hidden="true"></span>
            </button>
          </h2>
          <div :id="`cat-${c.categoryId}`" class="accordion-collapse collapse"
               data-bs-parent="#faqAccordion">
            <div class="accordion-body p-0">
              <ul class="list-group list-group-flush">
                <li v-for="f in c.faqs" :key="f.faqId" class="list-group-item">
                  <a class="link-body-emphasis" data-bs-toggle="collapse" :href="`#faq-${f.faqId}`">
                    {{ f.title }}
                  </a>
                  <div :id="`faq-${f.faqId}`" class="collapse mt-2">
                    <div class="small text-secondary" v-html="f.answerHtml"></div>
                  </div>
                </li>
              </ul>
            </div>
          </div>
        </div>
      </div>

    </div>
  </div>
</template>


<script>
export default {
  name: 'FaqSearch',
  data() {
    return {
      q: '',
      results: [],
      searched: false,
      loading: false,
      categories: [],
      quickActions: [
        { text: '追蹤我的訂單', href: '/orders/track', svg: `<svg width="52" height="52" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M3 7h13l3 5v5h-3"/><circle cx="7.5" cy="18.5" r="1.5"/><circle cx="17.5" cy="18.5" r="1.5"/></svg>` },
        { text: '修改或取消訂單', href: '/orders/manage', svg: `<svg width="52" height="52" viewBox="0 0 24 24" fill="none" stroke="currentColor"><rect x="3" y="3" width="18" height="14" rx="2"/><path d="M3 9h18"/></svg>` },
        { text: '忘記密碼', href: '/account/forgot', svg: `<svg width="52" height="52" viewBox="0 0 24 24" fill="none" stroke="currentColor"><rect x="3" y="11" width="18" height="10" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/></svg>` },
        { text: '客服服務', href: '/cs/contact', svg: `<svg width="52" height="52" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M21 15a4 4 0 0 1-4 4h-3l-4 3v-3H7a4 4 0 0 1-4-4V9a4 4 0 0 1 4-4h10a4 4 0 0 1 4 4z"/></svg>` }
      ]
    }
  },
  mounted() { this.loadCategories() },
  methods: {
    async doSearch() {
      if (!this.q) return
      this.loading = true
      this.searched = true
      try {
        const res = await fetch(`/api/cs/faq/search?q=${encodeURIComponent(this.q)}`)
        this.results = await res.json()
      } finally { this.loading = false }
    },
    async loadCategories() {
      const res = await fetch('/api/cs/faq/list')
      this.categories = await res.json()
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
/* 整體寬度控制：中間置中 */
.center-narrow { max-width: 880px; margin: 0 auto; }
/* 區塊標題（水平置中、小分隔線） */
.section-title { display:flex; align-items:center; gap:.75rem; margin: 8px 0 14px; }
.section-title::before, .section-title::after {
  content:""; flex:1; height:1px; background:#e6e6e6;
}
.section-title > span { white-space:nowrap; color:#333; font-weight:600; }

/* 分類按鈕右側 + 號（模擬 iHerb 風格） */
.cat-btn { position: relative; padding-right: 2.5rem; }
.cat-btn .plus {
  position:absolute; right:1rem; top:50%; transform: translateY(-50%);
  font-size: 1.25rem; line-height:1; color:#666;
}
.accordion-button:not(.collapsed) .plus { transform: translateY(-50%) rotate(45deg); } /* 展開變成 x */

/* 快捷卡片（iHerb 圓形圖示 + 文案） */
.quick-area { background: #f8faf8; padding: 28px 0 48px; margin-top: 36px; }
.qcard {
  background:#fff; border:1px solid #e8ece8; border-radius:16px;
  padding:18px 8px; position:relative; transition:transform .12s ease, box-shadow .12s ease;
}
.qcard:hover{ transform: translateY(-2px); box-shadow:0 6px 16px rgba(0,0,0,.06); }
.qicon{
  width:72px; height:72px; margin:0 auto 8px; border:1px solid #e8ece8; border-radius:50%;
  display:flex; align-items:center; justify-content:center; color:#0c8a4b;
}
.qcard a{ display:block; font-weight:600; color:#0f5132; text-decoration:none; }
.qcard a:hover{ text-decoration:underline; }
</style>
