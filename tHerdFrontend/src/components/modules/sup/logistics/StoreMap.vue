<template>
  <!-- <h2>門市地圖</h2> -->
  <div class="map-container">
    <div class="section">
      <h3 class="section-title">查詢物流門市</h3>
      <div class="selector-zone">
        <label for="logistics-select">物流商：</label>
        <select
          id="logistics-select"
          name="logistics"
          style="min-width: 150px; margin-right: 5px"
          v-model="selectedLogistics"
          required
        >
          <option :value="null" disabled>請選擇</option>
          <option v-for="opt in logisticsOpts" :value="opt.value" :key="opt.value">
            {{ opt.label }} - {{ opt.method }}
          </option>
        </select>

        <label for="city-select">地區：</label>
        <select
          id="city-select"
          name="city"
          style="margin-right: 5px"
          v-model="selectedCity"
          :disabled="!selectedLogistics"
          required
        >
          <option v-for="city in cityOptions" :value="city" :key="city">{{ city }}</option>
        </select>

        <label for="store-select">門市：</label>
        <select
          id="store-select"
          name="store"
          style="margin-right: 5px"
          v-model="selectedStoreId"
          :disabled="!selectedCity"
          required
        >
          <option v-for="store in storeOptions" :value="store.StoreID" :key="store.StoreID">
            {{ store.StoreName }}
          </option>
        </select>

        <button
          @click="onQuery"
          :disabled="!selectedLogistics || !selectedCity || !selectedStoreId"
        >
          查詢
        </button>
      </div>
    </div>

    <div class="divider"></div>

    <div class="section">
      <h3 class="section-title">輸入所在地，搜尋最近門市</h3>
      <form class="search-zone" @submit.prevent="searchNearestStore" autocomplete="off">
        <label for="search-logistics">物流商：</label>
        <select
          id="search-logistics"
          name="searchLogistics"
          style="min-width: 150px; margin-right: 5px"
          v-model="selectedLogistics"
          required
        >
          <option :value="null" disabled>請選擇</option>
          <option v-for="opt in logisticsOpts" :value="opt.value" :key="opt.value">
            {{ opt.label }} - {{ opt.method }}
          </option>
        </select>

        <label for="address-input">地址、地標：</label>
        <input
          id="address-input"
          name="address"
          v-model.lazy="addressInput"
          placeholder="輸入地址或地標"
          required
          autocomplete="off"
        />

        <button type="submit" :disabled="!addressInput || !selectedLogistics">搜尋最近門市</button>
      </form>

      <div class="example-buttons">
        <span style="margin-right: 8px">範例：</span>
        <button
          class="example-btn"
          type="button"
          @click="fillExampleAddress('桃園市中壢區新生路二段421號')"
        >
          聖德基督學院
        </button>
        <button class="example-btn" type="button" @click="fillExampleAddress('台北車站')">
          北車
        </button>
      </div>

      <div
        v-if="distanceText"
        style="
          margin-top: 10px;
          color: rgb(0, 112, 131);
          text-align: center;
          font-weight: bold;
          font-size: 18px;
        "
      >
        輸入地址與 {{ selectedLogisticsLabel }} 最近門市的距離：{{ distanceText }}
      </div>
    </div>

    <GMapMap
      ref="mapRef"
      :api-key="apiKey"
      :center="mapCenter"
      :zoom="mapZoom"
      style="width: 100%; height: 500px; margin-top: 16px"
      @click="
        () => {
          unifiedType = null
          unifiedInfo = null
        }
      "
      @ready="onMapReady"
    >
      <GMapMarker
        v-for="marker in markers"
        :key="marker.store.StoreID"
        :position="marker.position"
        @click="handleMarkerClick(marker.store)"
      />
      <GMapMarker v-if="userLocation" :position="userLocation" @click="openAddressInfoMarker" />
      <GMapPolyline
        v-if="activeMode === 'search' && routePath.length > 1"
        :key="routePath.length"
        :path="routePath"
        :options="{ strokeColor: '#0079fd', strokeWeight: 4, strokeOpacity: 0.8 }"
      />

      <!-- 單一 InfoWindow：用 opened 控制，切換內容 -->
      <GMapInfoWindow
        :position="
          unifiedType === 'store' && unifiedInfo
            ? { lat: unifiedInfo.Latitude, lng: unifiedInfo.Longitude }
            : unifiedType === 'address' && unifiedInfo
              ? { lat: unifiedInfo.lat, lng: unifiedInfo.lng }
              : null
        "
        :opened="Boolean(unifiedType && unifiedInfo)"
        :options="{
          pixelOffset: {
            width: 0,
            height: -35, // 負值表示向上偏移，您可以根據 Marker 的高度調整這個數值
          },
        }"
        @closeclick="handleCloseInfo"
      >
        <template v-if="unifiedType === 'store' && unifiedInfo">
          <div class="marker-card">
            <div class="card-title">{{ unifiedInfo?.StoreName }}</div>
            <div class="card-type">{{ unifiedInfo?.Type }}</div>
            <div class="card-row">
              <span class="label">分店編號：</span>{{ unifiedInfo?.StoreID }}
            </div>
            <div class="card-row"><span class="label">地址：</span>{{ unifiedInfo?.Address }}</div>
            <div class="card-row"><span class="label">城市：</span>{{ unifiedInfo?.City }}</div>
            <div class="card-row"><span class="label">電話：</span>{{ unifiedInfo?.Phone }}</div>
            <div class="card-row">
              <span class="label">座標：</span>
              {{ unifiedInfo?.Latitude?.toFixed(4) }}, {{ unifiedInfo?.Longitude?.toFixed(4) }}
            </div>
          </div>
        </template>
        <template v-else-if="unifiedType === 'address' && unifiedInfo">
          <div class="address-card">
            <div class="card-row">
              <span class="label">查詢地址：<br /></span>{{ unifiedInfo.address }}
            </div>
            <div class="card-row">
              <span class="label">座標：</span>
              {{ unifiedInfo.lat.toFixed(6) }}, {{ unifiedInfo.lng.toFixed(6) }}
            </div>
          </div>
        </template>
      </GMapInfoWindow>
    </GMapMap>
  </div>
