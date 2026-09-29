using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace RMD.Business.Services
{
	public interface IEmailService
	{
		Task SendEmailAsync(string to, string subject, string htmlMessage);
	}


	public class SmtpEmailService : IEmailService
	{
		private readonly IConfiguration _config;

		public SmtpEmailService(IConfiguration config)
		{
			_config = config;
		}


		public async Task SendEmailAsync(string to, string subject, string htmlMessage)
		{
			Console.WriteLine("SMTP Username: " + _config["Email:Username"]);
			Console.WriteLine("SMTP Password exists: " +
				(!string.IsNullOrEmpty(_config["Email:Password"])));



			var smtpClient = new SmtpClient("smtp.gmail.com")
			{
				Port = 587,
				Credentials = new NetworkCredential
				(
					_config["Email:Username"],
					_config["Email:Password"]
				),
				EnableSsl = true,
			};

			var mail = new MailMessage
			{
				From = new MailAddress(_config["Email:Username"]),
				Subject = subject,
				Body = htmlMessage,
				IsBodyHtml = true
			};

			mail.To.Add(to);

			await smtpClient.SendMailAsync(mail);
		}


	}
}
