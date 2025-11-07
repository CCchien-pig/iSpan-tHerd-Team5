<template>
  <div class="container py-4">
    <!-- ⭐ 新增一層 header box -->
    <div class="tag-header-box mb-3">
      <!-- ⭐ 頂部列：返回 / 標籤資訊 / 排序 -->
      <div class="d-flex flex-wrap align-items-center gap-3">
        <!-- 返回文章 -->
        <button class="btn teal-reflect-button text-white" @click="goBack">
          ← 返回文章
        </button>

      <!-- 中間：標籤種類 pill + 相關商品標題 + 標籤名稱 + 總件數 -->
      <div class="flex-grow-1 text-center">
        <!-- 標籤種類小 pill -->
        <div
          class="tag-type-label mb-1"
          v-if="tagInfo.tagTypeName || tagInfo.tagName"
        >
          <span class="tag-pill">
            {{ tagInfo.tagTypeName || '商品標籤' }}
          </span>
        </div>

        <!-- 主標題：只說「相關商品」 -->
        <h2 class="main-color-green-text mb-1">
          相關商品
        </h2>

        <!-- 副標：顯示標籤名稱 -->
        <p class="tag-subtext text-muted small mb-1" v-if="tagInfo.tagName">
          基於標籤
          <span class="tag-name-highlight">#{{ tagInfo.tagName }}</span>
        </p>

        <p v-if="total" class="tag-subtext text-muted small mb-0">
          共 {{ total }} 件商品
          <span v-if="tagInfo.description"> · {{ tagInfo.description }}</span>
        </p>
      </div>


        <!-- 右側：排序 -->
        <div class="sort-wrapper ms-auto">
          <label class="small text-muted me-2">排序方式</label>
          <select
            v-model="sort"
            class="form-select form-select-sm sort-select"
            @change="onSortChange"
          >
            <option value="default">預設排序</option>
            <option value="price-asc">價格：低到高</option>
            <option value="price-desc">價格：高到低</option>
          </select>
        </div>
      </div>
    </div>

    <hr class="tag-divider" />

    <!-- 商品清單 -->
    <div v-if="items.length" class="row g-4">
      <div
        class="col-6 col-md-4 col-lg-3"
        v-for="prod in items"
        :key="prod.productId"
      >
        <div class="card-shell h-100 rounded-3 border bg-white">
          <ProductCard
            :product="prod"
          />
        </div>
      </div>
    </div>

    <!-- 沒資料 -->
    <div v-else class="text-center text-muted py-5">
      這個標籤目前沒有可顯示的商品
    </div>
    
    <!-- 分頁：膠囊樣式 + 輸入跳頁 -->
    <nav
      v-if="items.length && totalPages > 1"
      class="mt-4 d-flex justify-content-center"
    >
      <ul class="pagination clean-pill align-items-stretch">
        <!-- 上一頁 -->
        <li class="page-item" :class="{ disabled: page === 1 }">
          <a
            class="page-link nav-pill-left"
            href="javascript:;"
            @click="goPage(page - 1)"
          >
            上一頁
          </a>
        </li>

        <!-- 中間：目前頁資訊 -->
        <li class="page-item disabled">
          <span class="page-link nav-pill-mid">
            第 {{ page }} / {{ totalPages }} 頁（共 {{ total }} 件）
          </span>
        </li>

        <!-- 新增：輸入跳頁 -->
        <li class="page-item">
          <span class="page-link nav-pill-mid page-input-wrapper">
            <span class="me-1 d-none d-md-inline">跳至</span>
            <input
              v-model.number="pageInput"
              type="number"
              min="1"
              :max="totalPages"
              class="page-input"
              @keyup.enter="goPageByInput"
            />
            <span class="ms-1">頁</span>
            <button
              type="button"
              class="btn btn-sm teal-reflect-button text-white ms-2"
              @click="goPageByInput"
            >
              Go
            </button>
          </span>
        </li>

        <!-- 下一頁（最後一段，當膠囊右邊） -->
        <li class="page-item" :class="{ disabled: page >= totalPages }">
          <a
            class="page-link nav-pill-right"
            href="javascript:;"
            @click="goPage(page + 1)"
          >
            下一頁
          </a>
        </li>
      </ul>
    </nav>
  </div>
</template>


<script setup>
import { ref, onMounted, watch } from "vue";
import { useRouter } from "vue-router";
import axios from "axios";
import ProductCard from "@/components/modules/prod/card/ProductCard.vue";

const props = defineProps({
  tagId: {
    type: [String, Number],
    required: true,
  },
});

const router = useRouter();

// 商品 + 分頁
const items = ref([]);
const page = ref(1);
const pageSize = ref(24);
const total = ref(0);
const totalPages = ref(1);
// 輸入頁碼用
const pageInput = ref(1);

// 標籤資訊
const tagInfo = ref({
  tagId: 0,
  tagName: "",
  tagTypeName: "",
  description: "",
});
// 排序
const sort = ref("default"); // default | price-asc | price-desc

