<template>
  <nav>
    <!-- SUP內部頁面的細節導航 -->
    <router-link to="/sup/logistics-fee" class="btn link-btn">運費計算</router-link>
    <router-link to="/sup/logistics-list" class="btn link-btn">配送資訊</router-link>
    <!-- <router-link to="/sup/store-map">門市地圖</router-link> -->
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

.link-btn {
  display: inline-block;
  padding: 8px 22px;
  margin: 0 8px 12px 0;
  border: none;
  border-radius: 4px;
  font-size: 16px;
  text-decoration: none;
  font-weight: 600;
  cursor: pointer;
  background-color: rgb(0, 112, 131);
  color: rgb(248, 249, 250);
  transition: background-color 0.3s ease;
}

.link-btn:hover {
  background-color: rgb(77, 180, 193);
  color: rgb(248, 249, 250);
}
</style>
