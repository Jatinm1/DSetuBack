//using DealerSetu_Data.Models;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Mail;
//using System.Net;
//using System.Text;
//using System.Threading.Tasks;
//using DealerSetu_Services.IServices;

//namespace DealerSetu_Services.Services
//{
//    public class EmailService : IEmailService
//    {
//        private readonly EmailSettings _emailSettings;
//        private readonly ILogger<EmailService> _logger;

//        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
//        {
//            _emailSettings = emailSettings.Value;
//            _logger = logger;
//        }

//        public async Task SendEmailAsync(string subject, List<EmailModel> toEmails, List<EmailModel> ccEmails, string message, string requestNo, string date)
//        {
//            try
//            {
//                using (var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.Port))
//                {
//                    client.UseDefaultCredentials = false;
//                    client.Credentials = new NetworkCredential(_emailSettings.UserName, _emailSettings.Password);
//                    client.EnableSsl = _emailSettings.EnableSsl;

//                    var mailMessage = new MailMessage
//                    {
//                        From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
//                        Subject = subject,
//                        Body = FormatEmailBody(message, requestNo, date),
//                        IsBodyHtml = true
//                    };

//                    // Add TO recipients
//                    foreach (var email in toEmails)
//                    {
//                        if (!string.IsNullOrEmpty(email?.Email))
//                        {
//                            mailMessage.To.Add(new MailAddress(email.Email, email.Name));
//                        }
//                    }

//                    // Add CC recipients
//                    foreach (var email in ccEmails)
//                    {
//                        if (!string.IsNullOrEmpty(email?.Email))
//                        {
//                            mailMessage.CC.Add(new MailAddress(email.Email, email.Name));
//                        }
//                    }

//                    if (mailMessage.To.Count > 0)
//                    {
//                        await client.SendMailAsync(mailMessage);
//                        _logger.LogInformation($"Email sent successfully to {string.Join(", ", mailMessage.To)}");
//                    }
//                    else
//                    {
//                        _logger.LogWarning("No valid recipients found. Email not sent.");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error sending email");
//                // Don't throw the exception as email sending failure shouldn't stop the main process
//            }
//        }

//        private string FormatEmailBody(string message, string requestNo, string date)
//        {
//            return $@"
//<!DOCTYPE html>
//<html>
//<head>
//    <style>
//        body {{ font-family: Arial, sans-serif; }}
//        .header {{ background-color: #f0f0f0; padding: 10px; }}
//        .content {{ padding: 20px; }}
//        .footer {{ background-color: #f0f0f0; padding: 10px; font-size: 12px; }}
//    </style>
//</head>
//<body>
//    <div class='header'>
//        <h2>SETU PORTAL - Request {requestNo}</h2>
//        <p>Date: {date}</p>
//    </div>
//    <div class='content'>
//        {message}
//    </div>
//    <div class='footer'>
//        <p>This is an automated message. Please do not reply to this email.</p>
//        <p>© {DateTime.Now.Year} SETU PORTAL</p>
//    </div>
//</body>
//</html>";
//        }
//    }
//}
