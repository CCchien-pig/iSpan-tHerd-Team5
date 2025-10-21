<template>
  <div class="container py-4">
    <!-- 標題 + 搜尋列 -->
    <div class="d-flex flex-column flex-md-row align-items-md-center gap-3 mb-4">
      <h2 class="m-0 main-color-green-text">健康文章</h2>
      <div class="ms-md-auto w-100" style="max-width:480px;">
        <form @submit.prevent="onSearch">
          <div class="input-group">
            <input v-model.trim="state.q" type="search" class="form-control border-main-color-green" placeholder="搜尋文章關鍵字…" />
            <button class="btn teal-reflect-button text-white" type="submit">搜尋</button>
          </div>
          <small v-if="route.query.tag" class="text-muted">
            目前依標籤搜尋：
            <span class="badge rounded-pill text-bg-light">{{ route.query.tag }}</span>
            <a href="javascript:;" @click="clearTag" class="ms-2">清除</a>
          </small>
        </form>
      </div>
    </div>

    <!-- 🔽 滾動定位起點 -->
    <div id="article-list-start"></div>

    <!-- 分類 Tabs -->
    <div class="mb-4">
      <div class="d-flex flex-wrap gap-2">
        <button
          :class="['btn', !currentCategory ? 'teal-reflect-button text-white' : 'btn-outline-secondary main-color-green-text bg-light']"
          @click="setCategory(null)"
        >
          全部 ({{ state.totalDisplay }})
        </button>
        <button
          v-for="cat in state.categories"
          :key="cat.name"
          :class="['btn', currentCategory === cat.name ? 'teal-reflect-button text-white' : 'btn-outline-secondary bg-light main-color-green-text']"
          @click="setCategory(cat.name)"
        >
          {{ cat.name }} ({{ cat.count }})
        </button>
      </div>
    </div>

    <!-- 清單 / 載入中 / 無資料 -->
    <div v-if="state.loading" class="text-center py-5">載入中…</div>

    <div v-else-if="state.items.length === 0" class="text-center py-5">
      <p class="mb-1">找不到符合的文章</p>
      <small class="text-muted">試試其他關鍵字或切換分類</small>
    </div>

    <div v-else class="row g-4">
      <div v-for="a in state.items" :key="a.pageId" class="col-12 col-md-6 col-lg-4">
        <div class="card h-100 shadow-sm">
          <img :src="a.coverImage" class="card-img-top" :alt="a.title" />
          <div class="card-body d-flex flex-column">
            <div class="d-flex align-items-center justify-content-between mb-2">
              <span class="badge rounded-pill bg-light main-color-green-text">{{ a.categoryName || '未分類' }}</span>
              <span v-if="a.isPaidContent" class="badge rounded-pill bg-warning text-dark">付費</span>
            </div>
            <h5 class="card-title mb-2 main-color-green-text">{{ a.title }}</h5>
            <p class="card-text text-muted small flex-grow-1">{{ a.excerpt }}</p>
            <div class="d-flex align-items-center justify-content-between mt-2">
              <small class="text-muted">{{ formatDate(a.publishedDate) }}</small>
              <router-link :to="detailTo(a)" class="btn btn-sm teal-reflect-button text-white">
                閱讀更多 →
              </router-link>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 分頁 -->
    <nav v-if="state.totalDisplay > state.pageSize" class="mt-4">
      <ul class="pagination justify-content-center">
        <li :class="['page-item', { disabled: state.page === 1 }]">
          <a class="page-link silver-reflect-button" href="javascript:;" @click="goPage(state.page - 1)">上一頁</a>
        </li>
        <li class="page-item disabled">
          <span class="page-link">第 {{ state.page }} / {{ totalPages }} 頁（共 {{ state.totalDisplay }} 筆）</span>
        </li>
        <li :class="['page-item', { disabled: state.page >= totalPages }]">
          <a class="page-link silver-reflect-button" href="javascript:;" @click="goPage(state.page + 1)">下一頁</a>
        </li>
      </ul>
    </nav>
  </div>
