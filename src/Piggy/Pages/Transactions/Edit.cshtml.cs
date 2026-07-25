using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Transactions;

public class EditModel : PageModel
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IVirtualAccountRepository _accountRepo;

    public EditModel(ITransactionRepository transactionRepo, IVirtualAccountRepository accountRepo)
    {
        _transactionRepo = transactionRepo;
        _accountRepo = accountRepo;
    }

    [BindProperty]
    public Transaction Transaction { get; set; } = new();

    [BindProperty]
    public string? VirtualAccountId { get; set; }

    public List<VirtualAccount> Accounts { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnMonth { get; set; }

    public async Task<IActionResult> OnGetAsync(string id, string? month)
    {
        var transaction = await _transactionRepo.GetByExternalIdAsync(id);
        if (transaction == null) return NotFound();

        Transaction = transaction;
        VirtualAccountId = transaction.VirtualAccountId;
        ReturnMonth = month;
        Accounts = await _accountRepo.GetAllAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        var existing = await _transactionRepo.GetByExternalIdAsync(id);
        if (existing == null) return NotFound();

        existing.Date = Transaction.Date;
        existing.Description = Transaction.Description;
        existing.Amount = Transaction.Amount;
        existing.Currency = Transaction.Currency;
        existing.VirtualAccountId = string.IsNullOrEmpty(VirtualAccountId) ? null : VirtualAccountId;
        existing.IsProcessed = !string.IsNullOrEmpty(VirtualAccountId);

        await _transactionRepo.UpdateAsync(existing);
        return RedirectToPage("Index", new { Month = ReturnMonth });
    }
}
