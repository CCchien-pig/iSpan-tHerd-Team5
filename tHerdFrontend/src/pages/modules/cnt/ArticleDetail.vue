<template>
  <div class="container py-4" v-if="article">
    <!-- Banner / Title -->
    <div class="rounded-3 p-4 mb-3" style="background: #e9f6f6;">
      <h1 class="m-0 main-color-green-text">{{ article.title }}</h1>
      <p class="text-muted mb-0">{{ formatDate(article.publishedDate) }}</p>
    </div>

    <!-- TOC：頂部橫向（可折疊，類 Medium） -->
    <div class="toc-bar bg-light rounded-3 p-2 mb-3">
      <button
        class="btn btn-sm teal-reflect-button text-white"
        type="button"
        @click="toggleToc"
        aria-controls="tocPanel"
        :aria-expanded="toc.open ? 'true' : 'false'"
      >
        📖 {{ toc.open ? '收起目錄' : '顯示目錄' }}
      </button>

      <transition name="fade">
        <div v-show="toc.open" id="tocPanel" class="mt-2">
          <div class="d-flex flex-wrap gap-2">
            <button
              v-for="(h, idx) in toc.headings"
              :key="idx"
              class="btn btn-sm btn-outline-secondary main-color-green-text bg-white"
              @click="scrollToAnchor(h.id)"
            >
              <span v-if="h.level===2">H2｜</span>
              <span v-else>H3｜</span>
              {{ h.text }}
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
  </div>

  <!-- 載入中 / 無資料 -->
  <div v-else class="container py-5 text-center">
    <p class="text-muted">文章載入中，請稍候...</p>
  </div>
</template>

<script setup>
import { ref, onMounted, nextTick, computed } from "vue";
import { useRoute, useRouter } from "vue-router";
import { getArticleDetail } from "./api/cntService";

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
  headings: [] // [{ id, level, text }]
});

// ==== lifecycle ====
onMounted(async () => {
  const pageId = route.params.id;
  const res = await getArticleDetail(pageId);
  // console.log("detail api:", res);

  if (res) {
    canViewFullContent.value = res.canViewFullContent ?? true;
    if (res.data) {
      article.value = res.data;
      blocks.value = Array.isArray(res.data.blocks) ? res.data.blocks : [];
    }
  }

  await nextTick();
  buildHeadings(); // 解析 H2/H3 建立 TOC
});

// ==== computed：依權限切換顯示的 blocks（未解鎖顯示前幾段）====
const displayBlocks = computed(() => {
  if (canViewFullContent.value) return blocks.value;

  // 取前 N 個 richtext 區塊（或合併成片段）
  const MAX_RICHTEXT = 2;
  const out = [];
  let count = 0;
  for (const b of blocks.value) {
    if (b.blockType === "richtext" && b.content && count < MAX_RICHTEXT) {
      out.push(b);
      count++;
    } else if (b.blockType === "image") {
      // 預覽可選擇顯示/不顯示圖片；這裡先顯示一張
      if (out.length && out[out.length - 1].blockType === "image") continue;
      out.push(b);
      if (count >= MAX_RICHTEXT) break;
    }
  }
  return out.length ? out : blocks.value.slice(0, 1);
});

// ==== methods ====
// 修正 RichText 中的 img 路徑（../../file?id= → 絕對路徑）
function safeHtml(html) {
  if (!html) return "";
  // 替換 ../../file?id= 開頭為後端完整路徑
  let fixed = html.replace(/src=["']..\/..\/file\?id=/g, 'src="https://localhost:7103/file?id=');
  // 也處理 /uploads/xxxx.jpg 這種相對路徑
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

  // 先給所有 H2/H3 建立 id（若 richtext 區塊含 h2/h3）
  const hs = root.querySelectorAll("h2, h3");
  let i = 0;
  hs.forEach((el) => {
    const text = (el.textContent || "").trim();
    if (!text) return;
    // 產生穩定 id
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
  // router.push({ name: 'login' })
  alert("請登入以解鎖內容");
}
function onPurchase() {
  // router.push({ name: 'purchase' })
  alert("購買流程尚未設計，先以 DB 設定為全免費");
}

// ==== utils ====
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
/* TOC 動畫 */
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

/* 付費遮罩 */
.content-mask {
  position: absolute;
  inset: 0;
  background: linear-gradient(180deg, rgba(255,255,255,0) 10%, rgba(255,255,255,.92) 40%, rgba(255,255,255,1) 70%);
  backdrop-filter: blur(1px);
}

/* RWD 微調（手機 TOC 更好點） */
@media (max-width: 576px) {
  .toc-bar button { font-size: .85rem; }
}
</style>
