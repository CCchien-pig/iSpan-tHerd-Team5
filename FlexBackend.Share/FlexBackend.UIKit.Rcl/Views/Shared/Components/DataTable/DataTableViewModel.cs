namespace FlexBackend.UIKit.Rcl.Views.Shared.Components.DataTable
{
    public class DataTableViewModel
    {
        /// 唯一表格 Id（必要）
        public string TableId { get; set; } = "dataTable";

        /// thead 顯示用標題（含第一欄子列控制空白欄位可傳空字串）
        public IEnumerable<string>? Headers { get; set; }

        /// DataTables columns 設定（JSON 字串，直接丟進 JS）
        public string? ColumnsJson { get; set; }

        /// 靜態資料（後端塞進來）→ 若你要用 AJAX，就不要給 DataJson，改在頁面上自己初始化 ajax
        public string? DataJson { get; set; }

        /// 是否顯示第一欄展開控制（dt-control）
        public bool EnableChildRow { get; set; } = true;

        /// 子列要顯示哪些欄位（例如 Email、Notes）
        public IEnumerable<string>? ChildFields { get; set; }

        /// 子列欄位顯示名稱（顯示「Email：」「備註：」）
        public Dictionary<string, string>? ChildFieldLabels { get; set; }

        /// 介面文字（語系）
        public string LanguageJson { get; set; } =
            "{ \"processing\":\"處理中...\",\"loadingRecords\":\"載入中...\",\"lengthMenu\":\"每頁顯示 _MENU_ 筆\",\"zeroRecords\":\"沒有符合的資料\",\"info\":\"第 _START_ ~ _END_ 筆，共 _TOTAL_ 筆\",\"infoEmpty\":\"第 0 ~ 0 筆，共 0 筆\",\"infoFiltered\":\"(從 _MAX_ 筆資料中篩選)\",\"search\":\"搜尋：\",\"paginate\":{\"first\":\"第一頁\",\"last\":\"最後一頁\",\"next\":\"下一頁\",\"previous\":\"上一頁\"},\"aria\":{\"sortAscending\":\": 升冪排列\",\"sortDescending\":\": 降冪排列\"}}";

        /// 每頁筆數與選單
        public int PageLength { get; set; } = 10;
        public IEnumerable<int> LengthMenu { get; set; } = new[] { 5, 10, 25, 50 };

        /// 凍結欄設定
        public int FixedLeftColumns { get; set; } = 1;
        public int FixedRightColumns { get; set; } = 0;

        /// 顏色主題（目前提供 purple）
        public string Theme { get; set; } = "purple";
    }
}