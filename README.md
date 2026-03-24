tHerd一個大型藥妝保健食品購物網站

本專案為資展國際c#全端班的期末專題，平台描述及技術架構如下：

一、平台背景及功能摘要：

tHerd全方位健康電商參考自iHerb這個專門販售健康產品及藥妝的電商，由於現代人口餘命的增加，以及對對外表的重視度的提高，健康意識在台灣也更加興盛，我們組內自己也會使用保養/保健品，因此選擇tHerd全方位健康電商做為主題。

tHerd的網站開發分成前台及後台，分成會員、活動、商品、供應商、訂單、客服以及內容管理共七個模組，網站於開發時有規劃基本SEO要求(slug、meta title、meta description、img alt)。

我負責整體專案的GCP線上雲端資料庫串聯，以及github flow規劃並且串聯slack做分支更新及PR提醒，並且在會員模組的部分，我在後台開發權限角色管理、員工管理以及會員管理，具備特定權限的員工只能看到特定功能的管理頁面，無法看到其他無權操作頁，而前臺則納入現代網頁基本上會使用的各種登入註冊方式：一般註冊登入(有強密碼要求)、google第三方登入(OAuth2.0)、信箱驗證、雙因素驗證、忘記密碼寄信驗證碼，登入後可使用推薦碼，並且針對會員等級有做依據消費進行升等。



二、技術架構：

前台：採用前端Vue.js結合後端ASP.NET CORE架構的前後端分離架構，API採用restful API，並透過Axios發送前端請求。 驗證機制透過JWT傳遞登入資訊交由.NET CORE Identity驗證，驗證成功後發送access token做使用者資訊並且使用refresh token避免頻繁登入問題。



後台：使用RCL架構進行後台開發，其前端技術為Razor page，驗證機制透過.NET CORE Identity進行，登入者資料的靜態快照透過加密cookie存取。



驗證機制詳細說明：

在我的專題中，我實作了一套基於 ASP.NET Core Identity 結合 JWT (JSON Web Token) 的完整身分驗證系統。

	在後端部分： 我使用 Identity 框架來管理使用者、角色 (Roles) 與聲明 (Claims)，並實作了標準的資安功能，包含透過 IEmailSender 寄發驗證信、重設密碼，以及整合了雙因子驗證 (2FA) 來提升安全性。 為了兼顧安全與體驗，我採用了 雙 Token 機制：

1.Access Token：效期短，用於每次 API請求的驗證。

2.Refresh Token：效期長，用於在 Access Token 過期時換發新憑證。

	在前端部分 (Vue + Pinia)： 我封裝了一個 useAuthStore 來集中管理使用者的登入狀態、Token 以及訪客 (Guest) 識別。

	為了讓使用者有流暢的體驗，我在 Axios 的 Interceptor (攔截器) 中實作了自動化的 Token 管理：

1.Request 攔截：自動從 Pinia 或 LocalStorage 讀取 Token 並寫入 Header。

2.Response 攔截：這是最關鍵的部分。當 API 回傳 401 Unauthorized 時，系統會自動暫停當前的請求，在背景呼叫 Refresh API 換取新 Token，成功後再重送原本失敗的請求。這讓使用者在操作過程中完全感覺不到 Token 已經過期，達到了無感換證的效果。



在實作前後端分離的驗證時，最大的挑戰在於處理 Token 過期時的併發請求 (Concurrency Requests) 問題。

設想情境：使用者進入頁面，同時觸發了 5 個 API 請求，而此時 Access Token 剛好過期。如果沒有妥善處理，這 5 個請求會同時收到 401 錯誤，並導致前端瞬間發出 5 次 Refresh Token 請求。這不僅浪費資源，還可能因為 Race Condition 導致 Refresh Token 失效或被後端封鎖。

	我的解決方案是： 我在 Axios 的 response interceptor 中實作了一個 佇列 (Queue) 機制。當第一個 401 錯誤發生時，我將 isRefreshing 旗標設為 true，並開始呼叫換證 API。

隨後到達的其他 401 請求，會被攔截並放入一個 pendingQueue (Promise 陣列) 中等待。

當換證成功拿到新的 Token 後，我再執行 flushQueue，將佇列中的請求喚醒，並使用新的 Token 重試。若換證失敗，則統一執行登出並導向登入頁。

	這段邏輯確保了同一時間只會有一次 Refresh Token 的網路請求，大幅提升了系統的穩定性。
