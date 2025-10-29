using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.Common
{
	public class RegisterDto
	{
		public string Email { get; set; } = default!;
		public string Password { get; set; } = default!;
		public string LastName { get; set; } = default!;
		public string FirstName { get; set; } = default!;
		public string PhoneNumber { get; set; } = default!; // 新增
		public string Gender { get; set; } = default!;      // 新增：建議 '男' / '女'
	}
}
