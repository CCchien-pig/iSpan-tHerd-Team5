<template>
  <div class="store-map-wrapper">
    <div v-if="loading" class="placeholder d-flex justify-content-center align-items-center">
      <div class="spinner-border text-secondary" role="status">
        <span class="visually-hidden">Loading...</span>
      </div>
    </div>

    <div
      v-else-if="error"
      class="placeholder d-flex justify-content-center align-items-center text-danger"
    >
      <i class="bi bi-exclamation-circle me-2"></i>
      {{ error }}
    </div>

    <GMapMap
      v-else
      :api-key="apiKey"
      :center="center"
      :zoom="16"
      :options="mapOptions"
      class="g-map"
    >
      <GMapMarker :position="center" :clickable="true" @click="infoWinOpen = !infoWinOpen">
        <GMapInfoWindow
          :opened="infoWinOpen"
          :options="{ pixelOffset: { width: 0, height: -35 } }"
          @closeclick="infoWinOpen = false"
        >
          <div class="info-window-content">
            <h6 class="fw-bold mb-1" style="color: #007083">{{ storeName }}</h6>
            <p class="mb-0 small">{{ address }}</p>
          </div>
        </GMapInfoWindow>
      </GMapMarker>
    </GMapMap>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'

const props = defineProps({
  address: { type: String, required: true },
  storeName: { type: String, default: '取貨門市' },
})

const apiKey = import.meta.env.VITE_GOOGLE_MAPS_KEY
const center = ref({ lat: 25.033, lng: 121.5654 }) // 預設台北101
const loading = ref(true)
const error = ref('')
const infoWinOpen = ref(true)

const mapOptions = {
  mapTypeControl: false,
  streetViewControl: false,
  fullscreenControl: true,
  zoomControl: true,
}

// 將地址轉為座標 (Geocoding)
async function geocodeAddress(addr) {
  if (!addr) return
  loading.value = true
  error.value = ''

  try {
    const url = `https://maps.googleapis.com/maps/api/geocode/json?address=${encodeURIComponent(addr)}&key=${apiKey}&language=zh-TW`
    const res = await fetch(url)
    const data = await res.json()

    if (data.status === 'OK' && data.results.length > 0) {
      const location = data.results[0].geometry.location
      center.value = { lat: location.lat, lng: location.lng }
      infoWinOpen.value = true
    } else {
      error.value = '無法定位此門市地址'
      console.warn('Geocode failed:', data.status)
    }
  } catch (e) {
    error.value = '地圖載入失敗'
    console.error('Geocode error:', e)
  } finally {
    loading.value = false
  }
}

// 監聽地址變動 (例如使用者重新選了門市)
watch(
  () => props.address,
  (newVal) => {
    if (newVal) geocodeAddress(newVal)
  },
)

onMounted(() => {
  geocodeAddress(props.address)
})
</script>

<style scoped>
.store-map-wrapper {
  width: 100%;
  height: 250px; /* 可依需求調整高度 */
  border-radius: 12px;
  overflow: hidden;
  border: 1px solid #e9ecef;
}

.g-map {
  width: 100%;
  height: 100%;
}

.placeholder {
  width: 100%;
  height: 100%;
  background-color: #f8f9fa;
}

.info-window-content {
  color: #333;
  min-width: 160px;
}
</style>
