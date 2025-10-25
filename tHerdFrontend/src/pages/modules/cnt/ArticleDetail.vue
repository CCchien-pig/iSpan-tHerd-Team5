<template>
  <div class="container py-4" v-if="article">
    <!-- 返回列表 + 分享 -->
    <div class="d-flex align-items-center justify-content-between mb-3">
      <button class="btn teal-reflect-button text-white" @click="goBack">← 返回文章列表</button>
      <div class="d-flex align-items-center gap-3">
        <span class="text-muted small d-none d-sm-inline">分享：</span>
        <button class="btn btn-sm btn-outline-secondary" @click="shareFacebook" title="分享到 Facebook">
          <i class="bi bi-facebook"></i>
        </button>
        <button class="btn btn-sm btn-outline-secondary" @click="shareLine" title="分享到 LINE">
          <i class="bi bi-line"></i>
        </button>
      </div>
    </div>

    <!-- Banner / Title -->
    <div class="rounded-3 p-4 mb-3" style="background:#e9f6f6;">
      <h1 class="m-0 main-color-green-text">{{ article.title }}</h1>
      <p class="text-muted mb-0">{{ formatDate(article.publishedDate) }}</p>
    </div>

    <!-- TOC：頂部橫向（可收合；非 sticky；顯示節數） -->
    <div class="toc-bar bg-light rounded-3 p-2 mb-3">
      <button
        class="btn btn-sm teal-reflect-button text-white"
        type="button"
        @click="toggleToc"
        aria-controls="tocPanel"
        :aria-expanded="toc.open ? 'true' : 'false'"
      >
        📖 {{ toc.open ? `收起目錄（共 ${toc.headings.length} 節）` : `顯示目錄（共 ${toc.headings.length} 節）` }}
      </button>

      <transition name="fade">
        <div v-show="toc.open" id="tocPanel" class="mt-2">
          <div class="d-flex flex-wrap gap-2">
            <button
              v-for="(h, idx) in toc.headings"
              :key="idx"
              class="btn btn-sm toc-item"
              :class="{ active: h.id === toc.activeId }"
              @click="onTocClick(h.id)"
            >
              <span class="me-1" v-if="h.level===2">H2｜</span>
              <span class="me-1" v-else>H3｜</span>
              <span class="text-truncate d-inline-block" style="max-width:220px">{{ h.text }}</span>
            </button>
          </div>
        </div>
      </transition>
    </div>

    <!-- 內容區（帶付費遮罩） -->
    <div class="position-relative">
      <div class="article-content" id="article-body-start" ref="contentRef">
        <!-- 逐塊渲染：richtext / image / cta -->
        <div v-for="(block, index) in displayBlocks" :key="index" class="mb-4">
          <!-- RichText：修正相對圖片路徑後用 v-html 輸出 -->
          <div
            v-if="block.blockType === 'richtext' && block.content"
            class="richtext-block"
            v-html="safeHtml(block.content)"
          ></div>

          <!-- Image：補完整網址後顯示 -->
          <div v-else-if="block.blockType === 'image' && block.content">
            <img :src="absoluteImageUrl(block.content)" class="img-fluid rounded my-3" />
          </div>

          <!-- ✅ CTA Card（卡片款；綠色主題；IG 彩色 Icon；外部/內部自動判斷） -->
          <div v-else-if="block.blockType === 'cta'" class="cta-card p-4 text-center">
            <h4 v-if="ctaPayload(block).title" class="cta-title main-color-green-text mb-2">
              {{ ctaPayload(block).title }}
            </h4>
            <p v-if="ctaPayload(block).desc" class="cta-desc text-muted mb-3">
              {{ ctaPayload(block).desc }}
            </p>

            <button class="btn teal-reflect-button text-white cta-button px-4 py-2" @click="() => openCta(block)">
              <!-- IG：使用彩色 SVG -->
              <span v-if="ctaType(ctaPayload(block).url) === 'ig'" class="cta-icon" v-html="igIconSvg"></span>

              <!-- 外部連結：box-arrow-up-right；內部/預設：arrow-right -->
              <i
                v-else
                class="me-2"
                :class="ctaType(ctaPayload(block).url) === 'external' ? 'bi bi-box-arrow-up-right' : 'bi bi-arrow-right'"
              ></i>

              {{ ctaPayload(block).text || '瞭解更多' }}
            </button>
          </div>
          <!-- ✅ CTA END -->
        </div>
      </div>

      <!-- 遮罩：未解鎖時顯示 -->
      <div
        v-if="!canViewFullContent"
        class="content-mask d-flex flex-column justify-content-center align-items-center text-center p-4"
      >
        <p class="mb-3 fw-bold">此內容需登入付費解鎖</p>
        <div class="d-flex gap-2">
          <button type="button" class="btn teal-reflect-button text-white" @click="onLogin">登入</button>
          <button type="button" class="btn btn-outline-secondary" @click="onPurchase">去購買</button>
        </div>
      </div>
    </div>

    <!-- Tags：底部（暫時作搜尋導回文章清單） -->
    <div v-if="article.tags && article.tags.length" class="mt-4 pt-3 border-top">
      <h5 class="main-color-green-text mb-2">相關標籤</h5>
      <div class="d-flex flex-wrap gap-2">
        <router-link
          v-for="tag in article.tags"
          :key="tag"
          :to="{ name: 'cnt-articles', query: { tag: tag } }"
          class="badge bg-light main-color-green-text text-decoration-none p-2"
        >
          # {{ tag }}
        </router-link>
      </div>
    </div>

    <!-- 推薦文章 -->
    <div v-if="recommended.length" class="mt-5">
      <h4 class="main-color-green-text mb-3">你可能還想看</h4>
      <div class="row g-3">
        <div class="col-12 col-md-6 col-lg-4" v-for="p in recommended" :key="p.pageId">
          <div class="card h-100 shadow-sm">
            <img :src="p.coverImage" class="card-img-top" :alt="p.title" />
            <div class="card-body d-flex flex-column">
              <h6 class="mb-2 main-color-green-text">{{ p.title }}</h6>
              <div class="mt-auto">
                <router-link
                  :to="{ name: 'cnt-article-detail', params: { id: p.pageId }, query: { scroll: 'body' } }"
                  class="btn btn-sm teal-reflect-button text-white"
                >
                  閱讀更多 →
                </router-link>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>

  <!-- 載入中 / 無資料 -->
  <div v-else class="container py-5 text-center">
    <p class="text-muted">文章載入中，請稍候...</p>
  </div>
