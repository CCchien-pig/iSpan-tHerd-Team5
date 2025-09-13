using System;
using System.ComponentModel.DataAnnotations;

namespace FlexBackend.CS.Rcl.Areas.CS.ViewModels
{
    // 列表用
    public class FaqListItemVM
    {
        [Display(Name = "編號")] public int FaqId { get; set; }
        [Display(Name = "標題")] public string Title { get; set; }
        [Display(Name = "答案內容")] public string AnswerHtml { get; set; }
        [Display(Name = "排序")] public int? OrderSeq { get; set; }
        [Display(Name = "最近發布時間")] public DateTime? LastPublishedTime { get; set; }
        [Display(Name = "啟用")] public bool IsActive { get; set; }
        [Display(Name = "建立時間")] public DateTime CreatedDate { get; set; }
        [Display(Name = "修改時間")] public DateTime? RevisedDate { get; set; }
        [Display(Name = "分類")] public int CategoryId { get; set; }
        [Display(Name = "分類")] public string CategoryName { get; set; }
    }

    // 建立/編輯用
    public class FaqEditVM
    {
        [Display(Name = "編號")] public int FaqId { get; set; }

        [Display(Name = "標題")]
        [Required(ErrorMessage = "請輸入標題")]
        [StringLength(300, ErrorMessage = "標題最長 300 字")]
        public string Title { get; set; }

        [Display(Name = "答案內容")]
        [Required(ErrorMessage = "請輸入答案內容")]
        public string AnswerHtml { get; set; }

        [Display(Name = "狀態")] public int? Status { get; set; }

        [Display(Name = "分類")]
        [Required(ErrorMessage = "請選擇分類")]
        public int CategoryId { get; set; }

        [Display(Name = "排序")] public int? OrderSeq { get; set; }
        [Display(Name = "最近發布時間")] public DateTime? LastPublishedTime { get; set; }
        [Display(Name = "啟用")] public bool IsActive { get; set; }
        [Display(Name = "建立時間")] public DateTime CreatedDate { get; set; }
        [Display(Name = "修改時間")] public DateTime? RevisedDate { get; set; }
        [Display(Name = "分類")] public string CategoryName { get; set; }
    }
}
