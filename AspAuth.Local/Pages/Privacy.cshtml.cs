using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspAuth.Local.Pages
{
    public class PrivacyModel : PageModel
    {
        private readonly ILogger<PrivacyModel> _logger;

        public PrivacyModel(ILogger<PrivacyModel> logger)
        {
            _logger = logger;
            Claims = [];
        }

        public Dictionary<string,string> Claims {get;set;}
        public void OnGet()
        {
            var allClaims = User.Claims.ToList();
            foreach (var claim in allClaims)
            {
                Claims.Add(claim.Type, claim.Value);
            }
        }
    }

}
