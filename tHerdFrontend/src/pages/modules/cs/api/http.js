// src/apis/http.js
import axios from "axios";

const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE || "https://localhost:7103",
  timeout: 15000,
});

// 自動帶 JWT（若有登入）
http.interceptors.request.use((config) => {
  const token = localStorage.getItem("jwt");
  if (token) config.headers.Authorization = `Bearer ${token}`; // 規範要求的格式
  return config;
});

export default http;
