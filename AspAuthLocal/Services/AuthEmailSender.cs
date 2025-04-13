using System.ComponentModel;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;

namespace AspAuthLocal.Services;

public class AuthEmailSender : IEmailSender<IdentityUser>
{
    static MailAddress From = new MailAddress("noreply@atle.guru","Atle " + (char)0xD8+ " Guru", System.Text.Encoding.UTF8);
    static private bool MailSent = false;

    public Task SendConfirmationLinkAsync(IdentityUser user, string email, string confirmationLink)
    {
        var subject = "Confirm your email";
        var body = $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(confirmationLink)}'>clicking here</a>.";
        SendMail(email, subject, body);
        return Task.FromResult(0);
    }

    public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string resetCode)
    {
        throw new NotImplementedException();
    }

    public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string resetLink)
    {
        throw new NotImplementedException();
    }

    private static void SendMail(string email, string subject, string body)
    {
        SmtpClient client = new();

        MailAddress to = new(email);

        MailMessage message = new(From, to);
        message.Subject = subject;
        message.Body = body;
        message.BodyEncoding =  System.Text.Encoding.UTF8;

        client.SendCompleted += new SendCompletedEventHandler(SendCompletedCallback);
        string userState = "test message1";
        client.SendAsync(message, userState);

    }
    private static void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
    {
        // Get the unique identifier for this asynchronous operation.
        String token = (string) e.UserState;

        if (e.Cancelled)
        {
            Console.WriteLine("[{0}] Send canceled.", token);
        }
        if (e.Error != null)
        {
            Console.WriteLine("[{0}] {1}", token, e.Error.ToString());
        } else
        {
            Console.WriteLine("Message sent.");
        }
        MailSent = true;
    }
}