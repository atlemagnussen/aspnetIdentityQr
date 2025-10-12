using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AspAuth.Lib.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspAuth.Local.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<ExternalLoginModel> _logger;
        private readonly IAuthenticationSchemeProvider _schemes;

        public ExternalLoginModel(
            SignInManager<IdentityUser> signInManager,
            ILogger<ExternalLoginModel> logger,
            IAuthenticationSchemeProvider schemes)
        {
            _signInManager = signInManager;
            _logger = logger;
            _schemes = schemes;
        }

        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string? ProviderDisplayName { get; set; }

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string? Email { get; set; }
        }
        
        public IActionResult OnGet() => RedirectToPage("./Login");

        public IActionResult OnPost(string provider, string? returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            //var info = await _signInManager.GetExternalLoginInfoAsync();
            var info = await GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal!.Identity!.Name, info.LoginProvider);
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }
            
            // If the user does not have an account, then ask the user to create an account.
            ReturnUrl = returnUrl;
            ProviderDisplayName = info.ProviderDisplayName!;
            if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
            {
                Input = new InputModel
                {
                    Email = info.Principal.FindFirstValue(ClaimTypes.Email)
                };
            }
            return Page();
            
        }

        private const string LoginProviderKey = "LoginProvider";
        private async Task<ExternalLoginInfo?> GetExternalLoginInfoAsync()
        {
            var auth = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
            var items = auth?.Properties?.Items;
            if (auth?.Principal == null || items == null)
            {
                _logger.LogWarning("No external principal or property items");
                return null;
            }

            if (!items.TryGetValue(LoginProviderKey, out var providerName))
            {
                _logger.LogWarning($"could not find {LoginProviderKey} in items");
                return null;
            }

            if (string.IsNullOrWhiteSpace(providerName))
            {
                _logger.LogWarning("ProviderName was empty");
                return null;
            }

            // if (expectedXsrf != null)
            // {
            //     if (!items.TryGetValue(XsrfKey, out var userId) || userId != expectedXsrf)
            //         return null;
            // }

            var allClaims = auth.Principal.Claims.ToList();
            // foreach (var claim in allClaims)
            // {
            //     _logger.LogInformation($"Claim Type={claim.Type} = Value = {claim.Value}, ValueType = {claim.ValueType}, Issuer = {claim.Issuer}, OriginalIssuer = {claim.OriginalIssuer}, Subject = {claim.Subject}");
            //     if (claim.Properties is not null)
            //     {
            //         foreach (var prop in claim.Properties)
            //             _logger.LogInformation($"Prop {prop.Key} = {prop.Value}");
            //     }
            // }

            var providerUserId = ClaimsHelper.GetValueByType(allClaims, ClaimTypes.NameIdentifier, "sub");
            if (providerUserId is null)
            {
                _logger.LogWarning("Got no identifier from external provider user");
                return null;
            }

            var providerDisplayName = await GetExternalAuthenticationDisplayName(providerName);
            
            return new ExternalLoginInfo(auth.Principal, providerName, providerUserId, providerDisplayName)
            {
                AuthenticationTokens = auth.Properties?.GetTokens(),
                AuthenticationProperties = auth.Properties
            };
        }

        private async Task<string> GetExternalAuthenticationDisplayName(string providerName)
        {
            var schemes = await _schemes.GetAllSchemesAsync();
            var provider = schemes.FirstOrDefault(s => s.Name == providerName);
            if (provider is not null && !string.IsNullOrWhiteSpace(provider.DisplayName))
                return provider.DisplayName;
            return providerName;
        }
        // disable auto registration options entirely
        // public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        // {
        //     returnUrl = returnUrl ?? Url.Content("~/");
        //     // Get the information about the user from the external login provider
        //     //var info = await _signInManager.GetExternalLoginInfoAsync();
        //     var info = await GetExternalLoginInfoAsync();
        //     if (info == null)
        //     {
        //         ErrorMessage = "Error loading external login information during confirmation.";
        //         return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        //     }

            
        //     if (ModelState.IsValid)
        //     {
        //         var user = CreateUser();

        //         await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
        //         await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

        //         var result = await _userManager.CreateAsync(user);
        //         if (result.Succeeded)
        //         {
        //             // var lo = await _userManager.GetLoginsAsync(user);
        //             // lo.Any(l => l.LoginProvider == info.LoginProvider)
        //             result = await _userManager.AddLoginAsync(user, info);
        //             if (result.Succeeded)
        //             {
        //                 _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

        //                 var userId = await _userManager.GetUserIdAsync(user);
        //                 var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        //                 code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        //                 var callbackUrl = Url.Page(
        //                     "/Account/ConfirmEmail",
        //                     pageHandler: null,
        //                     values: new { area = "Identity", userId = userId, code = code },
        //                     protocol: Request.Scheme);

        //                 await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
        //                     $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

        //                 // If account confirmation is required, we need to show the link if we don't have a real email sender
        //                 if (_userManager.Options.SignIn.RequireConfirmedAccount)
        //                 {
        //                     return RedirectToPage("./RegisterConfirmation", new { Email = Input.Email });
        //                 }

        //                 await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
        //                 return LocalRedirect(returnUrl);
        //             }
        //         }
        //         foreach (var error in result.Errors)
        //         {
        //             ModelState.AddModelError(string.Empty, error.Description);
        //         }
        //     }

        //     ProviderDisplayName = info.ProviderDisplayName;
        //     ReturnUrl = returnUrl;
        //     return Page();
        // }

        // private IdentityUser CreateUser()
        // {
        //     try
        //     {
        //         return Activator.CreateInstance<IdentityUser>();
        //     }
        //     catch
        //     {
        //         throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
        //             $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
        //             $"override the external login page in /Areas/Identity/Pages/Account/ExternalLogin.cshtml");
        //     }
        // }

        // private IUserEmailStore<IdentityUser> GetEmailStore()
        // {
        //     if (!_userManager.SupportsUserEmail)
        //     {
        //         throw new NotSupportedException("The default UI requires a user store with email support.");
        //     }
        //     return (IUserEmailStore<IdentityUser>)_userStore;
        // }
    }
}
