using tHerdBackend.Services.USER;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace tHerdBackend.USER.Rcl
{
	public class EmailSender : IEmailSender
	{

		private readonly SmtpSettings _smtpSettings;

		// 透過建構函式注入 IOptions<SmtpSettings>
		public EmailSender(IOptions<SmtpSettings> smtpSettings)
		{
			_smtpSettings = smtpSettings.Value;
		}

		public async Task SendEmailAsync(string email, string subject, string htmlMessage)
		{
			var mail = new MailMessage();
			mail.From = new MailAddress(_smtpSettings.smtpMailAddress);
			mail.To.Add(email);
			mail.Subject = subject;
			mail.IsBodyHtml = true;
			mail.Body = htmlMessage;

			SmtpClient client = new SmtpClient("smtp.gmail.com");
			//SmtpClient client = new SmtpClient("smtp.live.com");
			client.Port = 587;
			client.UseDefaultCredentials = false;
			client.Credentials = new NetworkCredential(_smtpSettings.smtpMailAddress, _smtpSettings.smtpMailPassword);
			client.EnableSsl = true;
			client.Send(mail);
		}
	}
}
