using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace PerformansTakip.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            _smtpUsername = _configuration["EmailSettings:Username"] ?? "";
            _smtpPassword = _configuration["EmailSettings:Password"] ?? "";
            _fromEmail = _configuration["EmailSettings:FromEmail"] ?? "";
        }

        public async Task SendPasswordResetEmail(string toEmail, string newPassword)
        {
            try
            {
                using (var client = new SmtpClient(_smtpServer, _smtpPort))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                    client.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_fromEmail),
                        Subject = "Performans Takip - Şifre Sıfırlama",
                        Body = $"Merhaba,\n\nYeni şifreniz: {newPassword}\n\nBu şifreyi kullanarak sisteme giriş yapabilirsiniz.\n\nGüvenliğiniz için lütfen giriş yaptıktan sonra şifrenizi değiştirin.\n\nSaygılarımızla,\nPerformans Takip Ekibi",
                        IsBodyHtml = false
                    };

                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("E-posta gönderilirken bir hata oluştu.", ex);
            }
        }
    }
} 