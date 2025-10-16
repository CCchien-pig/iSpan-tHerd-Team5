<script setup>
import { ref, watch, nextTick } from 'vue'
import sfStores from '../assets/mock_sf_stores.json'
import sevenStores from '../assets/mock_711_stores.json'
import blackcatStores from '../assets/mock_blackcat_stores.json'
import postStores from '../assets/mock_post_stores.json'

const apiKey = import.meta.env.VITE_GOOGLE_MAPS_KEY

const logisticsOpts = [
  { value: 'sf', label: '順豐速運', stores: sfStores },
  { value: '711', label: '7-11', stores: sevenStores },
  { value: 'blackcat', label: '黑貓宅急便', stores: blackcatStores },
  { value: 'post', label: '中華郵政', stores: postStores },
]

const selectedLogistics = ref(null)
const selectedCity = ref('全台')
const selectedStoreId = ref('all')
const cityOptions = ref([])
const storeOptions = ref([])
const addressInput = ref('')

const mapRef = ref(null)
const mapCenter = ref({ lat: 23.5, lng: 121 })
const mapZoom = ref(7)
const markers = ref([])
const userLocation = ref(null)
const routePath = ref([])
const distanceText = ref('')
const currentInfoStore = ref(null)
const addressInfo = ref(null) // 顯示搜尋地址內容
const activeMode = ref('query')

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
  nextTick(() => {
    const gmap = mapRef.value?.$mapObject
    if (gmap && filtered.length > 1) {
      const bounds = new window.google.maps.LatLngBounds()
      filtered.forEach((s) => bounds.extend(new window.google.maps.LatLng(s.Latitude, s.Longitude)))
      gmap.fitBounds(bounds)
      const center = gmap.getCenter()
      mapCenter.value = { lat: center.lat(), lng: center.lng() }
      mapZoom.value = gmap.getZoom()
    }
  })
}

function fitBoundsRoute(route, origin, dest) {
  nextTick(() => {
    const gmap = mapRef.value?.$mapObject
    if (gmap && route.length) {
      const bounds = new window.google.maps.LatLngBounds()
      route.forEach((pt) => bounds.extend(new window.google.maps.LatLng(pt.lat, pt.lng)))
      bounds.extend(new window.google.maps.LatLng(origin.lat, origin.lng))
      bounds.extend(new window.google.maps.LatLng(dest.lat, dest.lng))
      gmap.fitBounds(bounds)
      const center = gmap.getCenter()
      mapCenter.value = { lat: center.lat(), lng: center.lng() }
      mapZoom.value = gmap.getZoom()
    }
  })
}

