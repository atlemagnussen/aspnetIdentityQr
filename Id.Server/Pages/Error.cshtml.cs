using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Id.Server.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    // Only known codes are shown. Free text from the query string would let anyone put a message on this page.
    private static readonly Dictionary<string, string> Messages = new()
    {
        ["invalid_request"] = "The application sent an invalid sign-in request.",
        ["unauthorized_client"] = "The application isn't registered or isn't allowed to sign in here.",
    };

    public string? RequestId { get; set; }
    public string? Message { get; set; }

    public void OnGet(string? error)
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        Message = error is null ? null : Messages.GetValueOrDefault(error);
    }
}
