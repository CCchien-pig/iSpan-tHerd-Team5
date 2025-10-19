<template>
  <div>
    <!-- 🌿 Hero -->
    <section class="hero-nature text-center py-5 position-relative overflow-hidden">
      <div class="hero-overlay"></div>
      <div class="container position-relative">
        <h1 class="display-5 fw-bold main-color-white-text">健康 × 資訊分析平台</h1>
        <p class="lead mb-4 main-color-white-text">探索健康知識，打造科學飲食生活</p>
        <p class="mx-auto main-color-white-text" style="max-width: 680px;">
          本平台整合健康文章與營養資料，結合視覺化圖表與內容知識，幫助你理解食材營養，建立更智慧的飲食選擇。
        </p>
        <div class="mt-4 d-flex justify-content-center gap-3 flex-wrap">
          <button class="btn teal-reflect-button btn-hero-teal" @click="scrollTo('articles')" title="前往健康文章精選">
            開始閱讀文章
          </button>
          <button class="btn silver-reflect-button btn-hero-silver" @click="scrollTo('nutrition')" title="前往營養分析介紹">
            進入營養分析
          </button>
        </div>
      </div>
    </section>

    <!-- 📰 健康文章精選 -->
    <section id="articles" class="py-5 main-color-white">
      <div class="container">
        <div class="d-flex align-items-center gap-3 mb-3">
          <h2 class="m-0 main-color-green-text">健康文章精選</h2>
          <span class="badge rounded-pill text-bg-light" v-if="articles.length">{{ articles.length }} 篇</span>
          <router-link :to="{ name: 'cnt-articles' }" class="btn btn-sm btn-outline-secondary ms-auto main-color-green-text bg-light" title="查看更多文章">
            查看全部 →
          </router-link>
        </div>

        <div v-if="loading" class="text-center py-5 text-muted">文章載入中…</div>
        <div v-else-if="!articles.length" class="text-center py-5 text-muted">目前尚無文章</div>

        <div v-else class="row g-4">
          <div class="col-12 col-md-6 col-lg-4" v-for="(a, index) in articles" :key="a.pageId ?? index">
            <div class="card h-100 shadow-sm article-card fade-in">
              <div class="ratio ratio-16x9 overflow-hidden rounded-top">
              <img
                :src="
                  !a.coverImage || 
                  a.coverImage.includes('placeholder') || 
                  a.coverImage.includes('cover00') ||
                  a.coverImage.includes('cover01') // 若預設用的是 cover03.png
                    ? a.randomCover 
                    : absoluteImageUrl(a.coverImage)
                "
                class="card-img-top object-cover"
                :alt="a.title"
                @error="onImgError"
              />
              </div>
              <div class="card-body d-flex flex-column">
                <div class="d-flex align-items-center justify-content-between">
                  <span class="badge rounded-pill bg-light main-color-green-text">{{ a.categoryName || '未分類' }}</span>
                  <span v-if="a.isPaidContent" class="badge rounded-pill bg-warning text-dark">付費</span>
                </div>

                <h5 class="mt-2 mb-1 main-color-green-text line-clamp-2">{{ a.title }}</h5>
                <p class="card-text text-muted small flex-grow-1 line-clamp-3">
                  {{ a.excerpt || '—' }}
                </p>

                <div class="d-flex align-items-center justify-content-between mt-2">
                  <small class="text-muted">
                    <template v-if="displayDate(a.publishedDate)">
                      {{ displayDate(a.publishedDate) }}
                    </template>
                  </small>
                  <router-link :to="{ name: 'cnt-article-detail', params: { id: a.pageId } }" class="btn btn-sm teal-reflect-button text-white">
                    閱讀更多 →
                  </router-link>
                </div>
              </div>
            </div>
          </div>
        </div>

      </div>
    </section>

    <!-- 🥗 營養分析介紹 -->
    <section id="nutrition" class="py-5">
      <div class="container">
        <h2 class="main-color-green-text mb-3">營養分析模組</h2>
        <p class="text-muted" style="max-width: 760px;">
          探索不同食材的營養成分，支援多樣比較與圖表分析。你可以一次比較多種食材，
          透過雷達圖或長條圖更直覺地理解營養差異。
        </p>
        <router-link to="/cnt/nutrition" class="btn btn-outline-success">前往營養分析 →</router-link>
      </div>
    </section>

    <router-view></router-view>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { getArticleList } from './api/cntService' // 依你的專案實際路徑


const loading = ref(false)
const articles = ref([])