</template>

<script setup>
import { ref, onMounted, nextTick, computed, onBeforeUnmount } from "vue";
import { useRoute, useRouter } from "vue-router";
import { getArticleDetail, getArticleList } from "./api/cntService";

const route = useRoute();
const router = useRouter();
const article = ref(null);
const blocks = ref([]);
const canViewFullContent = ref(true); // 後端控制
const contentRef = ref(null);

// TOC 狀態
const toc = ref({ open: false, headings: [], activeId: null });
let observer = null;

// 推薦文章
const recommended = ref([]);

// === 全域導覽列偏移控制 ===
let currentNavbarOffset = 80;
const STICKY_EXTRA = 10; // h2/h3 的 sticky 額外間距，需與 CSS 的 +10px 一致
function getNavbarOffset() {
  const nav = document.querySelector(".navbar.fixed-top, header.fixed-top, nav.fixed-top");
  if (nav) {
    const rect = nav.getBoundingClientRect();
    return rect.height + 5;
  }
  return 80;
}

function scrollToWithOffset(selectorOrId) {
  let target = null;
  if (selectorOrId.startsWith("#") || selectorOrId.startsWith(".")) {
    target = document.querySelector(selectorOrId);
  } else {
    target = document.getElementById(selectorOrId);
  }
  if (!target) return;

  const offset = (currentNavbarOffset || getNavbarOffset()) + STICKY_EXTRA;
  const y = target.getBoundingClientRect().top + window.scrollY - offset;
  window.scrollTo({ top: y, behavior: "smooth" });
}
// ✅ 點 TOC 時即時高亮 + 平滑捲動
function onTocClick(id) {
  toc.value.activeId = id;
  scrollToWithOffset(id);
}