// ✅ 把 CNT / 後端回來的 badge 轉成 ProductBadge 會吃的中文字
function mapBadgeName(p) {
  const raw = (p.badgeName || p.badge || "").trim();
  if (!raw) return "";

  const lower = raw.toLowerCase();

  // 這裡依照你後端實際給的值去對：英文代碼就轉成中文
  if (["discount", "特價!"].includes(lower)) return "特價!";
  if (["only", "只限 therd", "只限 therd", "只限 tHerd"].includes(lower)) return "只限 tHerd";
  if (["new", "新品搶先購"].includes(lower)) return "新品搶先購";
  if (["try", "好物試用!"].includes(lower)) return "好物試用!";

  // 其他就原樣丟給 ProductBadge，用灰色樣式
  return raw;
}

function fixImageUrl(path) {
  if (!path) return "/images/no-image.png";
  if (/^https?:\/\//i.test(path)) return path;
  if (path.startsWith("/uploads/")) {
    return `https://localhost:7103${path}`;
  }
  if (path.startsWith("././file?id=")) {
    return path.replace("././file?id=", "https://localhost:7103/file?id=");
  }
  return `https://localhost:7103${path}`;
}

// 取得標籤資訊
async function loadTagInfo() {
  try {
    const { data } = await axios.get(`/api/cnt/tags/${props.tagId}`);
    tagInfo.value = data || {};
  } catch (err) {
    console.error("載入標籤資訊失敗", err);
    tagInfo.value = {
      tagId: 0,
      tagName: "",
      tagTypeName: "",
      description: "",
    };
  }
}

// 從後端抓「這個標籤底下的商品」
async function loadProducts() {
  try {
    const res = await axios.get(`/api/cnt/tags/${props.tagId}/products`, {
      params: {
        page: page.value,
        pageSize: pageSize.value,
        sort: sort.value,
      },
    });

    const rawTotal = res.data?.total ?? 0;
    const rawItems = res.data?.items ?? [];

    total.value = rawTotal;
    totalPages.value = Math.max(1, Math.ceil(total.value / pageSize.value));

    items.value = rawItems.map((p) => {
      const imgCandidate =
        p.imageUrl || p.mainImageUrl || p.coverImage || p.image || "";

      return {
        // === ✅ ProductCard 會用到的 key，全部對齊 ===
        productId: p.productId,
        mainSkuId: p.mainSkuId ?? p.productId ?? null,

        productName: p.productName ?? p.name ?? "",
        shortDesc: p.shortDesc || "",
        brandName: p.brandName || "",

        // 徽章：先經過 mapBadgeName，才會吃到紅/綠/藍/粉色樣式
        badgeName: mapBadgeName(p),

        // 評分＋評價數：後端叫 avgRating / reviewCount or rating / reviews 都兼容
        // avgRating: p.avgRating ?? p.rating ?? //沒有四捨五入
        avgRating: Math.floor(Number(p.avgRating ?? p.rating ?? 0) || 0),
        reviewCount: p.reviewCount ?? p.reviews ?? 0,

        // 價格：優先用 prod DTO 那種欄位，退一步用 CNT 的 salePrice / price / originalPrice
        billingPrice:
        // 1. 後端如果有算好的 BillingPrice 就先用它
        p.billingPrice ??
        // 2. 沒有就用優惠價
        p.salePrice ??
        // 3. 再沒有就用主商品單價
        p.unitPrice ??
        // 4. 全都沒有就用原價
        p.listPrice ??
        0,

      listPrice:
        // 1. 原價
        p.listPrice ??
        // 2. 沒有原價就用單價
        p.unitPrice ??
        // 3. 再不行用 BillingPrice / SalePrice 當作原價顯示
        p.billingPrice ??
        p.salePrice ??
        0,
        imageUrl: fixImageUrl(imgCandidate),
      };
    });

    // 小技巧：如果還是看到 0，可以在這裡 console.log(rawItems[0]) 看後端真正的欄位名稱
    // console.log("[TagProducts] sample item =", rawItems[0])
  } catch (err) {
    console.error("載入商品失敗", err);
    total.value = 0;
    totalPages.value = 1;
    items.value = [];
  }
}

// 排序下拉有變更時
function onSortChange() {
  page.value = 1;
  loadProducts();   // ⭐ 重打後端，套新的排序
}

// 分頁
function goPage(newPage) {
  if (newPage < 1) return;
  if (newPage > totalPages.value) return;
  page.value = newPage;
  loadProducts();
}

//手動輸入分頁
function goPageByInput() {
  let target = Number(pageInput.value);
  if (!Number.isFinite(target)) return;

  if (target < 1) target = 1;
  if (target > totalPages.value) target = totalPages.value;

  if (target === page.value) {
    pageInput.value = target;
    return;
  }

  page.value = target;
  pageInput.value = target;
  loadProducts();
}

// 返回文章
function goBack() {
  router.back();
}

// 初次載入：標籤資訊 + 商品列表
onMounted(() => {
  loadTagInfo();
  loadProducts();
});

// ✅ 這裡新增：同步 page → pageInput
watch(page, (val) => {
  pageInput.value = val || 1;
});

// 切換 tagId 時
watch(
  () => props.tagId,
  () => {
    page.value = 1;
    sort.value = "default";
    loadTagInfo();
    loadProducts();
  }
);
</script>


<style scoped>
/* ================= Tag Header / 排序區 ================= */

/* 上方 header 外框（淡漸層卡片） */
.tag-header-box {
  background: linear-gradient(135deg, #f5fbfb, #ffffff);
  border: 1px solid #d8eceb;
  border-radius: 0.9rem;
  padding: 0.75rem 1rem;
  margin-bottom: 1rem;
}

/* 標籤種類小 pill 上方那行文字 */
.tag-type-label {
  font-size: 0.8rem;
}

/* 標籤種類 pill（例如：商品標籤） */
.tag-pill {
  display: inline-flex;
  align-items: center;
  padding: 0.15rem 0.75rem;
  border-radius: 999px;
  background: #e9f6f5; /* 淺綠藍系，呼應主綠色 */
  color: #005a60;
}

/* 標題底下小字（件數、描述） */
.tag-subtext {
  line-height: 1.4;
}

/* 「基於標籤 #魚油」 這顆 chip */
.tag-name-highlight {
  display: inline-flex;
  align-items: center;
  padding: 0.05rem 0.5rem;
  margin-left: 0.15rem;
  border-radius: 999px;
  background-color: #f0faf9;
  color: #00796b;
  font-weight: 600;
  font-size: 0.85rem;
}

/* 排序區塊寬度控制 */
.sort-wrapper {
  min-width: 180px;
}

.sort-select {
  min-width: 150px;
}

/* header 下方分隔線 */
.tag-divider {
  margin-top: 0.25rem;
  margin-bottom: 1.25rem;
  border-color: #e5e7eb;
}

/* ================= 商品卡樣式 ================= */

/* 外殼卡片：圓角、邊框、hover 陰影 */
.card-shell {
  border: 1px solid #ddd;
  background-color: #fff;
  transition: box-shadow 0.2s ease, transform 0.15s ease;
  border-radius: 0.75rem;
  overflow: hidden;
  height: 100%;
  display: flex;
  align-items: stretch;
  justify-content: stretch;
}

.card-shell:hover {
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
  transform: translateY(-2px);
}

/* 讓內部 ProductCard 完全貼合外殼，不再有自己的邊框 */
.card-shell :deep(.product-card) {
  border: 0;
  border-radius: 0;
  box-shadow: none;
  margin: 0;
  width: 100%;
  max-width: 100%;
}

/* ================= 分頁膠囊外觀 ================= */

/* 三顆按鈕合在一起像膠囊（上一頁 | 中間資訊 | 下一頁） */
.pagination.clean-pill {
  list-style: none;
  padding-left: 0;
  margin-bottom: 0;

  display: flex;
  flex-wrap: nowrap;
  align-items: stretch;
  border-radius: 999px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
  overflow: hidden;
  background-color: #fff;
  border: 1px solid #ccc;
}

/* 拿掉 bootstrap 預設 li 的間距 */
.pagination.clean-pill .page-item {
  margin: 0;
}

.pagination.clean-pill .page-item.disabled .page-link {
  opacity: 0.4;
  pointer-events: none;
}

/* 每一段膠囊的文字樣式（全部垂直置中） */
.pagination.clean-pill .page-link {
  border: 0;
  border-radius: 0;
  background: transparent;
  /*     上    左右    下   */
  padding: 1.2rem 0.85rem 0.2rem;
  font-size: 0.9rem;
  line-height: 1.1;
  white-space: nowrap;
  color: #004f4a;

  display: flex;
  align-items: center;
  justify-content: center;
}

/* 頁碼輸入框造型 */
.page-input {
  width: 3rem;
  text-align: center;
  border: 1px solid #ccc;
  border-radius: 999px;
  /*   上     左右     下  */
  padding: 0.2rem 0.4rem 0.2rem;
  font-size: 0.85rem;
  outline: none;
}

/* 左中右段邊界線 */
.nav-pill-left {
  border-right: 1px solid #ccc;
}

.nav-pill-mid {
  border-right: 1px solid #ccc;
}

/* 「跳至第 X 頁」這塊：往上微調位置 */
.page-input-wrapper {
  gap: 0.25rem;
  position: relative;
  top: -8px;   /* 👈 往上移 2px，數字可以自己微調 -1 / -3 看效果 */
}


/* 讓 number input 不要有上下箭頭（可加可不加） */
.page-input::-webkit-outer-spin-button,
.page-input::-webkit-inner-spin-button {
  -webkit-appearance: none;
  margin: 0;
}
.page-input[type="number"] {
  -moz-appearance: textfield;
}
</style>