function onQuery() {
  currentInfoStore.value = null // 關掉舊 InfoWindow
  addressInfo.value = null // 關掉舊地址卡片
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
    // 下面這兩行強制重設 mapCenter（多加一層物件，確保 Vue 會 patch）
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
    // 同理小幅變化座標，保證 Vue 判定為新物件
    mapCenter.value = { lat: store.Latitude + Math.random() * 0.00001, lng: store.Longitude }
    mapZoom.value = 17
    routePath.value = []
    userLocation.value = null
    distanceText.value = ''
  }
}
//最近門市模式
async function searchNearestStore() {
  currentInfoStore.value = null // 每次執行查詢都先清空門市卡片
  addressInfo.value = null // 清空舊地址卡片

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

  // 只標註 userLocation，不自動開啟地址卡片
  userLocation.value = geo
  // addressInfo 不設值，留給 marker/地標 click 時才 set

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
function onMarkerClick(store) {
  addressInfo.value = null // 關掉地址卡片
  currentInfoStore.value = store // 開啟門市卡片
}
function handleMarkerClick(store) {
  currentInfoStore.value = null
  addressInfo.value = null
  onMarkerClick(store)
}
function openAddressInfoMarker() {
  // 清掉門市 InfoWindow，打開地址卡片
  currentInfoStore.value = null
  addressInfo.value = {
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
// watch([activeMode, routePath], ([mode, path]) => {
//   console.log('activeMode', mode, 'routePath length', path.length)
// })
</script>

<template>
  <h2>門市地圖</h2>
  <div class="map-container">
    <div class="section">
      <h3 class="section-title">依物流門市篩選</h3>
      <div class="selector-zone">
        <label for="logistics-select">物流商：</label>
        <select id="logistics-select" name="logistics" v-model="selectedLogistics" required>
          <option :value="null" disabled>請選擇</option>
          <option v-for="opt in logisticsOpts" :value="opt.value" :key="opt.value">
            {{ opt.label }}
          </option>
        </select>

        <label for="city-select">地區：</label>
        <select
          id="city-select"
          name="city"
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
      <h3 class="section-title">輸入地名、地址搜尋最近門市</h3>
      <!-- 輸入區 -->
      <form class="search-zone" @submit.prevent="searchNearestStore" autocomplete="off">
        <label for="search-logistics">物流商：</label>
        <select id="search-logistics" name="searchLogistics" v-model="selectedLogistics" required>
          <option :value="null" disabled>請選擇</option>
          <option v-for="opt in logisticsOpts" :value="opt.value" :key="opt.value">
            {{ opt.label }}
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
          @input="sanitize"
        />

        <button type="submit" :disabled="!addressInput || !selectedLogistics">搜尋最近門市</button>
      </form>
      <!-- 範例按鈕 -->
      <div class="example-buttons">
        <span style="margin-right: 8px">範例：</span>
        <button
          class="example-btn"
          type="button"
          @click="fillExampleAddress('桃園市中壢區新生路二段421號')"
        >
          聖德基督學院
        </button>
        <button
          class="example-btn"
          type="button"
          @click="fillExampleAddress('臺北市中正區黎明里北平西路3號')"
        >
          北車
        </button>
      </div>
      <div v-if="distanceText" style="margin-top: 12px; color: blue">
        導航線路徑距離：{{ distanceText }}
      </div>
    </div>

    <GMapMap
      ref="mapRef"
      :api-key="apiKey"
      :key="`${mapCenter.lat}_${mapCenter.lng}_${mapZoom}`"
      :center="mapCenter"
      :zoom="mapZoom"
      style="width: 100%; height: 500px; margin-top: 16px"
      @click="currentInfoStore = null"
    >
      <GMapMarker
        v-for="marker in markers"
        :key="marker.store.StoreID"
        :position="marker.position"
        @click="handleMarkerClick(marker.store)"
      />
      <!-- 使用者位置標記 -->
      <GMapMarker v-if="userLocation" :position="userLocation" @click="openAddressInfoMarker" />
      <!-- 路徑線 -->
      <GMapPolyline
        v-if="activeMode === 'search' && routePath.length > 1"
        :key="routePath.length"
        :path="routePath"
        :options="{ strokeColor: '#0079fd', strokeWeight: 4, strokeOpacity: 0.8 }"
      />
      <!-- 輸入地址卡片 -->
      <GMapInfoWindow
        v-if="addressInfo"
        :position="{ lat: addressInfo.lat, lng: addressInfo.lng }"
        @closeclick="addressInfo = null"
      >
        <div class="address-card">
          <!-- <div class="card-title">搜尋地址</div> -->
          <div class="card-row">
            <span class="label">查詢地址： <br /> </span>{{ addressInfo.address }}
          </div>
          <div class="card-row">
            <span class="label">座標：</span>{{ addressInfo.lat.toFixed(6) }},
            {{ addressInfo.lng.toFixed(6) }}
          </div>
        </div>
      </GMapInfoWindow>
      <!-- 門市卡片 -->
      <GMapInfoWindow
        v-if="currentInfoStore"
        :key="currentInfoStore.StoreID"
        :position="{ lat: currentInfoStore.Latitude, lng: currentInfoStore.Longitude }"
        @closeclick="currentInfoStore = null"
      >
        <!-- 門市卡片內容 -->
        <div class="marker-card">
          <div class="card-title">{{ currentInfoStore.StoreName }}</div>
          <div class="card-type">{{ currentInfoStore.Type }}</div>
          <div class="card-row">
            <span class="label">分店編號：</span>{{ currentInfoStore.StoreID }}
          </div>
          <div class="card-row">
            <span class="label">地址：</span>{{ currentInfoStore.Address }}
          </div>
          <div class="card-row"><span class="label">城市：</span>{{ currentInfoStore.City }}</div>
          <div class="card-row"><span class="label">電話：</span>{{ currentInfoStore.Phone }}</div>
          <div class="card-row">
            <span class="label">座標：</span>
            <span>
              {{ currentInfoStore.Latitude.toFixed(4) }},
              {{ currentInfoStore.Longitude.toFixed(4) }}
            </span>
          </div>
        </div>
      </GMapInfoWindow>
    </GMapMap>
  </div>
</template>

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
</style>