// === 自動重新計算 offset ===
function syncNavbarCssVar() {
  const px = (currentNavbarOffset || getNavbarOffset());
  document.documentElement.style.setProperty('--navbar-height', `${px}px`);
}

function handleResize() {
  currentNavbarOffset = getNavbarOffset();
  syncNavbarCssVar(); // ← 新增：同步到 CSS 變數，sticky 立刻生效
}
window.addEventListener("resize", handleResize);

onBeforeUnmount(() => {
  window.removeEventListener("resize", handleResize);
});

// ==== lifecycle ====
onMounted(async () => {
  // 只在本頁動態載入 Bootstrap Icons
  const existing = document.head.querySelector('link[href*="bootstrap-icons"]');
  if (!existing) {
    const link = document.createElement("link");
    link.rel = "stylesheet";
    link.href = "https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css";
    document.head.appendChild(link);
  }

  const pageId = route.params.id;
  const res = await getArticleDetail(pageId);
  if (res) {
    canViewFullContent.value = res.canViewFullContent ?? true;
    if (res.data) {
      article.value = res.data;
      blocks.value = Array.isArray(res.data.blocks) ? res.data.blocks : [];
    }
  }
  
  await nextTick();

  // ✅ 若從列表/首頁帶入 scroll=body，進入就捲到正文
  if (route.query.scroll === "body") {
    setTimeout(() => {
      scrollToWithOffset(".rounded-3.p-4.mb-3 h1");
    }, 300);
  }
  buildHeadings();
  await loadRecommended();
  syncNavbarCssVar();       // 進頁就把 --navbar-height 設準
  setupStickyAssist();      // 啟用加強版 sticky / 高亮同步（下一步定義）
});

onBeforeUnmount(() => {
  if (observer) observer.disconnect();
});

// ==== computed（付費遮罩時顯示部分內容）====
const displayBlocks = computed(() => {
  if (canViewFullContent.value) return blocks.value;
  const MAX_RICHTEXT = 2;
  const out = [];
  let richCount = 0;
  for (const b of blocks.value) {
    if (b.blockType === "richtext" && b.content) {
      out.push(b);
      if (++richCount >= MAX_RICHTEXT) break;
    } else if (b.blockType === "image" && b.content) {
      out.push(b);
    } else if (b.blockType === "cta" && b.content) {
      // 付費未解鎖時也允許展示 CTA（若你希望隱藏，移除此行）
      out.push(b);
    }
  }
  return out.length ? out : blocks.value.slice(0, 1);
});

// ==== methods ====

/** 
 * 加強版：同時處理
 * 1) H2/H3 在貼頂時加上 .is-stuck（陰影）
 * 2) TOC 高亮依「視窗頂端 + offset」就近原則更新
 */
function setupStickyAssist() {
  const root = contentRef.value;
  if (!root) return;
  if (!root || !root.querySelectorAll) return;
  const headers = Array.from(root.querySelectorAll("h2, h3"));
  if (headers.length === 0) return;

  // 2-1) 利用 scroll 事件，依「誰最貼近頂部（含 offset）」做為 active
  const onScroll = () => {
    const offset = currentNavbarOffset || getNavbarOffset() + STICKY_EXTRA;
    let activeId = headers[0].id;

    for (const h of headers) {
      const top = h.getBoundingClientRect().top - offset - 4; // 貼頂略過 4px
      if (top <= 0) activeId = h.id; else break;
    }
    toc.value.activeId = activeId;

    // 2-2) 視覺：誰正在貼頂就加 .is-stuck
    headers.forEach((h) => {
      const top = h.getBoundingClientRect().top - offset;
      if (top <= 1 && top > -1 * (h.offsetHeight || 32)) {
        h.classList.add("is-stuck");
      } else {
        h.classList.remove("is-stuck");
      }
    });
  };

  // 初始化與監聽
  onScroll();
  window.addEventListener("scroll", onScroll, { passive: true });

  // 卸載時移除監聽，避免重複與記憶體外洩
  onBeforeUnmount(() => {
    window.removeEventListener("scroll", onScroll);
  });
}

