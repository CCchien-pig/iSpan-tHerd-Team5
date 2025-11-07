<template>
  <div class="container py-4" v-if="article">
    <!-- 返回列表 + 分享 -->
    <div class="d-flex align-items-center justify-content-between mb-3">
      <!-- 標題上方操作列 -->
    <div class="d-flex flex-wrap align-items-center gap-2 mb-3">
      <button
        class="btn btn-sm teal-reflect-button text-white"
        @click="goBack"
      >
        ← 返回文章列表
      </button>

      <!-- 只在登入時顯示第二顆，但跟第一顆同一排靠左 -->
      <router-link
        v-if="isLogin"
        :to="{ name: 'cnt-my-articles' }"
        class="btn btn-sm teal-reflect-button text-white ms-2"
      >
        查看我買過的文章 →
      </router-link>
    </div>
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
    <div id="article-top" class="rounded-3 p-4 mb-3" style="background:#e9f6f6;">
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

    <!-- 內容區：只放實際文章（上面預覽，底部用漸層收尾） -->
    <div
      class="article-wrapper position-relative"
      :class="{ 'has-paywall': !canViewFullContent }"
    >
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

          <!-- ✅ CTA Card -->
          <div v-else-if="block.blockType === 'cta'" class="cta-card p-4 text-center">
            <h4 v-if="ctaPayload(block).title" class="cta-title main-color-green-text mb-2">
              {{ ctaPayload(block).title }}
            </h4>
            <p v-if="ctaPayload(block).desc" class="cta-desc text-muted mb-3">
              {{ ctaPayload(block).desc }}
            </p>

            <button class="btn teal-reflect-button text-white cta-button px-4 py-2" @click="() => openCta(block)">
              <span v-if="ctaType(ctaPayload(block).url) === 'ig'" class="cta-icon" v-html="igIconSvg"></span>
              <i
                v-else
                class="me-2"
                :class="ctaType(ctaPayload(block).url) === 'external'
                  ? 'bi bi-box-arrow-up-right'
                  : 'bi bi-arrow-right'"
              ></i>
              {{ ctaPayload(block).text || '瞭解更多' }}
            </button>
          </div>
          <!-- ✅ CTA END -->
        </div>
      </div>
    </div>

    <!-- 🔒 付費卡片：獨立區塊（大鎖 + 登入 + 立即購買），放在內容和標籤中間 -->
    <div
          v-if="!canViewFullContent"
          class="paywall-box my-5 d-flex flex-column justify-content-center align-items-center text-center p-4"
        >
          <div class="mask-lock-icon mb-3">
            <i class="bi bi-lock-fill"></i>
          </div>

          <p v-if="formatArticlePrice()" class="mb-1 text-muted">
            單篇價格：
            <span class="fw-bold text-danger">NT$ {{ formatArticlePrice() }}</span>
          </p>

          <p class="mb-3 fw-bold">此內容需登入付費解鎖</p>

          <div class="d-flex flex-wrap justify-content-center gap-2">
            <button
              v-if="!isLogin"
              class="btn teal-reflect-button text-white"
              @click="onLogin"
            >
              登入
            </button>

            <button
              class="btn teal-reflect-button text-white"
              :disabled="isPurchasing"
              @click="onPurchase"
            >
              {{ purchaseButtonText }}
          </button>
      </div>
    </div>

    <!-- Tags：底部（暫時作搜尋導回文章清單） -->
    <div v-if="article.tags && article.tags.length" class="mt-5 pt-4 border-top">
      <h4 class="main-color-green-text mb-2">相關標籤</h4>
      <div class="d-flex flex-wrap gap-2">
      <router-link
        v-for="t in article.tags"
        :key="t.tagId"
        :to="{ name: 'cnt-tag-products', params: { tagId: t.tagId } }"
        class="badge main-color-green-text text-decoration-none p-1 tag-badge"
      >
        # {{ t.tagName }}
      </router-link>
      </div>
    </div>

    <!-- 推薦文章 -->
    <div v-if="recommended.length" class="mt-5">
      <h4 class="main-color-green-text mb-3">你可能還想看</h4>
      <div class="row g-3">
        <div class="col-12 col-md-6 col-lg-4" v-for="p in recommended" :key="p.pageId">
          <div class="card h-100 shadow-sm">
            <div class="card-body d-flex flex-column">

              <!-- 類別 Badge -->
              <div class="mb-2 text-start">
                <span
                  v-if="p.categoryName"
                  class="badge rounded-pill bg-light main-color-green-text"
                  style="border:1px solid rgba(0,128,0,.2); font-size:.8rem; font-weight:500;"
                >
                  {{ p.categoryName }}
                </span>
              </div>

              <!-- 標題 -->
              <h5 class="mb-2 main-color-green-text fw-bold" style="line-height:1.4;">
                {{ p.title }}
              </h5>

              <!-- 摘要 / 前幾行 -->
              <p class="text-muted flex-grow-1" style="line-height:1.6;">
                {{ p.excerpt }}
              </p>

              <!-- 日期 + 閱讀更多 -->
              <div class="d-flex justify-content-between align-items-end mt-3">
                <small class="text-muted">{{ formatDate(p.publishedDate) }}</small>

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
import { ref, onMounted, watch, nextTick, computed, onBeforeUnmount } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useAuthStore } from '@/stores/auth';
import { getArticleDetail, getArticleList } from "@/pages/modules/cnt/api/cntService";

