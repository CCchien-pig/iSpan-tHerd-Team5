<template>
  <div class="container py-4" v-if="article">
    <!-- 返回列表 + 分享 -->
    <div class="d-flex align-items-center justify-content-between mb-3">
      <button class="btn btn-link p-0 main-color-green-text text-decoration-none" @click="goBack">
        ← 返回文章列表
      </button>
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
              @click="scrollToAnchor(h.id)"
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
      <div class="article-content" ref="contentRef">
        <!-- 逐塊渲染：richtext / image -->
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
        </div>
      </div>

      <!-- 遮罩：未解鎖時顯示 -->
      <div v-if="!canViewFullContent" class="content-mask d-flex flex-column justify-content-center align-items-center text-center p-4">
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
          :to="{ name: 'cnt-articles', query: { tag: tag }}"
          class="badge bg-light main-color-green-text text-decoration-none p-2"
        >
          # {{ tag }}
        </router-link>
      </div>
    </div>

    <!-- 推薦文章：同分類 + Tag 混合（iHerb 風格卡片） -->
    <div v-if="recommended.length" class="mt-5">
      <h4 class="main-color-green-text mb-3">你可能還想看</h4>
      <div class="row g-3">
        <div class="col-12 col-md-6 col-lg-4" v-for="p in recommended" :key="p.pageId">
          <div class="card h-100 shadow-sm">
            <img :src="p.coverImage" class="card-img-top" :alt="p.title" />
            <div class="card-body d-flex flex-column">
              <h6 class="mb-2 main-color-green-text">{{ p.title }}</h6>
              <div class="mt-auto">
                <router-link :to="{ name:'cnt-article-detail', params:{ id: p.pageId }}" class="btn btn-sm teal-reflect-button text-white">
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

// ==== state ====
const route = useRoute();
const router = useRouter();
const article = ref(null);
const blocks = ref([]);
const canViewFullContent = ref(true); // 後端控制
const contentRef = ref(null);

// TOC 狀態
const toc = ref({
  open: false,
  headings: [], // [{ id, level, text }]
  activeId: null
});
let observer = null;

// 推薦文章
const recommended = ref([]);

// ==== lifecycle ====
onMounted(async () => {
  // ✅ 只在本頁動態載入 Bootstrap Icons（避免全站汙染；重複判斷）
  const existing = document.head.querySelector('link[href*="bootstrap-icons"]');
  if (!existing) {
    const link = document.createElement("link");
    link.rel = "stylesheet";
    link.href = "https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css";
    document.head.appendChild(link);
  }

  const pageId = route.params.id;
  const res = await getArticleDetail(pageId);
  console.log("[Detail] API 返回：", res);

  if (res) {
    canViewFullContent.value = res.canViewFullContent ?? true;
    if (res.data) {
      article.value = res.data;
      blocks.value = Array.isArray(res.data.blocks) ? res.data.blocks : [];
    }
  }

  await nextTick();
  buildHeadings();     // 解析 H2/H3 建立 TOC
  setupObserver();     // 啟動 ScrollSpy
  await loadRecommended();
});

onBeforeUnmount(() => {
  if (observer) observer.disconnect();
});

// ==== computed：依權限切換顯示的 blocks（未解鎖顯示前幾段）====
const displayBlocks = computed(() => {
  if (canViewFullContent.value) return blocks.value;
  const MAX_RICHTEXT = 2;
  const out = [];
  let richCount = 0;
  for (const b of blocks.value) {
    if (b.blockType === "richtext" && b.content) {
      out.push(b);
      richCount++;
      if (richCount >= MAX_RICHTEXT) break;
    } else if (b.blockType === "image" && b.content) {
      out.push(b);
    }
  }
  return out.length ? out : blocks.value.slice(0, 1);
});

// ==== methods ====
// 返回列表
function goBack() {
  if (window.history.length > 1) {
    router.back();
  } else {
    router.push({ name: "cnt-articles" });
  }
}

// 分享
function currentUrl() {
  try {
    return window.location.href;
  } catch { return ""; }
}
function shareFacebook() {
  const url = encodeURIComponent(currentUrl());
  const t = encodeURIComponent(article.value?.title || "");
  const share = `https://www.facebook.com/sharer/sharer.php?u=${url}&quote=${t}`;
  window.open(share, "_blank", "noopener,noreferrer");
}
function shareLine() {
  const url = encodeURIComponent(currentUrl());
  const t = encodeURIComponent(article.value?.title || "");
  const share = `https://social-plugins.line.me/lineit/share?url=${url}&text=${t}`;
  window.open(share, "_blank", "noopener,noreferrer");
}

