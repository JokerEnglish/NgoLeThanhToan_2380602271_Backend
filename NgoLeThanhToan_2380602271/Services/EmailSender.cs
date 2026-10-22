using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace NgoLeThanhToan_2380602271.Services;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body);
}

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _cfg;
    public SmtpEmailSender(IConfiguration cfg) => _cfg = cfg;

    public async Task SendAsync(string to, string subject, string body)
    {
        var s = _cfg.GetSection("Email");
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(s["FromName"], s["Username"]));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(s["Host"], int.Parse(s["Port"]!), SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(s["Username"], s["Password"]);
        await client.SendAsync(msg);
        await client.DisconnectAsync(true);
    }
}