// 建立購買流程增加
import cntArticlesApi from '@/pages/modules/cnt/api/cntArticlesApi'
const auth = useAuthStore()
const isLogin = computed(() => auth.isAuthenticated)
const isPurchasing = ref(false)
const lastPurchase = ref(null)
// ---------------

const route = useRoute();
const router = useRouter();
const article = ref(null);
const blocks = ref([]);
const canViewFullContent = ref(true); // 後端控制
const contentRef = ref(null);
// 推薦文章
const recommended = ref([]);
// TOC 狀態
const toc = ref({ open: false, headings: [], activeId: null });
let observer = null;

// ⬇️⬇️⬇️ 在這裡貼上（新加的）⬇️⬇️⬇️
const purchaseButtonText = computed(() => {
  if (isPurchasing.value) return '建立訂單中…';

  // 後端可能回傳 price 或 Price，兩種都試
  const raw = article.value?.price ?? article.value?.Price;
  if (raw == null) return '立即購買全文';

  const num = Number(raw);
  if (!Number.isFinite(num) || num <= 0) return '立即購買全文';

  const formatted = num.toLocaleString('zh-TW', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  });

  return `立即購買全文（NT$${formatted}）`;
});

function formatArticlePrice() {
  const raw = article.value?.price ?? article.value?.Price;
  if (raw == null) return '';
  const num = Number(raw);
  if (!Number.isFinite(num) || num <= 0) return '';
  return num.toLocaleString('zh-TW', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  });
}
// ⬆️⬆️⬆️ 新加的結束 ⬆️⬆️⬆️

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

function scrollToWithOffset(id, adjust = 0) {
  const el = document.getElementById(id);
  if (!el) return;

  const scroller = getScrollParent(contentRef.value);
  const isWindow = scroller === window;
  const scTop = isWindow ? 0 : scroller.getBoundingClientRect().top;
  const current = isWindow ? window.scrollY : scroller.scrollTop;
  const offset = (currentNavbarOffset || getNavbarOffset()) + STICKY_EXTRA + adjust;

  const targetAbs = el.getBoundingClientRect().top - scTop + current;
  const to = Math.max(0, targetAbs - offset);

  if (isWindow) {
    window.scrollTo({ top: to, behavior: 'smooth' });
  } else {
    scroller.scrollTo({ top: to, behavior: 'smooth' });
  }
}

// 模組層級旗標（放在 <script setup> 最上方）
let isJumping = false;
let jumpTargetId = null;
let jumpTimer = null;