</template>

<script setup>
import { ref, watch, nextTick, computed } from 'vue'
import sfStores from '../assets/mock_sf_stores.json'
import sevenStores from '../assets/mock_711_stores.json'
import blackcatStores from '../assets/mock_blackcat_stores.json'
import postStores from '../assets/mock_post_stores.json'

const apiKey = import.meta.env.VITE_GOOGLE_MAPS_KEY

const logisticsOpts = [
  { value: 'sf', label: '順豐速運', method: '宅配到府', stores: sfStores },
  { value: 'blackcat', label: '黑貓宅急便', method: '低溫宅配', stores: blackcatStores },
  { value: '711', label: '7-11', method: '超商店到店', stores: sevenStores },
  { value: 'postBox', label: '中華郵政', method: 'i 郵箱', stores: postStores },
  { value: 'post', label: '中華郵政', method: '掛號包裹', stores: postStores },
]

const selectedLogistics = ref(null)
const selectedCity = ref('全台')
const selectedStoreId = ref('all')
const cityOptions = ref([])
const storeOptions = ref([])
const addressInput = ref('')

const isMapReady = ref(false)
const infoLock = ref(false)
const mapRef = ref(null)
const mapCenter = ref({ lat: 23.5, lng: 121 })
const mapZoom = ref(7)
const markers = ref([])
const userLocation = ref(null)
const routePath = ref([])
const distanceText = ref('')

// 單一資訊卡狀態
const unifiedType = ref(null) // 'store' | 'address' | null
const unifiedInfo = ref(null) // 內容物：store 或 { address, lat, lng }
const activeMode = ref('query')

const selectedLogisticsLabel = computed(() => {
  const option = logisticsOpts.find((opt) => opt.value === selectedLogistics.value)
  return option ? option.label : ''
})

function onMapReady() {
  isMapReady.value = true
}
function parseCity(address) {
  const list = [
    '台北市',
    '新北市',
    '桃園市',
    '台中市',
    '台南市',
    '高雄市',
    '基隆市',
    '新竹市',
    '嘉義市',
    '新竹縣',
    '苗栗縣',
    '彰化縣',
    '南投縣',
    '雲林縣',
    '嘉義縣',
    '屏東縣',
    '宜蘭縣',
    '花蓮縣',
    '台東縣',
    '澎湖縣',
    '金門縣',
    '連江縣',
  ]
  for (const city of list) if (address?.startsWith(city) || address?.includes(city)) return city
  return ''
}
function processStoreData(stores) {
  return stores.map((s) => ({
    ...s,
    City: parseCity(s.Address ?? s.StoreAddr),
    Latitude: Number(s.Latitude),
    Longitude: Number(s.Longitude),
  }))
}
function updateCityOptions() {
  const stores = logisticsOpts.find((o) => o.value === selectedLogistics.value)?.stores || []
  const withCity = processStoreData(stores)
  const cities = Array.from(new Set(withCity.map((s) => s.City))).filter(Boolean)
  cityOptions.value = ['全台', ...cities]
  selectedCity.value = '全台'
  updateStoreOptions()
}
function updateStoreOptions() {
  const stores = logisticsOpts.find((o) => o.value === selectedLogistics.value)?.stores || []
  const withCity = processStoreData(stores)
  let filtered = withCity
  if (selectedCity.value && selectedCity.value !== '全台')
    filtered = withCity.filter((s) => s.City === selectedCity.value)
  storeOptions.value = [{ StoreID: 'all', StoreName: '所有門市' }, ...filtered]
  selectedStoreId.value = 'all'
}

