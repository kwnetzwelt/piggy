using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Accounts;

public class CreateModel : PageModel
{
    private readonly IVirtualAccountRepository _accountRepo;
    private readonly IAccountGroupRepository _groupRepo;

    public CreateModel(IVirtualAccountRepository accountRepo, IAccountGroupRepository groupRepo)
    {
        _accountRepo = accountRepo;
        _groupRepo = groupRepo;
    }

    [BindProperty]
    public VirtualAccount Account { get; set; } = new();

    [BindProperty]
    public string? MatchStringsInput { get; set; }

    public List<AccountGroup> Groups { get; set; } = new();

    public async Task OnGetAsync()
    {
        Groups = await _groupRepo.GetAllAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Groups = await _groupRepo.GetAllAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        Account.MatchStrings = ParseMatchStrings(MatchStringsInput);
        await _accountRepo.CreateAsync(Account);
        return RedirectToPage("Index");
    }

    private static List<string>? ParseMatchStrings(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var strings = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return strings.Length > 0 ? strings.ToList() : null;
    }
}
