<template>
  <BreadcrumbNav :breadcrumbs="productBreadcrumbs" />
</template>

<script>
import BreadcrumbNav from '@/components/ui/BreadcrumbNav.vue'

export default {
  name: 'ProductBreadcrumb',
  components: { BreadcrumbNav },
  props: {
    product: { type: Object, required: true },
  },
  computed: {
    productBreadcrumbs() {
      const crumbs = [{ name: '首頁', path: '/' }]

      // 🔹 分類階層：生成可點擊路由
      if (this.product?.categoryPath) {
        const categories = this.product.categoryPath.split(' > ')
        let basePath = '/products' // 可依你實際路由修改
        categories.forEach((cat, i) => {
          // 轉成 SEO 友好的路徑，例如 /products/vitamins/123
          const slug = encodeURIComponent(cat.toLowerCase().replace(/\s+/g, '-'))
          const typeId = this.product.categoryIds?.[i] // ← 如果後端能提供對應的分類ID會更好
          crumbs.push({
            name: cat,
            path: i < categories.length - 1
              ? `${basePath}/${slug}${typeId ? '/' + typeId : ''}`
              : null,
          })
        })
      }

      // 🔹 品牌階層
      if (this.product?.brandName) {
        crumbs.push({
          name: this.product.brandName,
          path: `/brands/${this.product.brandCode || this.product.brandName}`,
        })
      }

      // 🔹 最後一層（商品名稱，無連結）
      crumbs.push({
        name: this.product?.productName || '產品詳情',
        path: null,
      })

      return crumbs
    },
  },
}
</script>
