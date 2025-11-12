namespace tHerdBackend.Core.DTOs.PROD
{
    public class ProductQuestionDto
    {
        public int QuestionId { get; set; }
        public string UserName { get; set; } = "";
        public string QuestionContent { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public List<ProductAnswerDto> Answers { get; set; } = new();
    }

    public class ProductAnswerDto
    {
        public int AnswerId { get; set; }
        public string UserName { get; set; } = "";
        public string AnswerContent { get; set; } = "";
        public bool IsOfficial { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // 用於 Dapper flat join 結果
    public class ProductQAFlatDto
    {
        public int QuestionId { get; set; }
        public int ProductId { get; set; }
        public int UserNumberId { get; set; }
        public string QuestionUserName { get; set; } = "";
        public string QuestionContent { get; set; } = "";
        public DateTime QuestionDate { get; set; }

        public int? AnswerId { get; set; }
        public string? AnswerContent { get; set; }
        public string? AnswerUserName { get; set; }
        public bool IsOfficial { get; set; }
        public DateTime? AnswerDate { get; set; }
    }
}
