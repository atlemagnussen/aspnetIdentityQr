using Id.Server.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Id.Server.Pages
{
    public class PrivacyModel : PageModel
    {
        //private readonly CryptoKeyService _keyService;

        public PrivacyModel()
        {
            Claims = [];
        }

        public Dictionary<string,string> Claims {get;set;}
        public async Task OnGetAsync()
        {
            var allClaims = User.Claims.ToList();
            foreach (var claim in allClaims)
            {
                Claims.Add(claim.Type, claim.Value);
            }
            //_keyService.CreateAndSaveNewKey();
            //await _seeder.SeedData();
        }
    }

}
