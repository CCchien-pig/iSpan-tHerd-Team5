namespace tHerdBackend.Core.DTOs.Common
{
    /// <summary>
    /// 使用者姓名
    /// </summary>
    public partial class UserNameInfoDto
    {
        /// <summary>
        /// 使用者編號
        /// </summary>
        public int UserNumberId { get; set; }

        /// <summary>
        /// 姓氏
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// 全名
        /// </summary>
        public string FullName => $"{LastName} {FirstName}";
    }
}