function goBack() {
  if (window.history.length > 1) router.back();
  else router.push({ name: "cnt-articles" });
}

function currentUrl() {
  try {
    return window.location.href;
  } catch {
    return "";
  }
}
function shareFacebook() {
  const url = encodeURIComponent(currentUrl());
  const t = encodeURIComponent(article.value?.title || "");
  window.open(`https://www.facebook.com/sharer/sharer.php?u=${url}&quote=${t}`, "_blank", "noopener,noreferrer");
}
function shareLine() {
  const url = encodeURIComponent(currentUrl());
  const t = encodeURIComponent(article.value?.title || "");
  window.open(`https://social-plugins.line.me/lineit/share?url=${url}&text=${t}`, "_blank", "noopener,noreferrer");
}

function safeHtml(html) {
  if (!html) return "";
  let fixed = html.replace(/src=["']..\/..\/file\?id=/g, 'src="https://localhost:7103/file?id=');
  fixed = fixed.replace(/src=["']\/uploads\//g, 'src="https://localhost:7103/uploads/');
  return fixed;
}

function absoluteImageUrl(path) {
  if (!path) return "";
  if (/^https?:\/\//i.test(path)) return path;
  if (path.startsWith("/uploads/")) return `https://localhost:7103${path}`;
  if (path.startsWith("../../file?id=")) return path.replace("../../file?id=", "https://localhost:7103/file?id=");
  return path;
}

// TOC
function buildHeadings() {
  toc.value.headings = [];
  const root = contentRef.value;
  if (!root) return;
  const hs = root.querySelectorAll("h2, h3");
  let i = 0;
  hs.forEach((el) => {
    const text = (el.textContent || "").trim();
    if (!text) return;
    let id = el.getAttribute("id");
    if (!id) {
      id = `h-${slugify(text)}-${i++}`;
      el.setAttribute("id", id);
    }
    toc.value.headings.push({ id, level: el.tagName.toLowerCase() === "h2" ? 2 : 3, text });
  });
}

function toggleToc() {
  toc.value.open = !toc.value.open;
}

// 推薦文章：同分類 + 第一個 tag
async function loadRecommended() {
  try {
    const cat = article.value?.categoryName || "";
    const tag = (article.value?.tags || [])[0] || "";
    const keyword = tag || cat || "";
    const res = await getArticleList({ q: keyword, page: 1, pageSize: 10 });
    let pool = (res.items || []).map(wireToCamel).filter((x) => x.pageId !== article.value?.pageId);
    let pick = pool.filter((x) => x.categoryName === cat);
    if (pick.length < 3 && tag) {
      pick = pick.concat(pool.filter((x) => (x.tags || []).includes(tag) && !pick.find((p) => p.pageId === x.pageId)));
    }
    if (pick.length < 3) {
      for (const x of pool) {
        if (!pick.find((p) => p.pageId === x.pageId)) pick.push(x);
        if (pick.length >= 3) break;
      }
    }
    recommended.value = pick.slice(0, 3);
  } catch {
    recommended.value = [];
  }
}

function slugify(s) {
  return s
    .toLowerCase()
    .replace(/[\s\/]+/g, "-")
    .replace(/[^a-z0-9\-]/g, "")
    .replace(/\-+/g, "-")
    .replace(/^\-|\-$/g, "");
}

// ===== CTA 支援（卡片款） =====

// 解析 CTA（因 content 可能是 JSON 字串）
function ctaPayload(block) {
  if (!block) return {};
  if (block._cta) return block._cta;
  let obj = {};
  try {
    if (typeof block.content === "string") obj = JSON.parse(block.content);
    else if (block.content && typeof block.content === "object") obj = block.content;
  } catch {
    obj = {};
  }
  block._cta = obj; // 簡單快取，避免重複 JSON.parse
  return obj;
}

// 判斷 CTA 類型：ig / external / internal
function ctaType(url) {
  if (!url) return "internal";
  if (/instagram\.com/i.test(url)) return "ig";
  if (/^https?:\/\//i.test(url) && !/localhost:7103/i.test(url)) return "external";
  return "internal";
}

// 開啟 CTA：外部 => 新視窗；內部 => router
function openCta(block) {
  const c = ctaPayload(block);
  if (!c?.url) return;
  if (ctaType(c.url) === "internal") router.push(c.url);
  else window.open(c.url, "_blank", "noopener,noreferrer");
}

// IG 彩色 Icon（inline SVG）
const igIconSvg = `
<!-- Instagram App Icon · 24x24 · Rounded Square · Polished -->
<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 512 512" class="me-2 align-text-bottom" aria-hidden="true">
  <defs>
    <!-- 官方高飽和漸層 -->
    <linearGradient id="igGradApp" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%"   stop-color="#FEDA75"/>
      <stop offset="25%"  stop-color="#FA7E1E"/>
      <stop offset="50%"  stop-color="#D62976"/>
      <stop offset="75%"  stop-color="#962FBF"/>
      <stop offset="100%" stop-color="#4F5BD5"/>
    </linearGradient>

    <!-- 極輕微的內陰影，增加量感 -->
    <filter id="innerSoftApp" x="-20%" y="-20%" width="140%" height="140%">
      <feOffset dx="0" dy="2"/>
      <feGaussianBlur stdDeviation="6" result="b"/>
      <feComposite in="SourceAlpha" in2="b" operator="arithmetic" k2="-1" k3="1" result="inner"/>
      <feColorMatrix in="inner" type="matrix"
        values="0 0 0 0 0
                0 0 0 0 0
                0 0 0 0 0
                0 0 0 0.18 0"/>
      <feComposite in="SourceGraphic" in2="inner" operator="over"/>
    </filter>

    <!-- 上方柔光條，營造 App 圖標質感 -->
    <linearGradient id="topGlossApp" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0%"   stop-color="rgba(255,255,255,0.45)"/>
      <stop offset="60%"  stop-color="rgba(255,255,255,0.08)"/>
      <stop offset="100%" stop-color="rgba(255,255,255,0)"/>
    </linearGradient>
  </defs>

  <!-- 背景：圓角方形（約 25% 圓角）+ 漸層 + 內陰影 -->
  <rect x="0" y="0" width="512" height="512" rx="110" fill="url(#igGradApp)" filter="url(#innerSoftApp)"/>

  <!-- 上方柔光（低調一點的高光帶） -->
  <rect x="16" y="16" width="480" height="240" rx="96" fill="url(#topGlossApp)"/>

  <!-- 相機主體：圓角方形白色外框（官方為線條感） -->
  <!-- 位置/圓角/線寬皆微調，盡量貼近官方比例 -->
  <rect x="128" y="128" width="256" height="256" rx="76"
        fill="none" stroke="#FFFFFF" stroke-width="28" stroke-linejoin="round"/>

  <!-- 鏡頭：官方樣式為實心白色圓（非空心圈） -->
  <circle cx="256" cy="256" r="66" fill="#FFFFFF"/>

  <!-- 取景器小白點（右上角） -->
  <circle cx="344" cy="168" r="22" fill="#FFFFFF"/>
</svg>
`;



// ===== 付費遮罩 CTA（示範用）=====
function onLogin() {
  alert("請登入以解鎖內容");
}
function onPurchase() {
  alert("購買流程尚未設計，先以 DB 設定為全免費");
}

// utils
function wireToCamel(x) {
  return {
    pageId: x.pageId ?? x.PageId,
    title: x.title ?? x.Title,
    slug: x.slug ?? x.Slug,
    excerpt: x.excerpt ?? x.Excerpt,
    coverImage: absoluteImageUrl(x.coverImage ?? x.CoverImage),
    categoryName: x.categoryName ?? x.CategoryName,
    publishedDate: x.publishedDate ?? x.PublishedDate,
    isPaidContent: x.isPaidContent ?? x.IsPaidContent,
    tags: x.tags ?? x.Tags ?? [],
  };
}

function formatDate(d) {
  try {
    const dt = new Date(d);
    if (Number.isNaN(dt.getTime())) return "";
    return dt.toLocaleDateString();
  } catch {
    return "";
  }
}
</script>

<style scoped>
/* 動畫 */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

/* 文章內容排版 */
.article-content {
  line-height: 1.85;
  color: #333;
  position: relative; /* ✅ 讓 sticky 的 top 有參考點 */
  z-index: 0;
}
/* 1) 統一用 CSS 變數表示導覽列高度，sticky 直接吃這個值 */
:global(:root) {
  --navbar-height: 80px; /* ✅ 變數全域生效，sticky 才會動 */
}

/* 2) 確保富文本容器不破壞 sticky 行為 */
.richtext-block {
  position: relative; /* sticky 的祖先不能全是 static */
  overflow: visible;  /* 不能把 sticky 的區域裁掉 */
}

/* 3) 讓 h2/h3 真的 sticky 並蓋在文字上方 */
.article-content h2,
.article-content h3 {
  position: sticky;
  top: calc(var(--navbar-height) + 10px);
  z-index: 10;
  background: #fff;
  padding: 0.25rem 0;
  margin-top: 1.5rem;
  margin-bottom: 1rem;
  line-height: 1.5;
  color: var(--main-color-green, #007078);
  transition: box-shadow 0.2s ease, background 0.2s ease;
}

/* 4) 視覺回饋（可選）：真正「貼住」頂端時加陰影 */
.article-content h2.is-stuck,
.article-content h3.is-stuck {
  background: #f8fdfd; /* ✅ 貼頂時背景微變色，更明顯 */
  box-shadow: 0 2px 4px rgba(0,0,0,0.08);
}

.article-content p {
  margin-bottom: 1rem;
}
.article-content img {
  max-width: 100%;
  height: auto;
  display: block;
}

/* TOC 外觀 */
.toc-bar {
  border: 1px solid #e6e6e6;
}
.toc-item {
  background: #fff;
  border: 1px solid #e6e6e6;
  color: #007078;
}
.toc-item.active {
  background: #e9f6f6;
  border-color: #9bd5d5;
  color: #005a60;
  font-weight: 600;
}

/* 付費遮罩 */
.content-mask {
  position: absolute;
  inset: 0;
  background: linear-gradient(
    180deg,
    rgba(255, 255, 255, 0) 10%,
    rgba(255, 255, 255, 0.92) 40%,
    rgba(255, 255, 255, 1) 70%
  );
  backdrop-filter: blur(1px);
  pointer-events: auto;
}

/* 分享 icon 大小微調 */
.bi {
  font-size: 1.05rem;
}

/* ✅ CTA Card 風格（綠色主題；淡綠陰影或灰色邊框） */
.cta-card {
  background: #fff;
  border: 1px solid #e8f4f4;              /* 淡綠邊框 */
  border-radius: 16px;
  box-shadow: 0 6px 18px rgba(0, 112, 120, 0.08), 0 2px 6px rgba(0, 0, 0, 0.04); /* 綠色系淡陰影 */
}
.cta-title {
  line-height: 1.35;
}
.cta-desc {
  line-height: 1.6;
}
.cta-button {
  border-radius: 12px;
  font-weight: 600;
}
.cta-icon :deep(svg) {
  /* 讓彩色 IG 圖標與文字對齊 */
  vertical-align: -2px;
}

/* RWD 微調 */
@media (max-width: 576px) {
  .toc-bar button {
    font-size: 0.85rem;
  }
}
</style>
