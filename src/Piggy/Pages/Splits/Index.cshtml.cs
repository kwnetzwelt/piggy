using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Splits;

public class IndexModel : PageModel
{
    private readonly ISplitTemplateRepository _splitRepo;
    private readonly IVirtualAccountRepository _accountRepo;

    public IndexModel(ISplitTemplateRepository splitRepo, IVirtualAccountRepository accountRepo)
    {
        _splitRepo = splitRepo;
        _accountRepo = accountRepo;
    }

    public List<SplitTemplate> Templates { get; set; } = new();
    public Dictionary<string, VirtualAccount> AccountLookup { get; set; } = new();

    public async Task OnGetAsync()
    {
        Templates = await _splitRepo.GetAllAsync();
        var accounts = await _accountRepo.GetAllAsync();
        AccountLookup = accounts.ToDictionary(a => a.Id, a => a);
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        await _splitRepo.DeleteAsync(id);
        return RedirectToPage();
    }
}
