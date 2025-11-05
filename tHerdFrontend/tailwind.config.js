/** @type {import('tailwindcss').Config} */
export default {
  content: [
    './index.html',
    './src/**/*.{vue,js,ts,jsx,tsx}', // <-- 這一行是關鍵
  ],
  theme: {
    extend: {},
  },
  plugins: [],
}
