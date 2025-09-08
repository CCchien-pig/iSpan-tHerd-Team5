using Microsoft.Data.SqlClient;

namespace FlexBackend.Core.Exceptions
{
    public static class ErrorHandler
    {
        /// <summary>
        /// 取得錯誤日誌的路徑
        /// </summary>
        private static readonly string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"ErrorLog_{DateTime.Now:yyyy-MM-dd}.log");

        /// <summary>
        /// 錯誤處理方法，將錯誤寫入日誌並顯示友善訊息
        /// </summary>
        /// <param name="ex">錯誤訊息</param>
        /// <param name="context">錯誤發生位置的描述（例如：模組名稱、操作說明）</param>
        public static void HandleErrorMsg(Exception ex)
        {
            // 取得友善的錯誤訊息
            string errMsg = GetFriendlyMessage(ex);

            // 1. 寫入 Log（未來可改為 NLog / Serilog）
            string msg = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} -【{errMsg}】\n{ex.StackTrace}";
            try
            {
                File.AppendAllText(logPath, msg + "\n\n");
            }
            catch
            {
                // 若寫入失敗也不擋主流程（可再記錄到 Event Viewer 或忽略）
            }

            // 2. Debug 模式顯示訊息及Log路徑
    //#if DEBUG
    //            MessageBox.Show($"錯誤位置：{GetErrorSource(ex)}\n\n{errMsg}\n\n詳情請見Log:\n{AppDomain.CurrentDomain.BaseDirectory}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
    //#else // 3. Release 模式顯示 MessageBox
    //                MessageBox.Show("發生錯誤，請聯絡資訊人員。", "系統錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
    //#endif
        }

        /// <summary>
        /// 取得錯誤來源的描述
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        private static string GetErrorSource(Exception ex)
        {
            var stackLines = ex.StackTrace?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (stackLines != null && stackLines.Length > 0)
            {
                string firstLine = stackLines[0];

                // 範例：at ISpan.eMiniHR.WinApp.Forms.LoginForm.loginBtn_Click(Object sender, EventArgs e) in ...
                int atIndex = firstLine.IndexOf("at ");
                int inIndex = firstLine.IndexOf(" in ");

                if (atIndex >= 0 && inIndex > atIndex)
                {
                    return firstLine.Substring(atIndex + 3, inIndex - atIndex - 3).Trim();
                }
            }

            return "未知來源";
        }

        /// <summary>
        /// 取得友善的錯誤訊息
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        private static string GetFriendlyMessage(Exception ex)
        {
            return ex switch
            {
                SqlException sqlEx => GetSqlExceptionMessage(sqlEx),
                FileNotFoundException => "找不到指定的檔案，請確認檔案路徑是否正確。",
                DirectoryNotFoundException => "找不到指定的資料夾，可能是路徑錯誤或資料夾不存在。",
                UnauthorizedAccessException => "您沒有足夠的權限執行此操作，請使用系統管理員權限再試一次。",
                IOException => "檔案存取錯誤，可能是檔案被鎖定或磁碟空間不足。",
                NullReferenceException => "出現空值錯誤，請檢查是否有物件尚未初始化。",
                ArgumentNullException => "發現必要參數為空值，請檢查輸入是否正確。",
                ArgumentException => "參數錯誤，請確認傳入的值是否有效。",
                FormatException => "資料格式錯誤，請確認數值或日期格式是否正確。",
                IndexOutOfRangeException => "索引超出範圍，請檢查集合或陣列的邊界。",
                InvalidOperationException => "操作無效，物件當前狀態不支援此操作。",
                NotImplementedException => "此功能尚未實作，請等待後續版本更新。",
                TimeoutException => "操作逾時，請確認網路或伺服器是否正常運作。",
                _ => ex.Message
            };
        }

        /// <summary>
        /// 取得 SQL 錯誤的友善訊息
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        private static string GetSqlExceptionMessage(SqlException ex)
        {
            return ex.Number switch
            {
                53 => "無法連線到資料庫伺服器，請確認伺服器名稱與網路連線。",
                18456 => "資料庫登入失敗，請確認帳號與密碼是否正確。",
                547 => "資料限制衝突（例如外鍵關聯失敗），請檢查資料內容。",
                2627 or 2601 => "資料重複，違反唯一鍵或索引限制。",
                _ => "資料庫發生錯誤，請確認連線與語法是否正確。"
            };
        }
    }
}
