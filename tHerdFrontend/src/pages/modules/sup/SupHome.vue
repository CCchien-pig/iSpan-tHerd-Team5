<template>
  <nav>
    <!-- SUP內部頁面的細節導航 -->
    <router-link to="/sup/logistics-fee" class="nav-btn">運費計算</router-link>
    <router-link to="/sup/logistics-list" class="nav-btn">運送資訊</router-link>
    <router-link to="/sup/store-map" class="nav-btn">門市地圖</router-link>

    <!-- <router-link to="/sup/suppliers">供應商列表</router-link> -->
    <!-- ...其他 SUP 分頁 -->
    <router-view />
  </nav>
</template>

<script setup>
// 可加自訂邏輯
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
  color: #fff;
}
.nav-btn {
  display: inline-block;
  padding: 8px 22px;
  margin: 0 8px 12px 0;
  border: none;
  border-radius: 4px;
  background: #07a5b9;
  color: #fff;
  font-size: 16px;
  text-decoration: none;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.18s;
}

.nav-btn:hover {
  background: #1c49c6;
}
</style>
