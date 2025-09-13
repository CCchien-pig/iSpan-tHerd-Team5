using System;
using System.ComponentModel.DataAnnotations;

namespace FlexBackend.CS.Rcl.Areas.CS.ViewModels
{
    // =======================
    // FAQ（清單）
    // =======================
    public class FaqListItemVM
    {
        [Display(Name = "編號")]
        public int FaqId { get; set; }

        [Display(Name = "標題")]
        public string Title { get; set; } = default!;

        [Display(Name = "答案內容")]
        public string AnswerHtml { get; set; } = default!;

        [Display(Name = "排序")]
        public int OrderSeq { get; set; }

        [Display(Name = "最近發布時間")]
        public DateTime? LastPublishedTime { get; set; }

        [Display(Name = "啟用")]
        public bool IsActive { get; set; }

        [Display(Name = "建立時間")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "修改時間")]
        public DateTime? RevisedDate { get; set; }

        [Display(Name = "分類")]
        public int CategoryId { get; set; }

        [Display(Name = "分類")]
        public string CategoryName { get; set; } = default!;
    }

    // =======================
    // FAQ（建立 / 編輯表單）
    // =======================
    public class FaqEditVM
    {
        [Display(Name = "編號")]
        public int FaqId { get; set; }

        [Required(ErrorMessage = "請輸入標題")]
        [StringLength(300, ErrorMessage = "標題最長 300 字")]
        [Display(Name = "標題")]
        public string Title { get; set; } = default!;

        [Required(ErrorMessage = "請輸入答案內容")]
        [Display(Name = "答案內容")]
        public string AnswerHtml { get; set; } = default!;

        [Required(ErrorMessage = "請選擇分類")]
        [Display(Name = "分類")]
        public int CategoryId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "排序需為 0 以上整數")]
        [Display(Name = "排序")]
        public int OrderSeq { get; set; }

        [Display(Name = "最近發布時間")]
        public DateTime? LastPublishedTime { get; set; }

        [Display(Name = "啟用")]
        public bool IsActive { get; set; }
    }

    // =======================
    // FAQ 分類（清單）
    // =======================
    public class FaqCategoryListVM
    {
        [Display(Name = "分類編號")]
        public int CategoryId { get; set; }

        [Display(Name = "分類名稱")]
        public string CategoryName { get; set; } = default!;

        [Display(Name = "上層分類")]
        public string? ParentCategoryName { get; set; }

        [Display(Name = "排序")]
        public int OrderSeq { get; set; }

        [Display(Name = "啟用")]
        public bool IsActive { get; set; }

        [Display(Name = "建立時間")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "修改時間")]
        public DateTime? RevisedDate { get; set; }

        // 這兩個是衍生資訊（可選，不需要可不使用）
        [Display(Name = "啟用FAQ數")]
        public int ActiveFaqCount { get; set; }

        [Display(Name = "總FAQ數")]
        public int TotalFaqCount { get; set; }
    }

    // =======================
    // FAQ 分類（建立 / 編輯表單）
    // =======================
    public class FaqCategoryEditVM
    {
        [Display(Name = "分類編號")]
        public int CategoryId { get; set; }

        [Display(Name = "上層分類")]
        public int? ParentCategoryId { get; set; }

        [Required(ErrorMessage = "請輸入分類名稱")]
        [StringLength(100, ErrorMessage = "分類名稱最長 100 字")]
        [Display(Name = "分類名稱")]
        public string CategoryName { get; set; } = default!;

        [Range(0, int.MaxValue, ErrorMessage = "排序需為 0 以上整數")]
        [Display(Name = "排序")]
        public int OrderSeq { get; set; }

        [Display(Name = "啟用")]
        public bool IsActive { get; set; }
    }

    // =======================
    // FAQ 關鍵字（清單）
    // =======================
    public class FaqKeywordListVM
    {
        [Display(Name = "編號")]
        public int KeywordId { get; set; }

        [Display(Name = "關鍵字")]
        public string Keyword { get; set; } = default!;

        [Display(Name = "建立時間")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "FAQ")]
        public int FaqId { get; set; }

        [Display(Name = "FAQ標題")]
        public string FaqTitle { get; set; } = default!;
    }

    // =======================
    // FAQ 關鍵字（建立 / 編輯表單）
    // =======================
    public class FaqKeywordEditVM
    {
        [Display(Name = "編號")]
        public int KeywordId { get; set; }

        [Required(ErrorMessage = "請選擇 FAQ")]
        [Display(Name = "FAQ")]
        public int FaqId { get; set; }

        [Required(ErrorMessage = "請輸入關鍵字")]
        [StringLength(100, ErrorMessage = "關鍵字最長 100 字")]
        [Display(Name = "關鍵字")]
        public string Keyword { get; set; } = default!;
    }
}
