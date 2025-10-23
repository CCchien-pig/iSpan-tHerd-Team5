<template>
  <div class="container py-4">
    <!-- 標題 + 搜尋列 -->
    <div class="d-flex flex-column flex-md-row align-items-md-center gap-3 mb-4">
      <h2 class="m-0 main-color-green-text">健康文章</h2>
      <div class="ms-md-auto w-100" style="max-width:480px;">
        <form @submit.prevent="onSearch">
          <div class="input-group">
            <input
              v-model.trim="state.q"
              type="search"
              class="form-control border-main-color-green"
              placeholder="搜尋文章標題關鍵字…"
            />
            <button class="btn teal-reflect-button text-white" type="submit">搜尋</button>
          </div>
        </form>
      </div>
    </div>

    <!-- 🔽 滾動定位起點 -->
    <div id="article-list-start"></div>

    <!-- 分類 Tabs（由後端提供） -->
    <div class="mb-4">
      <div class="d-flex flex-wrap gap-2">
        <button
          :class="[
            'btn',
            !currentCategoryId
              ? 'teal-reflect-button text-white'
              : 'btn-outline-secondary main-color-green-text bg-light'
          ]"
          @click="setCategory(null)"
        >
          全部 ({{ state.allCount }})
        </button>
        <button
          v-for="cat in state.categories"
          :key="cat.id"
          :class="[
            'btn',
            currentCategoryId === cat.id
              ? 'teal-reflect-button text-white'
              : 'btn-outline-secondary bg-light main-color-green-text'
          ]"
          @click="setCategory(cat.id)"
        >
          {{ cat.name }} ({{ cat.articleCount }})
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
      <div
        v-for="a in state.items"
        :key="a.pageId"
        class="col-12 col-md-6 col-lg-4"
      >
        <div class="card h-100 shadow-sm">
          <img :src="a.coverImage" class="card-img-top" :alt="a.title" />
          <div class="card-body d-flex flex-column">
            <div class="d-flex align-items-center justify-content-between mb-2">
              <span class="badge rounded-pill bg-light main-color-green-text">{{
                a.categoryName || "未分類"
              }}</span>
              <span
                v-if="a.isPaidContent"
                class="badge rounded-pill bg-warning text-dark"
                >付費</span
              >
            </div>
            <h5 class="card-title mb-2 main-color-green-text">{{ a.title }}</h5>
            <p class="card-text text-muted small flex-grow-1">{{ a.excerpt }}</p>
            <div
              class="d-flex align-items-center justify-content-between mt-2"
            >
              <small class="text-muted">{{ formatDate(a.publishedDate) }}</small>
              <router-link
                :to="detailTo(a)"
                class="btn btn-sm teal-reflect-button text-white"
              >
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
          <a
            class="page-link silver-reflect-button"
            href="javascript:;"
            @click="goPage(state.page - 1)"
            >上一頁</a
          >
        </li>
        <li class="page-item disabled">
          <span class="page-link"
            >第 {{ state.page }} / {{ totalPages }} 頁（共
            {{ state.totalDisplay }} 筆）</span
          >
        </li>
        <li :class="['page-item', { disabled: state.page >= totalPages }]">
          <a
            class="page-link silver-reflect-button"
            href="javascript:;"
            @click="goPage(state.page + 1)"
            >下一頁</a
          >
        </li>
      </ul>
    </nav>
  </div>
</template>

<script setup>
import { onMounted, reactive, computed, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { getArticleList, getArticleCategories } from "@/pages/modules/cnt/api/cntService";

const route = useRoute();
const router = useRouter();

const state = reactive({
  items: [],
  categories: [],
  totalDisplay: 0,
  allCount: 0,          // 👈 新增：全站文章總數
  page: Number(route.query.page || 1),
  pageSize: 12,
  q: route.query.q ? String(route.query.q) : "",
  loading: false,
});

const currentCategoryId = computed(() =>
  route.query.categoryId ? Number(route.query.categoryId) : null
);
const totalPages = computed(() =>
  Math.max(1, Math.ceil(state.totalDisplay / state.pageSize))
);

onMounted(async () => {
  await loadCategories();
  await loadArticles();

  // ✅ 精準滾動到卡片起點
  if (route.query.scroll === "list") {
    setTimeout(() => {
      const target = document.getElementById("article-list-start");
      if (target)
        target.scrollIntoView({ behavior: "smooth", block: "start" });
    }, 300);
  }
});

watch(
  () => route.query.categoryId,
  async () => {
    await loadArticles();
  }
);

async function loadCategories() {
  try {
    const { items } = await getArticleCategories();
    state.categories = items;

    // 🔹 計算全部文章數量
    state.allCount = items.reduce((sum, c) => sum + (c.articleCount || 0), 0);
  } catch (err) {
    console.error("載入分類失敗:", err);
  }
}

async function loadArticles() {
  state.loading = true;
  try {
    const { items, total, page, pageSize } = await getArticleList({
      q: state.q,
      categoryId: currentCategoryId.value,
      page: state.page,
      pageSize: state.pageSize,
    });

    state.items = (items || []).map(wireToCamel);
    state.totalDisplay = total || state.items.length;
    state.page = page || 1;
    state.pageSize = pageSize || 12;
  } catch (err) {
    console.error("載入文章失敗:", err);
    state.items = [];
    state.totalDisplay = 0;
  } finally {
    state.loading = false;
  }
}

function setCategory(id) {
  router.replace({
    query: { ...route.query, categoryId: id || undefined, page: 1, scroll: "list" },
  });
  state.page = 1;
  loadArticles();
}

function onSearch() {
  router.replace({
    query: {
      ...route.query,
      q: state.q || undefined,
      categoryId: currentCategoryId.value || undefined,
      page: 1,
      scroll: "list",
    },
  });
  state.page = 1;
  loadArticles();
}

function goPage(p) {
  if (p < 1 || p > totalPages.value) return;
  state.page = p;
  router.replace({
    query: { ...route.query, page: p, scroll: "list" },
  });
  loadArticles();
}

function detailTo(a) {
  return {
    name: "cnt-article-detail",
    params: { id: a.pageId },
    query: { scroll: "body" },
  };
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
    isPaidContent: x.isPaidContent ?? x.IsPaidContent,
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
.card-title {
  line-height: 1.35;
}
.card-text {
  line-height: 1.7;
}
.border-main-color-green {
  border-color: rgb(0, 112, 131) !important;
}
</style>
