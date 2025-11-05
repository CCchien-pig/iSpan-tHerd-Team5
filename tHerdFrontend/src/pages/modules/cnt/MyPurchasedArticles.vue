<template>
  <div class="container py-4">
    <h2 class="main-color-green-text mb-3">我買過的文章</h2>

    <div v-if="loading" class="text-center py-5 text-muted">載入中…</div>

    <div v-else-if="items.length === 0" class="text-center py-5 text-muted">
      <p class="mb-1">目前還沒有購買紀錄</p>
      <router-link
        :to="{ name: 'cnt-articles', query: { scroll: 'list' } }"
        class="btn teal-reflect-button text-white mt-3"
      >
        去逛逛文章 →
      </router-link>
    </div>

    <div v-else class="row g-3">
      <div class="col-12 col-md-6" v-for="p in items" :key="p.purchaseId">
        <div class="card h-100 shadow-sm">
          <div class="card-body d-flex flex-column">
            <div class="d-flex justify-content-between align-items-center mb-1">
              <span class="badge bg-light main-color-green-text">
                {{ p.categoryName || '文章' }}
              </span>
              <small class="text-muted">
                購買日期：{{ formatDate(p.purchasedDate) }}
              </small>
            </div>

            <h5 class="main-color-green-text mb-2">{{ p.title }}</h5>

            <p class="mb-1">
              單篇價格：
              <strong class="text-danger">{{ p.unitPrice.toFixed(0) }}</strong> 元
            </p>

            <div class="mt-auto d-flex justify-content-end">
              <router-link
                :to="{ name: 'cnt-article-detail', params: { id: p.pageId }, query: { scroll: 'body' } }"
                class="btn btn-sm teal-reflect-button text-white"
              >
                前往閱讀 →
              </router-link>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from "vue";
import { getMyPurchasedArticles } from "@/pages/modules/cnt/api/cntService";

const loading = ref(false);
const items = ref([]);

onMounted(async () => {
  loading.value = true;
  try {
    const data = await getMyPurchasedArticles();
    items.value = Array.isArray(data) ? data : [];
  } catch (err) {
    console.error("取得購買文章列表失敗", err);
    items.value = [];
  } finally {
    loading.value = false;
  }
});

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
