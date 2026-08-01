using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Groups;

public class IndexModel : PageModel
{
    private readonly IAccountGroupRepository _groupRepo;
    private readonly IVirtualAccountRepository _accountRepo;

    public IndexModel(IAccountGroupRepository groupRepo, IVirtualAccountRepository accountRepo)
    {
        _groupRepo = groupRepo;
        _accountRepo = accountRepo;
    }

    public List<GroupWithCount> Groups { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var count = await _accountRepo.CountByGroupAsync(id);
        if (count > 0)
        {
            ErrorMessage = "This group still has virtual accounts assigned. Reassign or unassign them before deleting.";
            await LoadAsync();
            return Page();
        }

        await _groupRepo.DeleteAsync(id);
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var groups = await _groupRepo.GetAllAsync();
        Groups = new List<GroupWithCount>();
        foreach (var group in groups)
        {
            Groups.Add(new GroupWithCount
            {
                Group = group,
                AccountCount = await _accountRepo.CountByGroupAsync(group.Id)
            });
        }
    }

    public class GroupWithCount
    {
        public AccountGroup Group { get; set; } = new();
        public long AccountCount { get; set; }
    }
}
