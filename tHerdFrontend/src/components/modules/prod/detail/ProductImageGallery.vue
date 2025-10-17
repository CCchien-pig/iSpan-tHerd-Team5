<!--
  ProductImageGallery.vue - 商品圖片展示組件
  功能：主圖展示、縮圖切換、圖片放大
-->
<template>
  <div class="product-image-gallery">
    <!-- 主圖區域 -->
    <div class="main-image-container">
      <img :src="currentImage.fileUrl" :alt="productName" class="main-image" />
    </div>

    <!-- 縮圖列表 -->
    <div class="thumbnail-list" v-if="images.length > 1">
      <div
        v-for="(image, index) in images"
        :key="image.imageId"
        class="thumbnail-item"
        :class="{ active: currentImageIndex === index }"
        @click="selectImage(index)"
      >
        <img
          :src="image.fileUrl"
          :alt="`${productName} - 圖片 ${index + 1}`"
          class="thumbnail-image"
        />
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'

const props = defineProps({
  images: {
    type: Array,
    required: true,
    default: () => [],
  },
  productName: {
    type: String,
    required: true,
  },
})

// 當前顯示的圖片索引
const currentImageIndex = ref(0)

// 當前顯示的圖片
const currentImage = computed(() => {
  if (props.images.length === 0) {
    // 沒有圖片，顯示預設圖片
    return {
      imageId: 0,
      fileUrl: 'https://via.placeholder.com/500x500?text=No+Image',
      isMain: true,
    }
  }
  return props.images[currentImageIndex.value]
})

/**
 * 選擇圖片
 */
const selectImage = (index) => {
  currentImageIndex.value = index
}
</script>

<style scoped>
.product-image-gallery {
  position: sticky;
  top: 20px;
}

/* 主圖容器 */
.main-image-container {
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  padding: 20px;
  margin-bottom: 15px;
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 400px;
}

.main-image {
  max-width: 100%;
  max-height: 400px;
  object-fit: contain;
  cursor: zoom-in;
}

/* 縮圖列表 */
.thumbnail-list {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  justify-content: center;
}

.thumbnail-item {
  width: 80px;
  height: 80px;
  border: 2px solid #e0e0e0;
  border-radius: 8px;
  padding: 5px;
  cursor: pointer;
  transition: all 0.3s ease;
  background: #fff;
}

.thumbnail-item:hover {
  border-color: #f68b1e;
  box-shadow: 0 2px 8px rgba(246, 139, 30, 0.3);
}

.thumbnail-item.active {
  border-color: #f68b1e;
  box-shadow: 0 2px 8px rgba(246, 139, 30, 0.5);
}

.thumbnail-image {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

/* 響應式設計 */
@media (max-width: 768px) {
  .product-image-gallery {
    position: static;
  }

  .main-image-container {
    min-height: 300px;
  }

  .main-image {
    max-height: 300px;
  }

  .thumbnail-item {
    width: 60px;
    height: 60px;
  }
}
</style>
