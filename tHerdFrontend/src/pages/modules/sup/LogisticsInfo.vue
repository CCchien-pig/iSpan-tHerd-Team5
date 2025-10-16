<template>
  <nav>
    <!-- SUP內部頁面的細節導航 -->
    <router-link to="/sup/logistics-fee" class="btn btn-info" style="color: #fff"
      >運費計算</router-link
    >
    <!-- <router-link to="/sup/logistics-list">運送資訊</router-link>
    <router-link to="/sup/store-map">門市地圖</router-link> -->
    <!-- <router-link to="/sup/suppliers">供應商列表</router-link> -->
    <!-- ...其他 SUP 分頁 -->
    <router-view />
  </nav>
  <div>
    <StoreMap />
    <LogisticsListTable />
  </div>
</template>

<script setup>
import LogisticsListTable from '@/components/modules/sup/logistics/LogisticsList.vue'
import StoreMap from '@/components/modules/sup/logistics/StoreMap.vue'
</script>

<style scoped>
nav {
  padding: 1rem;
}
nav a {
  margin-right: 1rem;
  text-decoration: none;
  color: #2c3e50;
}
nav a:hover {
  color: #42b983;
}
</style>
