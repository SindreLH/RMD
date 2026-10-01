using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace RMD.Business.Services
{
	public interface IEmailService
	{
		Task SendEmailAsync(string to, string subject, string htmlMessage);
	}


	/// <summary>
	/// Sends e-mail through SMTP. Settings: Email:Username, Email:Password (user-secrets in Development,
	/// app settings / environment variables in production), optional Email:Host (default smtp.gmail.com) and Email:Port (587).
	/// </summary>
	public class SmtpEmailService : IEmailService
	{
		private readonly IConfiguration _config;

		public SmtpEmailService(IConfiguration config)
		{
			_config = config;
		}


		public async Task SendEmailAsync(string to, string subject, string htmlMessage)
		{
			var username = _config["Email:Username"];
			var password = _config["Email:Password"];

			if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
				throw new InvalidOperationException("SMTP is not configured: set Email:Username and Email:Password.");

			using var smtpClient = new SmtpClient(_config["Email:Host"] ?? "smtp.gmail.com")
			{
				Port = int.TryParse(_config["Email:Port"], out var port) ? port : 587,
				Credentials = new NetworkCredential(username, password),
				EnableSsl = true,
			};

			using var mail = new MailMessage
			{
				From = new MailAddress(username),
				Subject = subject,
				Body = htmlMessage,
				IsBodyHtml = true
			};

			mail.To.Add(to);

			await smtpClient.SendMailAsync(mail);
		}
	}
}
