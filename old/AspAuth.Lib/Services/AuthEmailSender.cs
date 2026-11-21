using AspAuth.Lib.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AspAuth.Lib.Services;

public class AuthEmailSender : IEmailSender<ApplicationUser>
{
    private readonly EmailSettings _emailSettings;

    public AuthEmailSender(IOptions<EmailSettings> options)
    {
        _emailSettings = options.Value;
    }
    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var subject = "Confirm your email";
        // <a href='{HtmlEncoder.Default.Encode( for html
        var body = $"Please confirm your account by {confirmationLink}";
        await SendMail(email, subject, body);
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        throw new NotImplementedException();
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        throw new NotImplementedException();
    }

    private async Task SendMail(string to, string subject, string body)
    {
        using var client = new MailKitSender(_emailSettings);
        await client.SendMail(to, subject, body);
    }
}