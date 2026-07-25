using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Settings;

public class IndexModel : PageModel
{
    private readonly ISettingsRepository _settingsRepo;
    private readonly IVirtualAccountRepository _accountRepo;

    public IndexModel(ISettingsRepository settingsRepo, IVirtualAccountRepository accountRepo)
    {
        _settingsRepo = settingsRepo;
        _accountRepo = accountRepo;
    }

    public List<VirtualAccount> Accounts { get; set; } = new();
    public string? CurrentOverflowAccountId { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public string? OverflowAccountId { get; set; }

    public async Task OnGetAsync()
    {
        Accounts = await _accountRepo.GetAllAsync();
        var settings = await _settingsRepo.GetAsync();
        CurrentOverflowAccountId = settings.OverflowAccountId;
        OverflowAccountId = settings.OverflowAccountId;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var settings = await _settingsRepo.GetAsync();
        settings.OverflowAccountId = string.IsNullOrEmpty(OverflowAccountId) ? null : OverflowAccountId;
        await _settingsRepo.UpdateAsync(settings);

        SuccessMessage = "Settings saved.";
        return RedirectToPage();
    }
}
