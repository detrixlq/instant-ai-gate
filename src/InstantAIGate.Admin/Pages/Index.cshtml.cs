using InstantAIGate.Admin.Config;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace InstantAIGate.Admin.Pages;

public class IndexModel(IOptions<APIClientOptions> apiOptions) : PageModel
{
    public string APIUrl { get; } = apiOptions.Value.PublicUrl;
    public void OnGet()
    {

    }
}