function fitBoundsAllMarker(filtered) {
  setTimeout(() => {
    const gmap = mapRef.value?.$mapObject
    if (gmap && filtered.length > 1 && window?.google?.maps) {
      const bounds = new window.google.maps.LatLngBounds()
      filtered.forEach((s) => bounds.extend(new window.google.maps.LatLng(s.Latitude, s.Longitude)))
      gmap.fitBounds(bounds)
      const center = gmap.getCenter()
      mapCenter.value = { lat: center.lat(), lng: center.lng() }
      mapZoom.value = gmap.getZoom()
    }
  }, 0)
}

function fitBoundsRoute(route, origin, dest) {
  setTimeout(() => {
    const gmap = mapRef.value?.$mapObject
    if (gmap && route.length && window?.google?.maps) {
      const bounds = new window.google.maps.LatLngBounds()
      route.forEach((pt) => bounds.extend(new window.google.maps.LatLng(pt.lat, pt.lng)))
      bounds.extend(new window.google.maps.LatLng(origin.lat, origin.lng))
      bounds.extend(new window.google.maps.LatLng(dest.lat, dest.lng))
      gmap.fitBounds(bounds)
      const center = gmap.getCenter()
      mapCenter.value = { lat: center.lat(), lng: center.lng() }
      mapZoom.value = gmap.getZoom()
    }
  }, 0)
}

function closeUnified() {
  if (infoLock.value) return
  unifiedType.value = null
  unifiedInfo.value = null
}

function onQuery() {
  // 關掉舊卡片
  closeUnified()
  const stores = logisticsOpts.find((o) => o.value === selectedLogistics.value)?.stores || []
  const withCity = processStoreData(stores)
  let filtered = withCity
  if (selectedCity.value !== '全台')
    filtered = withCity.filter((s) => s.City === selectedCity.value)
  const store = filtered.find((s) => s.StoreID === selectedStoreId.value)

  if (selectedStoreId.value === 'all') {
    markers.value = filtered.map((s) => ({
      position: { lat: s.Latitude, lng: s.Longitude },
      store: s,
    }))
    mapCenter.value = {
      lat: filtered[0].Latitude + Math.random() * 0.00001,
      lng: filtered[0].Longitude,
    }
    mapZoom.value = 10
    routePath.value = []
    userLocation.value = null
    distanceText.value = ''
    fitBoundsAllMarker(filtered)
  } else if (store) {
    markers.value = [{ position: { lat: store.Latitude, lng: store.Longitude }, store }]
    mapCenter.value = { lat: store.Latitude + Math.random() * 0.00001, lng: store.Longitude }
    mapZoom.value = 17
    routePath.value = []
    userLocation.value = null
    distanceText.value = ''
  }
}

// 最近門市模式
async function searchNearestStore() {
  closeUnified()
  activeMode.value = 'search'
  if (!addressInput.value || !selectedLogistics.value) return
  const geo = await geocode(addressInput.value)
  if (!geo) return alert('查無此地址')

  const stores = logisticsOpts.find((o) => o.value === selectedLogistics.value)?.stores || []
  const allStores = processStoreData(stores)
  let min = Infinity,
    nearest = null
  for (const s of allStores) {
    const d = Math.hypot(geo.lat - s.Latitude, geo.lng - s.Longitude)
    if (d < min) {
      min = d
      nearest = s
    }
  }
  if (!nearest) return alert('這個物流商沒有門市')

  userLocation.value = geo
  // 僅顯示最近門市 marker
  markers.value = [{ position: { lat: nearest.Latitude, lng: nearest.Longitude }, store: nearest }]
  mapCenter.value = geo
  mapZoom.value = 13

  const dir = new window.google.maps.DirectionsService()
  dir.route(
    {
      origin: geo,
      destination: { lat: nearest.Latitude, lng: nearest.Longitude },
      travelMode: 'DRIVING',
    },
    (res, status) => {
      if (status === 'OK') {
        routePath.value = res.routes[0].overview_path.map((p) => ({ lat: p.lat(), lng: p.lng() }))
        distanceText.value = res.routes[0].legs[0].distance.text
        fitBoundsRoute(routePath.value, geo, { lat: nearest.Latitude, lng: nearest.Longitude })
      }
    },
  )
}

