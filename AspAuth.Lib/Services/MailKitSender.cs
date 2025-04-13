using MimeKit;
using MailKit.Net.Smtp;
using AspAuth.Liv.Models;

namespace AspAuth.Lib.Services;

public class MailKitSender : IDisposable
{
    private readonly string Server;
    private readonly int Port;
    private readonly string FromUser;
    private readonly MailboxAddress FromAddress;
    private readonly SmtpClient SmtpClient;

    public MailKitSender(EmailSettings settings) : this(settings.Server, settings.Port, settings.From, settings.Password)
    {
    }

    public MailKitSender(string server, int port, string fromUser, string password)
    {
        Server = server;
        Port = port;
        FromUser = fromUser;
        FromAddress = new MailboxAddress("Auth Guru", fromUser);

        SmtpClient = new SmtpClient();
        SmtpClient.Connect(server, Port, false);
        SmtpClient.Authenticate(FromUser, password);
    }

    public async Task<string> SendMail(string to, string subject, string mail)
    {
        var message = new MimeMessage ();
        message.From.Add(FromAddress);

        message.To.Add(new MailboxAddress(to, to));

        message.Subject = subject;

        message.Body = new TextPart ("plain") { Text = mail };

        var res = await SmtpClient.SendAsync(message);
        return res;
    }

    public void Dispose()
    {
        SmtpClient.Disconnect(true);
        SmtpClient.Dispose();
    }
}
