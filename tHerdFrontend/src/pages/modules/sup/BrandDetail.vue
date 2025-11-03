<!-- 依 getBrandDetail API 取回 Banner、Buttons、Accordion，
先以預設順序渲染；
未來後端若回傳 orderedBlocks，就能依序渲染切換版位而不動骨架。 -->

<template>
  <section class="container py-3">
    <header class="d-flex align-items-center gap-2 mb-3">
      <h1 class="h4 m-0">{{ detail?.brandName || '品牌' }}</h1>
    </header>

    <!-- Banner -->
    <div v-if="detail?.bannerUrl" class="mb-4">
      <img :src="detail.bannerUrl" :alt="detail.brandName" class="img-fluid w-100 rounded" />
    </div>

    <!-- 分類按鈕 -->
    <nav v-if="detail?.buttons?.length" class="d-flex flex-wrap gap-2 mb-4">
      <button
        v-for="btn in detail.buttons"
        :key="btn.id"
        type="button"
        class="btn btn-outline-primary btn-sm"
        @click="onFilter(btn)"
      >
        {{ btn.text }}
      </button>
    </nav>

    <!-- Accordion -->
    <div v-if="detail?.accordions?.length">
      <section v-for="grp in detail.accordions" :key="grp.contentKey" class="mb-3">
        <div
          v-for="item in grp.items"
          :key="`${grp.contentKey}-${item.order}`"
          class="border rounded mb-2"
        >
          <details>
            <summary class="p-2 fw-semibold">{{ item.title }}</summary>
            <div class="p-3" v-html="item.body"></div>
          </details>
        </div>
      </section>
    </div>
  </section>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { getBrandDetail } from '@/core/api/modules/sup/supBrands'

const route = useRoute()
const detail = ref(null)
const loading = ref(false)

const fetchDetail = async () => {
  loading.value = true
  try {
    const id = Number(route.params.brandId)
    const res = await getBrandDetail(id)
    const data = res?.data?.data ?? res?.data ?? null

    // 保險排序（若後端未排序）
    if (data?.buttons?.length) {
      data.buttons = [...data.buttons].sort((a, b) => a.order - b.order)
    }
    if (data?.accordions?.length) {
      data.accordions = data.accordions.map((g) => ({
        ...g,
        items: [...g.items].sort((a, b) => a.order - b.order),
      }))
    }

    detail.value = data
    document.title = data?.brandName ? `${data.brandName}｜品牌` : '品牌'
  } finally {
    loading.value = false
  }
}

onMounted(fetchDetail)
watch(() => route.params.brandId, fetchDetail)

const onFilter = (btn) => {
  // TODO: 依需求導商品列表或切換該品牌商品類型
  // 例如：router.push({ name: 'ProductList', query: { brandId: detail.value?.brandId, typeId: btn.id } })
}
</script>

<style scoped>
details[open] summary {
  background: #f8fafc;
}
</style>
