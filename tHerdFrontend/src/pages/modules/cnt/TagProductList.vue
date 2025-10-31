<template>
  <div class="container py-4">
    <!-- 標題列：左返回 / 中置中標題 / 右空白佔位 -->
    <div class="d-flex align-items-center mb-3">
      <button class="btn teal-reflect-button text-white me-3" @click="goBack">
        ← 返回文章
      </button>

      <h2 class="flex-grow-1 text-center m-0 main-color-green-text">
        相關商品
      </h2>

      <!-- 右邊塞一個固定寬度的空 div，讓標題真的在正中間 -->
      <div style="width:2.5rem;"></div>
    </div>

    <!-- 商品清單 -->
    <div v-if="items.length" class="row g-3">
      <div
        class="col-6 col-md-4 col-lg-3"
        v-for="prod in items"
        :key="prod.productId"
      >
        <div
          class="h-100"
          style="cursor:pointer; text-decoration:none; color:inherit;"
          @click="goDetail(prod.productId)"
        >
          <!-- 不動 prod 的卡片，直接餵我們整理好的物件 -->
          <ProductCard
            :product="prod"
            @add-to-cart="addToCart"
          />
        </div>
      </div>
    </div>

    <!-- 無商品狀態 -->
    <div v-else class="text-center text-muted py-5">
      這個標籤目前沒有可顯示的商品
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from "vue";
import { useRouter } from "vue-router";
import axios from "axios";

// 這個頁面在 router 裡是
// path: 'tag/:tagId/products',
// name: 'cnt-tag-products',
// props: true  ← 所以這裡會拿到 tagId
// （見你目前的 routes 設定）:contentReference[oaicite:4]{index=4}
const props = defineProps({
  tagId: {
    type: [String, Number],
    required: true,
  },
});

const router = useRouter();
const items = ref([]); // 這個陣列會直接餵給 <ProductCard :product="..."/>

/**
 * 把後端給的圖片路徑補成可用的完整網址。
 * 這邏輯是複製你在 ArticleDetail 那支檔案裡用的 absoluteImageUrl。:contentReference[oaicite:5]{index=5}
 */
function fixImageUrl(path) {
  if (!path) {
    // 沒圖就給個安全的 placeholder，否則卡片會顯示破圖
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

/**
 * 從後端拿相關商品，再整理成 ProductCard 看的懂的格式。
 * 你的舊版是 items.value = res.data；然後在 template 再呼叫 mapProductForCard()。:contentReference[oaicite:6]{index=6}
 * 這裡我直接一次轉好（包含圖片 / 價格 / badge）。
 */
async function loadProducts() {
  const res = await axios.get(`/api/cnt/tags/${props.tagId}/products`, {
    params: { take: 24 },
  });

  const rawList = res.data || [];

  items.value = rawList.map(p => {
    // 後端目前一定有的：productId, productName, maybe badge, maybe shortDesc
    // 其他欄位（價錢、評分、主圖）如果後端有就吃，沒有就給安全 fallback
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

      salePrice: p.salePrice ?? p.price ?? 0,        // 現價
      listPrice: p.listPrice ?? p.originalPrice ?? 0, // 原價

      imageUrl: fixImageUrl(imgCandidate),
    };
  });
}

/**
 * 點卡片 -> 進商品詳情
 * 目前你用的是 name: 'prod-detail'。:contentReference[oaicite:7]{index=7}
 * 這條路由在 CNT 的 routes 裡沒有註冊到，所以才報 "No match for 'prod-detail'"。
 * （下面我會講怎麼補 router）
 */
function goDetail(productId) {
  router.push({ name: "prod-detail", params: { id: productId } });
}

function goBack() {
  router.back();
}

function addToCart(prod) {
  console.log("加入購物車:", prod);
}

// 初次載入
onMounted(loadProducts);

// 如果同一個 component 內 props.tagId 改變（例如同頁切不同標籤），重新載入
watch(
  () => props.tagId,
  () => {
    loadProducts();
  }
);

// 不動 prod 模組的卡片
import ProductCard from "@/components/modules/prod/card/ProductCard.vue";
</script>
