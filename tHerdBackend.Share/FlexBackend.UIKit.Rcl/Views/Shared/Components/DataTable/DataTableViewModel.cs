namespace FlexBackend.UIKit.Rcl.Views.Shared.Components.DataTable
{
    public class DataTableViewModel
    {
        // HTML table 的 id（必填）
        public string TableId { get; set; } = "dataTable";

        // 表頭文字（數量必須與 ColumnsJson 對齊；若有子列控制欄，第一格留空字串 ""）
        public IEnumerable<string> Headers { get; set; } = Array.Empty<string>();

        // DataTables columns 的 JSON（直接序列化後丟進來）
        // 例：
        // [
        //   { "className":"dt-control","orderable":false,"data":null,"defaultContent":"" },
        //   { "data":"Name" }, { "data":"Position" }, ...
        // ]
        public string ColumnsJson { get; set; } = "[]";

        // 二擇一：本地資料 JSON（array）或 Ajax
        public string? DataJson { get; set; }   // 例：[{ "Name":"Tiger", ... }, ...]
        public string? AjaxUrl { get; set; }    // 例："/api/employees"
        public string AjaxDataSrc { get; set; } = ""; // 若 API 回 { data:[...] } 就填 "data"

        // 凍結欄位
        public int FrozenLeft { get; set; } = 0;
        public int FrozenRight { get; set; } = 0;

        // FixedHeader 參考的 selector（navbar/topbar）
        public string HeaderOffsetSelector { get; set; } = ".navbar, .topbar";

        // 子列 formatter（前端的全域 JS 函數名稱；例如 "employeeChildFormatter"）
        public string? ChildFormatterFn { get; set; }

        // 高度微調
        public int MinHeight { get; set; } = 240;
        public int Gap { get; set; } = 20;

        // DataTables 語系是否顯示（預設使用內建繁中）
        public bool UseDefaultLanguage { get; set; } = true;

        // 分頁選項
        public int PageLength { get; set; } = 10;
        public IEnumerable<int> LengthMenu { get; set; } = new[] { 5, 10, 25, 50 };
    }
}