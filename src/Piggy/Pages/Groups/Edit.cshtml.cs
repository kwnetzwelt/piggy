using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Groups;

public class EditModel : PageModel
{
    private readonly IAccountGroupRepository _groupRepo;

    public EditModel(IAccountGroupRepository groupRepo)
    {
        _groupRepo = groupRepo;
    }

    [BindProperty]
    public AccountGroup Group { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var group = await _groupRepo.GetByIdAsync(id);
        if (group == null)
        {
            return NotFound();
        }

        Group = group;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Group.Name))
        {
            ModelState.AddModelError("Group.Name", "Name is required");
            return Page();
        }

        await _groupRepo.UpdateAsync(Group);
        return RedirectToPage("Index");
    }
}
