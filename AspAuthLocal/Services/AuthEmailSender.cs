using System.Text.Encodings.Web;
using AspAuth.Lib.Services;
using Microsoft.AspNetCore.Identity;

namespace AspAuthLocal.Services;

public class AuthEmailSender : IEmailSender<IdentityUser>
{
    public async Task SendConfirmationLinkAsync(IdentityUser user, string email, string confirmationLink)
    {
        var subject = "Confirm your email";
        // <a href='{HtmlEncoder.Default.Encode( for html
        var body = $"Please confirm your account by {confirmationLink}";
        await SendMail(email, subject, body);
    }

    public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string resetCode)
    {
        throw new NotImplementedException();
    }

    public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string resetLink)
    {
        throw new NotImplementedException();
    }

    private async Task SendMail(string to, string subject, string body)
    {
        using var client = new MailKitSender("mx.atle.guru", 587, "hello@atle.guru", "xx");
        await client.SendMail(to, subject, body);
    }
}