using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.Services.USER
{
	public class SmtpSettings
	{
		public required string smtpMailAddress { get; set; }
		public required string smtpMailPassword { get; set; }
	}
}
