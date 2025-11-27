using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class EmailService
{
    private readonly string _smtpServer = "smtp.gmail.com";
    private readonly int _smtpPort = 587;
    private readonly string _fromEmail = "matveybelskiy950@gmail.com";
    private readonly string _fromName = "MyQueue";
    private readonly string _password = "gcehxbgdffqycazx"; // без пробелов!

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        try
        {
            using (var client = new SmtpClient(_smtpServer, _smtpPort))
            {
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(_fromEmail, _password);
                //client.TargetName = "STARTTLS/smtp.gmail.com"; // <== добавь это

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);
                await client.SendMailAsync(mailMessage);

                Console.WriteLine("✅ Письмо успешно отправлено");
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Ошибка: " + ex.Message);
            return false;
        }
    }
}
