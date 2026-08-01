using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Groups;

public class CreateModel : PageModel
{
    private readonly IAccountGroupRepository _groupRepo;

    public CreateModel(IAccountGroupRepository groupRepo)
    {
        _groupRepo = groupRepo;
    }

    [BindProperty]
    public AccountGroup Group { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Group.Name))
        {
            ModelState.AddModelError("Group.Name", "Name is required");
            return Page();
        }

        await _groupRepo.CreateAsync(Group);
        return RedirectToPage("Index");
    }
}
