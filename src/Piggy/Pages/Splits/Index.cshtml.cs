using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Splits;

public class IndexModel : PageModel
{
    private readonly ISplitTemplateRepository _splitRepo;
    private readonly IVirtualAccountRepository _accountRepo;
    private readonly ISettingsRepository _settingsRepo;

    public IndexModel(ISplitTemplateRepository splitRepo, IVirtualAccountRepository accountRepo, ISettingsRepository settingsRepo)
    {
        _splitRepo = splitRepo;
        _accountRepo = accountRepo;
        _settingsRepo = settingsRepo;
    }

    public List<SplitTemplate> Templates { get; set; } = new();
    public Dictionary<string, VirtualAccount> AccountLookup { get; set; } = new();
    public List<VirtualAccount> Accounts { get; set; } = new();
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public string? OverflowAccountId { get; set; }

    public async Task OnGetAsync()
    {
        Templates = await _splitRepo.GetAllAsync();
        var accounts = await _accountRepo.GetAllAsync();
        AccountLookup = accounts.ToDictionary(a => a.Id, a => a);
        Accounts = accounts;
        var settings = await _settingsRepo.GetAsync();
        OverflowAccountId = settings.OverflowAccountId;
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        await _splitRepo.DeleteAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostOverflowAsync()
    {
        var settings = await _settingsRepo.GetAsync();
        settings.OverflowAccountId = string.IsNullOrEmpty(OverflowAccountId) ? null : OverflowAccountId;
        await _settingsRepo.UpdateAsync(settings);

        SuccessMessage = "Overflow account saved.";
        return RedirectToPage();
    }
}