async function handleMarkerClick(store) {
  if (infoLock.value) return
  infoLock.value = true

  // 關閉舊卡片，等一個 tick 確保 Overlay 卸載完成
  unifiedType.value = null
  unifiedInfo.value = null
  await nextTick()

  // 再開新卡
  unifiedType.value = 'store'
  unifiedInfo.value = store
  await nextTick()

  infoLock.value = false
}

function handleCloseInfo() {
  if (infoLock.value) return
  unifiedType.value = null
  unifiedInfo.value = null
}

function openAddressInfoMarker() {
  if (infoLock.value) return
  unifiedType.value = 'address'
  unifiedInfo.value = {
    address: addressInput.value,
    lat: userLocation.value.lat,
    lng: userLocation.value.lng,
  }
}

async function geocode(addr) {
  const url = `https://maps.googleapis.com/maps/api/geocode/json?address=${encodeURIComponent(addr)}&key=${apiKey}`
  const res = await fetch(url).then((r) => r.json())
  if (res.status === 'OK' && res.results.length) return res.results[0].geometry.location
  return null
}

function fillExampleAddress(a) {
  addressInput.value = a
}

watch(selectedLogistics, updateCityOptions)
watch(selectedCity, updateStoreOptions)
</script>

<style scoped>
h2 {
  margin-top: 30px;
  margin-left: 20px;
}
.map-container {
  width: 80vw;
  margin: 20px auto 0 auto;
  box-sizing: border-box;
  border: 2px solid #207fa5;
  border-radius: 18px;
  background: #fff;
  padding: 24px 16px;
}
.section {
  margin-bottom: 16px;
}
.section-title {
  font-size: 18px;
  font-weight: bold;
  margin: 0 0 8px 0;
  color: #207fa5;
}
.selector-zone,
.search-zone {
  display: flex;
  gap: 12px;
  align-items: center;
  flex-wrap: wrap;
  font-size: 16px;
}
.search-zone input {
  width: 295px;
  min-width: 210px;
}
.example-buttons {
  margin-top: 8px;
  display: flex;
  gap: 8px;
  align-items: center;
}
.example-btn {
  padding: 2px 8px;
  font-size: 14px;
  border-radius: 4px;
  background: #f0f8ff;
  border: 1px solid #ccc;
  cursor: pointer;
}
.example-btn:hover {
  background: #e0f0ff;
}
.divider {
  height: 1px;
  background: #ddd;
  margin: 16px 0;
}
label {
  font-weight: bold;
  display: flex;
  align-items: center;
  gap: 8px;
}
select,
input {
  min-width: 80px;
  font-size: 16px;
  padding: 4px 8px;
  border-radius: 6px;
  border: 1px solid #ccc;
}
input {
  min-width: 200px;
}
button {
  padding: 5px 18px;
  font-size: 16px;
  border-radius: 7px;
  background: #d3ece8;
  font-weight: bold;
  border: none;
  cursor: pointer;
}
button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.address-card {
  border-radius: 16px;
  padding: 0px;
  /* width: 220px; */
  font-family: 'Segoe UI', 'Arial', sans-serif;
}
.marker-card {
  border-radius: 16px;
  /* box-shadow: 0 2px 12px rgba(32, 127, 165, 0.12); */
  padding: 0 5px 2px 10px;
  /* width: 245px; */
  font-family: 'Segoe UI', 'Arial', sans-serif;
}
.card-title {
  font-size: 18px;
  font-weight: bold;
  color: #207fa5;
  margin-bottom: 8px;
}
.card-type {
  display: inline-block;
  background-color: #cde8f7;
  font-size: 13px;
  color: #1781c1;
  padding: 2px 12px;
  border-radius: 8px;
  margin-bottom: 10px;
  font-weight: 500;
}
.card-row {
  margin: 5px 0;
  font-size: 15px;
}
.label {
  color: #197ba8;
  font-weight: bold;
  margin-right: 4px;
}

select {
  width: auto; /* 讓寬度自動根據內容最長的選項來決定 */
  max-width: 100%; /* 防止內容過長時超出父容器 */
  flex-shrink: 0; /* 確保不會因為空間不足而被擠壓變形 */
  padding-right: 40px;
}
</style>