</template>

<script setup>
import { onMounted, reactive, computed } from "vue";
import { useRoute, useRouter } from "vue-router";
import { getArticleList } from "./api/cntService";

const route = useRoute();
const router = useRouter();

const state = reactive({
  items: [],
  total: 0,
  totalDisplay: 0,
  categories: [],
  page: Number(route.query.page || 1),
  pageSize: 9,
  q: route.query.q ? String(route.query.q) : "",
  loading: false
});

const currentCategory = computed(() => route.query.category || null);
const totalPages = computed(() => Math.max(1, Math.ceil(state.totalDisplay / state.pageSize)));

onMounted(async () => {
  await fetchList();
  // ✅ 精準滾動到卡片起點
  if (route.query.scroll === 'list') {
    setTimeout(() => {
      const target = document.getElementById('article-list-start');
      if (target) target.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }, 300);
  }
});

async function fetchList() {
  state.loading = true;
  try {
    const { items, total, page, pageSize } = await getArticleList({
      q: state.q, page: state.page, pageSize: state.pageSize
    });

    state.items = (items || []).map(wireToCamel);

    if ((total === 0 || total == null) && state.items.length > 0) {
      state.total = state.items.length;
      state.totalDisplay = state.items.length;
    } else {
      state.total = total;
      state.totalDisplay = total;
    }

    state.page = page || 1;
    state.pageSize = pageSize || 9;

    const map = new Map();
    state.items.forEach(a => {
      const cat = a.categoryName || "未分類";
      map.set(cat, (map.get(cat) || 0) + 1);
    });
    state.categories = Array.from(map, ([name, count]) => ({ name, count }));

    applyCategoryFilter();
  } catch (err) {
    console.error(err);
    state.items = [];
    state.totalDisplay = 0;
  } finally {
    state.loading = false;
  }
}

function applyCategoryFilter() {
  if (!currentCategory.value) return;
  state.items = state.items.filter(a => a.categoryName === currentCategory.value);
}

function setCategory(name) {
  router.replace({ query: { ...route.query, category: name, page: 1, scroll: 'list' } });
  state.page = 1;
  fetchList();
}

function onSearch() {
  router.replace({ query: { ...route.query, q: state.q || undefined, category: undefined, page: 1, scroll: 'list' } });
  state.page = 1;
  fetchList();
}

function clearTag() {
  const { tag, ...rest } = route.query;
  router.replace({ query: { ...rest, q: undefined, page: 1, scroll: 'list' } });
  state.q = "";
  state.page = 1;
  fetchList();
}

function goPage(p) {
  if (p < 1 || p > totalPages.value) return;
  state.page = p;
  router.replace({ query: { ...route.query, page: p, scroll: 'list' } });
  fetchList();
}

function detailTo(a) {
  // ✅ 帶上 scroll=body，詳情頁會自動捲到正文
  return { name: "cnt-article-detail", params: { id: a.pageId }, query: { scroll: 'body' } };
}

function wireToCamel(x) {
  return {
    pageId: x.pageId ?? x.PageId,
    title: x.title ?? x.Title,
    slug: x.slug ?? x.Slug,
    excerpt: x.excerpt ?? x.Excerpt,
    coverImage: x.coverImage ?? x.CoverImage,
    categoryName: x.categoryName ?? x.CategoryName,
    publishedDate: x.publishedDate ?? x.PublishedDate,
    isPaidContent: x.isPaidContent ?? x.IsPaidContent
  };
}

function formatDate(d) {
  try {
    const dt = new Date(d);
    if (Number.isNaN(dt.getTime())) return "";
    return dt.toLocaleDateString();
  } catch { return ""; }
}
</script>

<style scoped>
.card-title { line-height: 1.35; }
.card-text  { line-height: 1.7; }
.border-main-color-green { border-color: rgb(0, 112, 131) !important; }
</style>
