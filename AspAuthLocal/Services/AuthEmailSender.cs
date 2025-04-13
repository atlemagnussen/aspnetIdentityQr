using System.Text.Encodings.Web;
using AspAuth.Lib.Services;
using AspAuth.Liv.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AspAuthLocal.Services;

public class AuthEmailSender : IEmailSender<IdentityUser>
{
    private readonly EmailSettings _emailSettings;

    public AuthEmailSender(IOptions<EmailSettings> options)
    {
        _emailSettings = options.Value;
    }
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
        using var client = new MailKitSender(_emailSettings);
        await client.SendMail(to, subject, body);
    }
}