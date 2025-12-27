using System.ComponentModel.DataAnnotations;
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
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ExternalLoginModel> _logger;

        public LoginPasskeyModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<ExternalLoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }
        
        public IActionResult OnGet() => RedirectToPage("./Login");

        public async Task<IActionResult> OnPostAsync(string credentialJson, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            
            var result = await _signInManager.PerformPasskeyAssertionAsync(credentialJson);
            if (!result.Succeeded)
            {
                ErrorMessage = result.Failure.Message;
                _logger.LogError(result.Failure, ErrorMessage);
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var user = await _userManager.FindByPasskeyIdAsync(result.Passkey.CredentialId);
            if (user is null)
            {
                ErrorMessage = "User not found";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            await _signInManager.SignInAsync(user, isPersistent: Input.RememberMe, "hwk");

            // await _signInManager.PasskeySignInAsync(credentialJson)
            // if (result.IsNotAllowed)
            // {
            //     ErrorMessage = $"Not allowed";
            //     _logger.LogInformation(ErrorMessage);
            //     return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            // }
            // if (result.IsLockedOut)
            // {
            //     ErrorMessage = $"Locked out";
            //     _logger.LogInformation(ErrorMessage);
            //     return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            // }
            // if (!result.Succeeded)
            // {
            //     ErrorMessage = "Unsuccessful";
            //     _logger.LogInformation(ErrorMessage);
            //     return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            // }

            _logger.LogInformation("Authentication passkey OK");
            return LocalRedirect(returnUrl);
        }
    }
}
