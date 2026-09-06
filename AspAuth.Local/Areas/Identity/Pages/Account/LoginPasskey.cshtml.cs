using AspAuth.Lib.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspAuth.Local.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginPasskeyModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginPasskeyModel> _logger;

        public LoginPasskeyModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginPasskeyModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }
        
        public IActionResult OnGet(string? returnUrl = null) => RedirectToPage("./Login", new { ReturnUrl = returnUrl });

        public async Task<IActionResult> OnPostAsync(string credentialJson, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (string.IsNullOrWhiteSpace(credentialJson))
            {
                ErrorMessage = "Passkey credential is missing.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var result = await _signInManager.PasskeySignInAsync(credentialJson);
            if (result.Succeeded)
            {
                if (Input.RememberMe)
                {
                    // PasskeySignInAsync does not accept isPersistent. Re-issue the app cookie as persistent.
                    var user = await _userManager.GetUserAsync(User);
                    if (user is not null)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: true, authenticationMethod: "Passkey");
                    }
                    else
                    {
                        _logger.LogWarning("Passkey sign-in succeeded but user could not be resolved for persistent cookie.");
                    }
                }

                _logger.LogInformation("User logged in with passkey.");
                return Redirect(returnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out during passkey sign-in.");
                return RedirectToPage("./Lockout");
            }

            if (result.IsNotAllowed)
            {
                ErrorMessage = "Passkey sign-in is not allowed for this account.";
                _logger.LogInformation("Passkey sign-in not allowed.");
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            ErrorMessage = "Invalid passkey login attempt.";
            _logger.LogInformation("Invalid passkey login attempt.");
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }
    }
}
