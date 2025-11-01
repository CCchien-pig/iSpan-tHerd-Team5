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
          <!-- 直接塞商品模組現成的 ProductCard -->
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
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from "vue";
import { useRouter } from "vue-router";
import axios from "axios";

// 這頁會掛在 /cnt/tag/:tagId/products （router 已經在 cnt.js 註冊了 name: 'cnt-tag-products'）:contentReference[oaicite:2]{index=2}
const props = defineProps({
  tagId: {
    type: [String, Number],
    required: true,
  },
});

const router = useRouter();

// 這是要丟給 <ProductCard /> 的資料陣列
const items = ref([]);

/* 把圖片路徑補成能用的完整 URL（同你內容模組裡處理文章內圖的邏輯）:contentReference[oaicite:3]{index=3} */
function fixImageUrl(path) {
  if (!path) {
    return "https://via.placeholder.com/400?text=No+Image";
  }
  if (/^https?:\/\//i.test(path)) return path;
  if (path.startsWith("/uploads/")) {
    return `https://localhost:7103${path}`;
  }
  if (path.startsWith("../../file?id=")) {
    return path.replace(
      "../../file?id=",
      "https://localhost:7103/file?id="
    );
  }
  return path;
}

/* 從後端抓「這個標籤底下的商品」，並整理成 ProductCard 可以吃的格式。:contentReference[oaicite:4]{index=4} */
async function loadProducts() {
  const res = await axios.get(`/api/cnt/tags/${props.tagId}/products`, {
    params: { take: 24 },
  });

  const rawList = res.data || [];

  items.value = rawList.map((p) => {
    const imgCandidate =
      p.imageUrl ||
      p.mainImageUrl ||
      p.coverImage ||
      p.image ||
      "";

    return {
      // ProductCard.vue 需要的各欄位結構（含品牌、名稱、標籤、評分、價格等）:contentReference[oaicite:5]{index=5}
      productId: p.productId,
      productName: p.productName,
      shortDesc: p.shortDesc || "",
      brandName: p.brandName || "",
      badge: p.badge || "",

      avgRating: p.avgRating ?? 0,
      reviewCount: p.reviewCount ?? 0,

      salePrice: p.salePrice ?? p.price ?? 0,         // 現價
      listPrice: p.listPrice ?? p.originalPrice ?? 0, // 原價(拿來畫刪除線)

      imageUrl: fixImageUrl(imgCandidate),
    };
  });
}

/* 點商品卡片 => 進商品詳情頁  
   router 裡已經有 name:'prod-detail' 指到你的商品詳情元件 ProductDetail.vue，props: true。:contentReference[oaicite:6]{index=6} */
function goDetail(productId) {
  router.push({ name: "prod-detail", params: { id: productId } });
}

// 左上「← 返回文章」
function goBack() {
  router.back();
}

// 購物車按鈕（有需要可往後串購物車模組）
function addToCart(prod) {
  console.log("加入購物車:", prod);
}

// 初次載入
onMounted(loadProducts);

// 同一個 component 被重用但 tagId 不同時，自動重抓
watch(
  () => props.tagId,
  () => {
    loadProducts();
  }
);

// 從商品模組匯入卡片元件（你首頁展示商品就是用這顆的結構，只是那邊是手刻樣式，這裡改用統一版元件維護會比較爽）:contentReference[oaicite:7]{index=7}
import ProductCard from "@/components/modules/prod/card/ProductCard.vue";
</script>

<style scoped>
/* 類似首頁那種有邊框、圓角、hover 提升一點陰影的感覺 */
.card-shell {
  border: 1px solid #ddd;
  background-color: #fff;
  transition: box-shadow 0.2s ease, transform 0.15s ease;
  cursor: pointer;
  border-radius: 0.75rem; /* 跟首頁那種 rounded-3 一樣柔一點 */
  overflow: hidden;
  height: 100%;
  display: flex;
  align-items: stretch;
  justify-content: stretch;
}

/* hover 效果：跟你首頁卡片那種「浮一點」的視覺 */
.card-shell:hover {
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
  transform: translateY(-2px);
}

/* 讓內部 ProductCard 填滿，避免裡面再塞一層白底造成雙邊框不齊 */
.card-shell :deep(.product-card) {
  border: 0;
  border-radius: 0;
  box-shadow: none;
  margin: 0;
  width: 100%;
  max-width: 100%;
}

/* ProductCard 裡面那顆「加入購物車」原本是 hover 才浮出，我們保持那個互動 */
</style>
