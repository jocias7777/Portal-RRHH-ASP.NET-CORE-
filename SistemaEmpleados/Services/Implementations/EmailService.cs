using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SistemaEmpleados.Services.Interfaces;

namespace SistemaEmpleados.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private IConfiguration _configuration;
        private ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, string cc = null, byte[] attachment = null, string attachmentName = null)
        {
            try
            {
                var smtpHost = _configuration["Email:SmtpHost"];
                var smtpPort = _configuration["Email:SmtpPort"];
                var smtpUser = _configuration["Email:SmtpUser"];
                var smtpPass = _configuration["Email:SmtpPass"];
                var fromEmail = _configuration["Email:FromEmail"];
                var fromName = _configuration["Email:FromName"];

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
                {
                    _logger.LogWarning("Email config not set");
                    return true;
                }

                using (var client = new System.Net.Mail.SmtpClient(smtpHost, int.Parse(smtpPort ?? "587")))
                {
                    client.EnableSsl = true;
                    client.Credentials = new System.Net.NetworkCredential(smtpUser, smtpPass);

                    var message = new System.Net.Mail.MailMessage();
                    message.From = new System.Net.Mail.MailAddress(fromEmail ?? smtpUser, fromName ?? "Portal RRHH");
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;
                    message.To.Add(to);

                    if (attachment != null && !string.IsNullOrEmpty(attachmentName))
                    {
                        var stream = new MemoryStream(attachment);
                        var att = new System.Net.Mail.Attachment(stream, attachmentName);
                        message.Attachments.Add(att);
                    }

                    await client.SendMailAsync(message);
                    _logger.LogInformation("Email sent to " + to);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to " + to);
                return false;
            }
        }

        public async Task<bool> SendEmailWithAttachmentsAsync(string to, string subject, string body, Dictionary<string, byte[]> archivos)
        {
            try
            {
                var smtpHost = _configuration["Email:SmtpHost"];
                var smtpPort = _configuration["Email:SmtpPort"];
                var smtpUser = _configuration["Email:SmtpUser"];
                var smtpPass = _configuration["Email:SmtpPass"];
                var fromEmail = _configuration["Email:FromEmail"];
                var fromName = _configuration["Email:FromName"];

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
                {
                    _logger.LogWarning("Email config not set - simulating");
                    return true;
                }

                using (var client = new System.Net.Mail.SmtpClient(smtpHost, int.Parse(smtpPort ?? "587")))
                {
                    client.EnableSsl = true;
                    client.Credentials = new System.Net.NetworkCredential(smtpUser, smtpPass);

                    var message = new System.Net.Mail.MailMessage();
                    message.From = new System.Net.Mail.MailAddress(fromEmail ?? smtpUser, fromName ?? "Portal RRHH");
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;
                    message.To.Add(to);

                    foreach (var archivo in archivos)
                    {
                        var stream = new MemoryStream(archivo.Value);
                        var att = new System.Net.Mail.Attachment(stream, archivo.Key);
                        message.Attachments.Add(att);
                    }

                    await client.SendMailAsync(message);
                    _logger.LogInformation("Email with attachments sent to " + to);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email with attachments to " + to);
                return false;
            }
        }
    }
}