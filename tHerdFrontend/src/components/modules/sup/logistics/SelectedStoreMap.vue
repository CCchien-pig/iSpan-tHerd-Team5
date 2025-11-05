<template>
  <div v-if="loading" class="text-center p-3 text-muted bg-light rounded">
    <span class="spinner-border spinner-border-sm me-1"></span>
    正在載入門市地圖...
  </div>

  <div v-else-if="error" class="text-center p-3 text-danger bg-light rounded">
    <i class="bi bi-exclamation-circle me-1"></i>
    {{ error }}
  </div>

  <GMapMap
    v-else
    ref="mapRef"
    :api-key="apiKey"
    :center="mapCenter"
    :zoom="16"
    :options="{
      mapTypeControl: false,
      streetViewControl: false,
      fullscreenControl: true,
    }"
    style="width: 100%; height: 300px; border-radius: 12px; overflow: hidden"
  >
    <GMapMarker :position="mapCenter" :clickable="true" @click="isInfoWindowOpen = true" />

    <GMapInfoWindow
      :position="mapCenter"
      :opened="isInfoWindowOpen"
      :options="{
        pixelOffset: { width: 0, height: -35 },
      }"
      @closeclick="isInfoWindowOpen = false"
    >
      <div class="marker-info">
        <h6 class="fw-bold mb-1 text-teal">{{ storeName }}</h6>
        <p class="mb-0 small">{{ address }}</p>
      </div>
    </GMapInfoWindow>
  </GMapMap>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'

const props = defineProps({
  address: { type: String, required: true },
  storeName: { type: String, default: '取貨門市' },
})

const apiKey = import.meta.env.VITE_GOOGLE_MAPS_KEY
const mapRef = ref(null)
const mapCenter = ref({ lat: 25.033, lng: 121.5654 }) // 預設台北101
const loading = ref(true)
const error = ref('')
const isInfoWindowOpen = ref(true)

// Geocoding: 地址轉座標
async function geocodeAndSetCenter(addr) {
  if (!addr) return
  loading.value = true
  error.value = ''
  try {
    const url = `https://maps.googleapis.com/maps/api/geocode/json?address=${encodeURIComponent(addr)}&key=${apiKey}`
    const res = await fetch(url)
    const data = await res.json()

    if (data.status === 'OK' && data.results.length > 0) {
      mapCenter.value = data.results[0].geometry.location
      isInfoWindowOpen.value = true
    } else {
      error.value = '無法找到此門市的地图位置'
    }
  } catch (err) {
    console.error('Geocode error:', err)
    error.value = '地圖載入失敗'
  } finally {
    loading.value = false
  }
}

// 監聽地址變化 (如果使用者重新選擇門市)
watch(
  () => props.address,
  (newAddr) => {
    geocodeAndSetCenter(newAddr)
  },
)

onMounted(() => {
  geocodeAndSetCenter(props.address)
})
</script>

<style scoped>
.text-teal {
  color: #007083;
}
.marker-info {
  min-width: 150px;
  font-family: sans-serif;
}
</style>