/** 平滑滾動到區塊（Hero 兩顆主按鈕專用） */
function scrollTo(sectionId) {
  const el = document.getElementById(sectionId)
  if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

/** 🔹 品牌插畫封面池（放在 /public/images/） */
const coverPool = [
  '/images/cover00.png', // 
  '/images/cover01.png', // 健康飲食
  '/images/cover02.png', // 健康知識
  '/images/cover03.png', // 自然療癒
  '/images/cover04.png', // 
  '/images/cover05.png', // 營養洞察（繁中新）
  '/images/cover06.png'  // 健康生活
]

/** 🔸 給每篇文章附一張「穩定、互不重複」的封面
 *   - 使用 index 做循環，避免三張拿到同一張
 *   - 若未來大於封面數，會循環使用
 */
function attachRandomCover(list) {
  return list.map((x, index) => ({
    ...x,
    randomCover: coverPool[index % coverPool.length]
  }))
}

/** 文章列表載入：取多一點先過濾，最後擇優取 3 */
async function loadFeaturedArticles() {
  loading.value = true
  try {
    const res = await getArticleList({ page: 1, pageSize: 12 })
    const items = Array.isArray(res?.items) ? res.items : []

    const normalized = items.map(wireToCamel).map(x => ({
      ...x,
      _dt: parseDateSafe(x.publishedDate)
    }))

    // 有效日期優先（desc），無效日期排後
    normalized.sort((a, b) => {
      const aValid = !!a._dt, bValid = !!b._dt
      if (aValid && bValid) return b._dt - a._dt
      if (aValid && !bValid) return -1
      if (!aValid && bValid) return 1
      return 0
    })

// 取前三筆並附加封面（關鍵）
articles.value = attachRandomCover(normalized.slice(0, 3))

// 🟡 查看封面狀態
console.log('🔥 Final Articles:', articles.value)
  } catch (err) {
    console.warn('[CntHome] 文章載入失敗：', err)
    articles.value = []
  } finally {
    loading.value = false
  }
}

/** 後端鍵名 → 駝峰 */
function wireToCamel(x) {
  return {
    pageId: x.pageId ?? x.PageId,
    title: x.title ?? x.Title,
    slug: x.slug ?? x.Slug,
    excerpt: x.excerpt ?? x.Excerpt,
    coverImage: x.coverImage ?? x.CoverImage,
    categoryName: x.categoryName ?? x.CategoryName,
    publishedDate: x.publishedDate ?? x.PublishedDate,
    isPaidContent: x.isPaidContent ?? x.IsPaidContent,
    tags: x.tags ?? x.Tags ?? []
  }
}

/** 解析日期；'0001-01-01' 或不合法 → null */
function parseDateSafe(v) {
  if (!v) return null
  try {
    const dt = new Date(v)
    const year = dt.getUTCFullYear()
    if (!Number.isFinite(dt.getTime()) || year < 1900) return null
    if (String(v).startsWith('0001-01-01')) return null
    return dt
  } catch {
    return null
  }
}

/** 顯示日期（若無效則不顯） */
function displayDate(v) {
  const dt = parseDateSafe(v)
  if (!dt) return ''
  return dt.toLocaleDateString()
}

/** ✅ 只負責把「已有封面」補成完整 URL，不做亂數 */
function absoluteImageUrl(path) {
  if (!path) return null   // ⛔ 不要回傳空字串！
  if (/^https?:\/\//i.test(path)) return path
  if (path.startsWith('/uploads/')) return `https://localhost:7103${path}`
  return path
}

/** ✅ 圖片載入失敗時，改用品牌插畫（穩定一張） */
function onImgError(e) {
  e.target.src = '/images/cover03.png'
}

onMounted(() => {
  loadFeaturedArticles()
})
</script>
<style scoped>
/* ===== Hero：自然感背景 ===== */
.hero-nature {
  position: relative;
  background:
    linear-gradient(160deg, rgba(0, 112, 131, .75) 0%, rgba(0, 112, 131, .55) 40%, rgba(0, 112, 131, .35) 100%),
    url('https://images.unsplash.com/photo-1501004318641-b39e6451bec6?q=80&w=1920&auto=format&fit=crop') center/cover no-repeat;
}
.hero-overlay { position: absolute; inset: 0; background: radial-gradient(60% 60% at 50% 40%, rgba(255,255,255,.08) 0%, rgba(255,255,255,0) 60%); }

/* ===== 卡片 & 圖片：Hover 輕柔浮起 + 微縮放 ===== */
.article-card {
  border: 1px solid #eee;
  transition: transform .25s ease, box-shadow .25s ease, border-color .25s ease;
  will-change: transform, box-shadow;
  border-radius: .5rem;
  overflow: hidden;
}
.article-card .card-img-top {
  transition: transform .35s ease, filter .35s ease;
  background-color: #f7f7f7;  /* 防止載入時空白閃爍 */
}
.article-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 10px 22px rgba(0,0,0,.08), 0 2px 8px rgba(0,0,0,.06);
  border-color: #e9f3ef; /* 淺綠，氣質感 */
}
.article-card:hover .card-img-top {
  transform: scale(1.03);
  filter: contrast(1.02) saturate(1.02);
}

.object-cover { object-fit: cover; }
.fade-in { animation: fadeIn .35s ease-out both; }
@keyframes fadeIn {
  from { opacity: 0; transform: translateY(6px); }
  to   { opacity: 1; transform: translateY(0); }
}

/* 行數裁切 */
.line-clamp-2 { display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
.line-clamp-3 { display: -webkit-box; -webkit-line-clamp: 3; -webkit-box-orient: vertical; overflow: hidden; }

/* Hero 按鈕字色對比（依你規範） */
.btn-hero-teal { color: #f2f2f2 !important; }
.btn-hero-silver { color: #444 !important; }

@media (max-width: 576px) {
  .article-card h5 { font-size: 1.05rem; }
}
</style>