// 修正 RichText 中的 img 路徑（../../file?id= → 絕對路徑）
function safeHtml(html) {
  if (!html) return "";
  let fixed = html.replace(/src=["']..\/..\/file\?id=/g, 'src="https://localhost:7103/file?id=');
  fixed = fixed.replace(/src=["']\/uploads\//g, 'src="https://localhost:7103/uploads/');
  return fixed;
}

// 單一 image block 的相對路徑補全
function absoluteImageUrl(path) {
  if (!path) return "";
  if (/^https?:\/\//i.test(path)) return path;
  if (path.startsWith("/uploads/")) return `https://localhost:7103${path}`;
  if (path.startsWith("../../file?id=")) {
    return path.replace("../../file?id=", "https://localhost:7103/file?id=");
  }
  return path;
}

// 生成 TOC：抓取 contentRef 內的 h2/h3
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
    toc.value.headings.push({
      id,
      level: el.tagName.toLowerCase() === "h2" ? 2 : 3,
      text
    });
  });
  console.log("[TOC] Headings:", toc.value.headings);
}

function toggleToc() {
  toc.value.open = !toc.value.open;
}

function scrollToAnchor(id) {
  const root = contentRef.value;
  if (!root) return;
  const target = root.querySelector(`#${CSS.escape(id)}`);
  if (target) {
    target.scrollIntoView({ behavior: "smooth", block: "start" });
  }
}

// ScrollSpy：監控目前所在章節，套用 active 樣式
function setupObserver() {
  if (observer) observer.disconnect();
  const root = contentRef.value;
  if (!root) return;

  const options = {
    root: null,
    rootMargin: "0px 0px -65% 0px", // 提前切換 active
    threshold: 0
  };
  observer = new IntersectionObserver(handleIntersect, options);
  const hs = root.querySelectorAll("h2, h3");
  hs.forEach(el => observer.observe(el));
}

function handleIntersect(entries) {
  let topMost = null;
  for (const entry of entries) {
    if (entry.isIntersecting) {
      if (!topMost || entry.boundingClientRect.top < topMost.boundingClientRect.top) {
        topMost = entry;
      }
    }
  }
  if (topMost) {
    const id = topMost.target.id;
    toc.value.activeId = id;
    // console.log("[TOC] Active:", id);
  }
}

// 推薦文章：同分類 + Tag 混合
async function loadRecommended() {
  try {
    const cat = article.value?.categoryName || "";
    the_tag: {
      const tag = (article.value?.tags || [])[0] || "";
      const keyword = tag || cat || "";

      // 先用關鍵字抓 10 筆候選
      const res = await getArticleList({ q: keyword, page: 1, pageSize: 10 });
      let pool = (res.items || []).map(wireToCamel);
      // 排除自己
      pool = pool.filter(x => x.pageId !== article.value?.pageId);

      // 優先：同分類
      let pick = pool.filter(x => x.categoryName === cat);
      // 補齊：相同第一個 tag
      if (pick.length < 3 && tag) {
        pick = pick.concat(pool.filter(x => (x.tags || []).includes(tag) && !pick.find(p => p.pageId === x.pageId)));
      }
      // 仍不足：補最新
      if (pick.length < 3) {
        for (const x of pool) {
          if (!pick.find(p => p.pageId === x.pageId)) pick.push(x);
          if (pick.length >= 3) break;
        }
      }
      recommended.value = pick.slice(0, 3);
    }
    console.log("[Recommend] Candidates:", recommended.value);
  } catch (e) {
    console.warn("[Recommend] 無法載入推薦文章：", e);
    recommended.value = [];
  }
}

// 小工具：字串轉 slug
function slugify(s) {
  return s
    .toLowerCase()
    .replace(/[\s\/]+/g, "-")
    .replace(/[^a-z0-9\-]/g, "")
    .replace(/\-+/g, "-")
    .replace(/^\-|\-$/g, "");
}

// CTA（先留空，之後可串登入/購買）
function onLogin() {
  alert("請登入以解鎖內容");
}
function onPurchase() {
  alert("購買流程尚未設計，先以 DB 設定為全免費");
}

// ==== utils ====
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
    tags: x.tags ?? x.Tags ?? []
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
.fade-enter-active, .fade-leave-active { transition: opacity .2s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }

/* 文章內容排版 */
.article-content {
  line-height: 1.85;
  color: #333;
}
.article-content h2,
.article-content h3 {
  color: var(--main-color-green, #007078);
  margin-top: 1.5rem;
  margin-bottom: .5rem;
}
.article-content p { margin-bottom: 1rem; }
.article-content img {
  max-width: 100%;
  height: auto;
  display: block;
}

/* TOC 外觀 */
.toc-bar { border: 1px solid #e6e6e6; }
.toc-item {
  background: #fff;
  border: 1px solid #e6e6e6;
  color: #007078;
}
.toc-item.active {
  background: #e9f6f6; /* 底色高亮 */
  border-color: #9bd5d5;
  color: #005a60;
  font-weight: 600;
}

/* 付費遮罩 */
.content-mask {
  position: absolute;
  inset: 0;
  background: linear-gradient(180deg, rgba(255,255,255,0) 10%, rgba(255,255,255,.92) 40%, rgba(255,255,255,1) 70%);
  backdrop-filter: blur(1px);
  pointer-events: auto;
}

/* 分享 icon 大小微調（使用 Bootstrap Icons 時適用） */
.bi { font-size: 1.05rem; }

@media (max-width: 576px) {
  .toc-bar button { font-size: .85rem; }
}
</style>
