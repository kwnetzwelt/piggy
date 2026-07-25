using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Transactions;

public class CreateModel : PageModel
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IVirtualAccountRepository _accountRepo;

    public CreateModel(ITransactionRepository transactionRepo, IVirtualAccountRepository accountRepo)
    {
        _transactionRepo = transactionRepo;
        _accountRepo = accountRepo;
    }

    [BindProperty]
    public Transaction Transaction { get; set; } = new() { Date = DateTime.Today };

    [BindProperty]
    public string? VirtualAccountId { get; set; }

    public List<VirtualAccount> Accounts { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnMonth { get; set; }

    public async Task OnGetAsync(string? month)
    {
        ReturnMonth = month;
        Accounts = await _accountRepo.GetAllAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Accounts = await _accountRepo.GetAllAsync();

        Transaction.ExternalId = $"manual-{Guid.NewGuid():N}";
        Transaction.Currency = string.IsNullOrEmpty(Transaction.Currency) ? "EUR" : Transaction.Currency;
        Transaction.VirtualAccountId = string.IsNullOrEmpty(VirtualAccountId) ? null : VirtualAccountId;
        Transaction.IsProcessed = !string.IsNullOrEmpty(VirtualAccountId);

        await _transactionRepo.CreateAsync(Transaction);
        return RedirectToPage("Index", new { Month = ReturnMonth });
    }
}
