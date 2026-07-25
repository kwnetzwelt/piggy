using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Splits;

public class CreateModel : PageModel
{
    private readonly ISplitTemplateRepository _splitRepo;
    private readonly IVirtualAccountRepository _accountRepo;

    public CreateModel(ISplitTemplateRepository splitRepo, IVirtualAccountRepository accountRepo)
    {
        _splitRepo = splitRepo;
        _accountRepo = accountRepo;
    }

    [BindProperty]
    public SplitTemplate Template { get; set; } = new();

    public List<VirtualAccount> Accounts { get; set; } = new();

    public async Task OnGetAsync()
    {
        Accounts = await _accountRepo.GetAllAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Accounts = await _accountRepo.GetAllAsync();

        Template.Items = Template.Items
            .Where(i => !string.IsNullOrEmpty(i.VirtualAccountId))
            .ToList();

        if (string.IsNullOrWhiteSpace(Template.Name))
        {
            ModelState.AddModelError("Template.Name", "Name is required");
            return Page();
        }

        if (!Template.Items.Any())
        {
            ModelState.AddModelError("Template.Items", "Add at least one item");
            return Page();
        }

        await _splitRepo.CreateAsync(Template);
        return RedirectToPage("Index");
    }
}
