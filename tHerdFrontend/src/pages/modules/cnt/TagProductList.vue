<template>
  <div class="container py-4">
    <!-- 頂部列：返回 / 標題置中 -->
    <div class="d-flex align-items-center mb-4">
      <button class="btn teal-reflect-button text-white me-3" @click="goBack">
        ← 返回文章
      </button>

      <h2 class="flex-grow-1 text-center m-0 main-color-green-text">
        相關商品
      </h2>

      <!-- 右邊放一個固定寬度的空div，讓標題真的置中 -->
      <div style="width:2.5rem;"></div>
    </div>

    <!-- 商品清單 -->
    <div v-if="items.length" class="row g-4">
      <div
        class="col-6 col-md-4 col-lg-3"
        v-for="prod in items"
        :key="prod.productId"
      >
        <!-- 外層殼：加圓角/邊框/hover 陰影，整張可點 -->
        <div
          class="card-shell h-100 rounded-3 border shadow-sm-hover bg-white"
          role="button"
          @click="goDetail(prod.productId)"
        >
          <!-- 使用現成的商品卡 -->
          <ProductCard
            :product="prod"
            @add-to-cart.stop="addToCart"
          />
        </div>
      </div>
    </div>

    <!-- 沒資料 -->
    <div v-else class="text-center text-muted py-5">
      這個標籤目前沒有可顯示的商品
    </div>

    <!-- ⭐ 分頁列：只有在有資料而且總頁數>1時顯示 -->
    <nav
      v-if="items.length && totalPages > 1"
      class="mt-4 d-flex justify-content-center"
    >
      <ul class="pagination clean-pill align-items-stretch">
        <!-- 上一頁 -->
        <li
          class="page-item"
          :class="{ disabled: page === 1 }"
        >
          <a
            class="page-link nav-pill-left"
            href="javascript:;"
            @click="goPage(page - 1)"
          >
            上一頁
          </a>
        </li>

        <!-- 中間資訊 -->
        <li class="page-item disabled">
          <span class="page-link nav-pill-mid">
            第 {{ page }} / {{ totalPages }} 頁（共 {{ total }} 件）
          </span>
        </li>

        <!-- 下一頁 -->
        <li
          class="page-item"
          :class="{ disabled: page >= totalPages }"
        >
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

// 這頁會掛在 /cnt/tag/:tagId/products
const props = defineProps({
  tagId: {
    type: [String, Number],
    required: true,
  },
});

const router = useRouter();

// 給 ProductCard 用的商品陣列
const items = ref([]);

// ⭐ 分頁 state
const page = ref(1);          // 目前第幾頁 (1-based)
const pageSize = ref(24);     // 一頁幾筆
const total = ref(0);         // 總筆數（後端回來）
const totalPages = ref(1);    // Math.ceil(total/pageSize)

// 把圖片路徑補成完整 URL
function fixImageUrl(path) {
  if (!path) return "/images/no-image.png";

  // 已經是完整 URL
  if (/^https?:\/\//i.test(path)) return path;

  // 典型靜態上傳路徑
  if (path.startsWith("/uploads/")) {
    return `https://localhost:7103${path}`;
  }

  if (path.startsWith("././file?id=")) {
    return path.replace(
      "././file?id=",
      "https://localhost:7103/file?id="
    );
  }

  // 新版後端很可能直接給像 "/file?id=123" 或 "/assets/img/xxx.webp"
  // => 把它當作「後端 baseUrl + 相對路徑」
  return `https://localhost:7103${path}`;
}


// 從後端抓「這個標籤底下的商品」
// ⭐ 期望後端回傳 { total, items: [...] }
async function loadProducts() {
  try {
    const res = await axios.get(`/api/cnt/tags/${props.tagId}/products`, {
  params: { page: page.value, pageSize: pageSize.value },
});

    const rawTotal = res.data?.total ?? 0;
    const rawItems = res.data?.items ?? [];

    total.value = rawTotal;
    totalPages.value = Math.max(
      1,
      Math.ceil(total.value / pageSize.value)
    );

    items.value = rawItems.map((p) => {
      const imgCandidate =
        p.imageUrl ||
        p.mainImageUrl ||
        p.coverImage ||
        p.image ||
        "";

      return {
        productId: p.productId,
        productName: p.productName,
        shortDesc: p.shortDesc || "",
        brandName: p.brandName || "",
        badge: p.badge || "",

        avgRating: p.avgRating ?? 0,
        reviewCount: p.reviewCount ?? 0,

        salePrice: p.salePrice ?? p.price ?? 0,
        listPrice: p.listPrice ?? p.originalPrice ?? 0,

        imageUrl: fixImageUrl(imgCandidate),
      };
    });
  } catch (err) {
    console.error("載入商品失敗", err);
    // 失敗時你還是要清畫面，避免殘留舊資料
    total.value = 0;
    totalPages.value = 1;
    items.value = [];
  }
}


// 換頁
function goPage(newPage) {
  if (newPage < 1) return;
  if (newPage > totalPages.value) return;

  page.value = newPage;
  loadProducts();
}

// 點商品卡片 => 進商品詳情頁
function goDetail(productId) {
  router.push({
    name: "prod-detail",
    params: { id: productId },
  });
}

// 左上「← 返回文章」
function goBack() {
  router.back();
}

// 加入購物車（可後續串購物車模組）
function addToCart(prod) {
  console.log("加入購物車:", prod);
}

// 初次載入
onMounted(loadProducts);

// 如果同一個 component 被重用，但 tagId 改變，就重抓第 1 頁
watch(
  () => props.tagId,
  () => {
    page.value = 1; // ⭐ 重要：切換標籤時回到第一頁
    loadProducts();
  }
);
</script>

<style scoped>
/* 外殼卡片的視覺：微圓角、邊框、hover 陰影 */
.card-shell {
  border: 1px solid #ddd;
  background-color: #fff;
  transition: box-shadow 0.2s ease, transform 0.15s ease;
  cursor: pointer;
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
.card-shell :deep(.product-card) {
  border: 0;
  border-radius: 0;
  box-shadow: none;
  margin: 0;
  width: 100%;
  max-width: 100%;
}

/* ------- 分頁膠囊外觀 ------- */

/* 讓三顆按鈕視覺上像一條膠囊（上一頁 | 中間資訊 | 下一頁） */
.pagination.clean-pill {
  list-style: none;
  padding-left: 0;
  margin-bottom: 0;

  display: flex;
  flex-wrap: nowrap;
  align-items: stretch;
  border-radius: 999px;
  box-shadow: 0 4px 16px rgba(0,0,0,.08);
  overflow: hidden; /* 把圓角吃進去 */
  background-color: #fff;
  border: 1px solid #ccc;
}

/* 拿掉 bootstrap 預設的 li > a spacing 行為干擾 */
.pagination.clean-pill .page-item {
  margin: 0;
}
.pagination.clean-pill .page-item.disabled .page-link {
  opacity: .4;
  pointer-events: none;
}

/* 每段膠囊的樣式 */
.pagination.clean-pill .page-link {
  border: 0;
  border-radius: 0;
  background: transparent;
  padding: .5rem .75rem;
  font-size: .9rem;
  line-height: 1.2rem;
  white-space: nowrap;
  color: #004f4a; /* 深綠系字色，跟整體主色系靠近 */
}

.nav-pill-left {
  border-right: 1px solid #ccc;
}
.nav-pill-mid {
  border-right: 1px solid #ccc;
}
.nav-pill-right {
  /* 最右段不用邊框 */
}
</style>