function onTocClick(id) {
  toc.value.activeId = id;  // 先高亮
  isJumping = true;
  jumpTargetId = id;

  scrollToWithOffset(id);   // 你的平滑捲動函式

  // 安全閥，最多 2 秒自動解鎖避免卡住
  clearTimeout(jumpTimer);
  jumpTimer = setTimeout(() => {
    isJumping = false;
    jumpTargetId = null;
  }, 2000);
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

// lifecycle：抓文章、建 TOC、啟動 stickyAssist
let disposeSticky = null; //加一個變數來接收清理函式，並統一清理
// ==== lifecycle ====
onMounted(async () => {
  // 只要負責載入 icon
  const existing = document.head.querySelector('link[href*="bootstrap-icons"]');
  if (!existing) {
    const link = document.createElement("link");
    link.rel = "stylesheet";
    link.href = "https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css";
    document.head.appendChild(link);
  }

  // 然後交給 loadPage() 做真正的載入與定位
  await loadPage();
});


// ⭐ 監聽 URL 上的文章 id 變了沒
watch(
  () => route.params.id,
  async () => {
    await loadPage();
  }
);

onBeforeUnmount(() => {
  window.removeEventListener("resize", handleResize);

  if (observer) observer.disconnect();
  if (disposeSticky) disposeSticky();
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
// === 工具：找出實際可滾動容器（window 或內層 div） ===
function getScrollParent(el) {
  let node = el;
  while (node && node !== document.body) {
    const style = getComputedStyle(node);
    const overflowY = style.overflowY;
    const canScroll =
      (overflowY === "auto" || overflowY === "scroll") &&
      node.scrollHeight > node.clientHeight;
    if (canScroll) return node;
    node = node.parentElement;
  }
  return window; // 找不到就退回 window
}

/** 
 * 加強版：同時處理
 * 1) H2/H3 在貼頂時加上 .is-stuck（陰影）
 * 2) TOC 高亮依「視窗頂端 + offset」就近原則更新
 */
function setupStickyAssist() {
  const root = contentRef.value;
  if (!root) return;
  

  const headers = Array.from(root.querySelectorAll('h2, h3')).filter(h => h.id);
  if (!headers.length) return;

  // 取得實際的 scroller（可能是 window，也可能是某個 div）
  const scroller = getScrollParent(root);
  console.log("[TOC] 實際滾動容器 =", scroller, "isWindow =", scroller === window);
  const isWindow = scroller === window;

  // 把「視窗座標」換成「scroller 座標」的量法
  const getScrollTop = () => (isWindow ? window.scrollY : scroller.scrollTop);
  const getScrollerTop = () => (isWindow ? 0 : scroller.getBoundingClientRect().top);

  // 你的 navbar 高度 offset（保持原本的函式/變數）
  const getOffset = () => (currentNavbarOffset || getNavbarOffset()) + STICKY_EXTRA;

  // 量錨點的「絕對 Y（以 scroller 的座標系）」
  let anchorTops = [];
  const measure = () => {
    const scTop = getScrollerTop();
    const sTop = getScrollTop();
    anchorTops = headers.map(h => ({
      id: h.id,
      // 🚩 把 header 的視窗 top 轉成 scroller 座標：rect.top - scrollerRect.top + scrollTop
      y: h.getBoundingClientRect().top - scTop + sTop,
      txt: (h.textContent || '').trim().slice(0, 30),
    }));
    // console.table(anchorTops); // 需要時打開
  };

  // 跳轉偏好
  const NEAR_RANGE = 120;

  const onScroll = () => {
    const offset = getOffset();
    const pos = getScrollTop() + offset;

    // 跳轉期間，沒到站就不覆蓋 active
    if (isJumping && jumpTargetId) {
      const el = document.getElementById(jumpTargetId);
      if (el) {
        const targetAbs = el.getBoundingClientRect().top - getScrollerTop() + getScrollTop();
        if (Math.abs(targetAbs - pos) <= 6) {
          isJumping = false;
          clearTimeout(jumpTimer);
        } else {
          return;
        }
      }
    }

    // 近目標優先（避免第一顆 sticky 抢回）
    if (jumpTargetId) {
      const el = document.getElementById(jumpTargetId);
      if (el) {
        const targetAbs = el.getBoundingClientRect().top - getScrollerTop() + getScrollTop();
        if (Math.abs(targetAbs - pos) <= NEAR_RANGE) {
          toc.value.activeId = jumpTargetId;
          // 貼頂視覺（仍用視窗 rect 計）
          headers.forEach(h => {
            const top = h.getBoundingClientRect().top - offset;
            if (top <= 1 && top > -1 * (h.offsetHeight || 32)) h.classList.add('is-stuck');
            else h.classList.remove('is-stuck');
          });
          return;
        }
      }
    }

    // 一般就近判定（用 scroller 座標）
    let activeId = anchorTops[0]?.id;
    for (let i = 0; i < anchorTops.length; i++) {
      if (anchorTops[i].y <= pos + 1) activeId = anchorTops[i].id;
      else break;
    }
    if (activeId) toc.value.activeId = activeId;

    // 視覺貼頂（與原來相同）
    headers.forEach(h => {
      const top = h.getBoundingClientRect().top - offset;
      if (top <= 1 && top > -1 * (h.offsetHeight || 32)) h.classList.add('is-stuck');
      else h.classList.remove('is-stuck');
    });
  };

  // 防抖 remeasure（避免頻繁重算）
  let remeasureTimer = null;
  let remeasurePending = false;
  const remeasure = () => {
    if (remeasurePending) return;
    remeasurePending = true;
    clearTimeout(remeasureTimer);
    remeasureTimer = setTimeout(() => {
      remeasurePending = false;
      measure();
      onScroll();
    }, 80);
  };

  // 初始化
  measure();
  onScroll();

  // 監聽「正確的 scroller」
  const addScroll = () =>
    (isWindow
      ? window.addEventListener('scroll', onScroll, { passive: true })
      : scroller.addEventListener('scroll', onScroll, { passive: true }));
  const removeScroll = () =>
    (isWindow
      ? window.removeEventListener('scroll', onScroll)
      : scroller.removeEventListener('scroll', onScroll));

  addScroll();
  window.addEventListener('resize', remeasure);

  // 🔎 圖片載入/內容變更重新量測（注意不要監聽 attributes，避免自觸發）
  const imgs = root.querySelectorAll('img');
  imgs.forEach(img => {
    if (!img.complete) img.addEventListener('load', remeasure, { once: true });
  });
  const mo = new MutationObserver(remeasure);
  mo.observe(root, { childList: true, subtree: true }); // 不監聽 attributes

  window.addEventListener('load', remeasure);
  setTimeout(remeasure, 150);
  setTimeout(remeasure, 600);

  return () => {
    removeScroll();
    window.removeEventListener('resize', remeasure);
    window.removeEventListener('load', remeasure);
    mo.disconnect();
  };
}

function goBack() {
  const { categoryId, q, page, from } = route.query || {};
  if (from === "list") {
    router.push({
      name: "cnt-articles",
      query: {
        categoryId,
        q,
        page,
        scroll: "title",
      },
    });
  } else {
    // 不確定來源就回標題
    router.push({ name: "cnt-articles", query: { scroll: "title" } });
  }
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
    // 從目前文章抓一些線索
    const catName = article.value?.categoryName || "";
    const firstTagName = article.value?.tags?.[0]?.tagName || "";

    // 我們要嘗試使用的搜尋關鍵字（優先用第一個標籤）
    let keyword = firstTagName || catName || "";

    // 步驟1：用 keyword 去抓候選文章
    let res = await getArticleList({
      q: keyword || undefined,
      page: 1,
      pageSize: 10
    });

    let pool = (res.items || [])
      .map(wireToCamel)
      .filter(x => x.pageId !== article.value?.pageId);

    // 如果第一輪抓不到任何東西，就退而求其次：抓「不過濾的熱門/最新」
    if (!pool.length) {
      const fallbackRes = await getArticleList({
        // 不帶 q，請求一批最常用列表 (你的後端應該是預設排序：最新 / 熱門)
        page: 1,
        pageSize: 10
      });

      pool = (fallbackRes.items || [])
        .map(wireToCamel)
        .filter(x => x.pageId !== article.value?.pageId);
    }

    // 現在 pool 是候選，我們來排序一下，盡量放相關的在前面
    const pick = [];
    for (const x of pool) {
      // 先塞「同分類」或「包含同標籤名稱的」
      const sameCat = catName && x.categoryName === catName;
      const sameTag =
        firstTagName &&
        Array.isArray(x.tags) &&
        x.tags.includes(firstTagName);

      if (sameCat || sameTag) {
        pick.push(x);
      }
      if (pick.length >= 3) break;
    }

    // 如果還不夠 3 篇，拿 pool 其他的來補滿
    for (const x of pool) {
      if (pick.find(p => p.pageId === x.pageId)) continue;
      pick.push(x);
      if (pick.length >= 3) break;
    }

    recommended.value = pick.slice(0, 3);
  } catch (err) {
    console.warn("loadRecommended() 失敗", err);
    recommended.value = [];
  }
}

// 👇 新增這個：把整個載入流程包成一個可重複呼叫的函式
async function loadPage() {
  // 1. 如果上一篇文章已經裝過 sticky 監聽，要先拆掉，避免越疊越多
  if (disposeSticky) {
    disposeSticky();
    disposeSticky = null;
  }

  // 2. 抓目前的 pageId
  const pageId = Number(route.params.id)

  // 3. 從後端拿文章詳情
  const res = await getArticleDetail(pageId);
  console.log('detail API 回傳', res);   // 👈 這行看一下實際回傳
  if (res) {
    canViewFullContent.value = res.canViewFullContent ?? true;
    if (res.data) {
      article.value = res.data;
      blocks.value = Array.isArray(res.data.blocks) ? res.data.blocks : [];
    }
  }

  // 4. 等 DOM 真的畫出來 (h2/h3、richtext…)
  await nextTick();

  // 5. 如果 query 帶 scroll=body，就往正文/標題區捲
  if (route.query.scroll === "body") {
    setTimeout(() => {
      // 你檔案裡現在用的是 "article-top" 當目標錨點，這行沿用
      scrollToWithOffset("article-top", 0);
    }, 300);
  } else {
    // 如果沒有 scroll=body，通常是你從推薦文章跳過來
    // 這時候我們至少應該把畫面捲回頁首，避免還卡在舊文章中段
    window.scrollTo({ top: 0, behavior: "auto" });
  }

  // 6. 重建 TOC 標題們 (h2/h3)
  buildHeadings();

  // 7. 重新抓推薦文章 (它會用 article.value 的分類/標籤去推別篇)
  await loadRecommended();

  // 8. 同步 navbar 高度到 CSS 變數，讓 sticky 正常
  syncNavbarCssVar();

  // 9. 最後重新啟動 stickyAssist (h2/h3 貼頂 + TOC 高亮)
  disposeSticky = setupStickyAssist();
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
  const returnUrl = route.fullPath || route.path || `/cnt/article/${route.params.id}`

  router.push({
    name: 'userlogin',  // ✅ 換成真正存在的 route name
    query: { returnUrl },
  })
}

async function onPurchase() {
  // 1) 沒登入先導去登入
  if (!isLogin.value) {
    router.push({ name: 'userlogin', query: { returnUrl: route.fullPath } });
    return;
  }

  // 2) 防止連點
  if (isPurchasing.value) return;
  isPurchasing.value = true;

  try {
    const pageId = article.value?.pageId || Number(route.params.id);

    // 3) 建立 / 取得訂單（後端會回 PurchaseSummaryDto）
    const summary = await cntArticlesApi.createPurchase(pageId, "LINEPAY");
    console.log("建立訂單成功", summary);
    lastPurchase.value = summary;

    // 4) 取出付款網址（不同命名都試一下）
    const paymentUrl =
      summary.paymentUrl ??
      summary.PaymentUrl ??
      summary.linePayPaymentUrl ??
      null;

    if (!paymentUrl) {
      alert("訂單建立成功，但後端沒有回付款連結，請稍後再試。");
      return;
    }

    // 5) 導去 LINE Pay 付款頁
    window.location.href = paymentUrl;
  } catch (err) {
    console.error("購買失敗", err?.response?.status, err);
    if (err?.response?.status === 401) {
      alert("登入逾時，請重新登入後再購買");
      router.push({ name: "login", query: { returnUrl: route.fullPath } });
    } else {
      alert("購買失敗，請稍後再試");
    }
  } finally {
    isPurchasing.value = false;
  }
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
.toc-bar .toc-item.active { background:#e9f6f6; }

/* 付費遮罩 */
/* 付費預覽效果：有付費牆時，在內容最下方加一層漸層收尾 */
.article-wrapper.has-paywall::after {
  content: "";
  position: absolute;
  left: 0;
  right: 0;
  bottom: 0;
  height: 140px;  /* 想遮多高自己調 */
  background: linear-gradient(
    180deg,
    rgba(255, 255, 255, 0) 0%,
    rgba(255, 255, 255, 0.92) 45%,
    rgba(255, 255, 255, 1) 100%
  );
  pointer-events: none; /* 不擋滑鼠操作 */
}

/* 付費卡片本體（大鎖 + 按鈕那塊） */
.paywall-box {
  max-width: 520px;
  margin: 0 auto;           /* 置中 */
  border-radius: 20px;
  background: #e7e7e7;
  box-shadow: 0 12px 24px rgba(0, 0, 0, 0.4);
}

/* 分享 icon 大小微調 */
.bi {
  font-size: 1.05rem;
}

/* ✅ CTA Card 風格（綠色主題；淡綠陰影或灰色邊框） */
.cta-card {
  background: #fff;
  border: 1px solid #e8f4f4;
  border-radius: 16px;
  /* 陰影加重：位移、模糊、透明度都往上調 */
  box-shadow:
    0 14px 30px rgba(0, 112, 120, 0.28),
    0 4px 12px rgba(0, 0, 0, 0.12);
}
.cta-card:hover {
  box-shadow:
    0 18px 40px rgba(0, 112, 120, 0.32),
    0 6px 16px rgba(0, 0, 0, 0.16);
  transform: translateY(-2px);
  transition: box-shadow 0.2s ease, transform 0.2s ease;
}
.cta-title {
  line-height: 1.35;
}
.cta-desc {
  line-height: 1.6;
}
/* 讓 CTA 按鈕裡的圖示和文字排成一行 */
.cta-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;       /* 圖示和文字中間的距離 */
}

/* 圖示外層 span */
.cta-icon {
  display: inline-flex;
  align-items: center;
}

/* 彩色 IG svg 調整一下尺寸就好，保持在同一行 */
.cta-icon :deep(svg) {
  width: 24px;
  height: 24px;
  vertical-align: middle;
}

/* RWD 微調 */
@media (max-width: 576px) {
  .toc-bar button {
    font-size: 0.85rem;
  }
}

/* 讓原生 #錨點 或 scrollIntoView 也對齊 */
.article-content :where(h2[id], h3[id]) {
  scroll-margin-top: calc(var(--navbar-height) + 10px);
}

.tag-badge {
  font-size: 0.95rem;          /* 字大一點 */
  padding: 0.35rem 0.6rem;

  /* 比 bg-light 再深一點的綠系底色，想更深可以再調 */
  background-color: #d1f0e5;   /* 淺綠 */
}

.mask-lock-icon i {
  font-size: 2.5rem;
  color: #f5a623;   /* 金黃色鎖比較明顯 */
}

.btn-my-articles {
  /* 加重一點陰影，比較有「實體按鈕」感 */
  box-shadow:
    0 3px 0 rgba(0, 0, 0, 0.1),
    0 6px 12px rgba(0, 0, 0, 0.2);
  font-weight: 480;
}

</style>
