namespace SistemaEmpleados.Services.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body, string? cc = null, byte[]? attachment = null, string? attachmentName = null);
    Task<bool> SendEmailWithAttachmentsAsync(string to, string subject, string body, Dictionary<string, byte[]> archivos);
}