using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Accounts;

public class IndexModel : PageModel
{
    private readonly IVirtualAccountRepository _accountRepo;
    private readonly ITransactionRepository _transactionRepo;

    public IndexModel(IVirtualAccountRepository accountRepo, ITransactionRepository transactionRepo)
    {
        _accountRepo = accountRepo;
        _transactionRepo = transactionRepo;
    }

    public List<AccountWithBalance> Accounts { get; set; } = new();

    public async Task OnGetAsync()
    {
        var accounts = await _accountRepo.GetAllAsync();
        var transactions = await _transactionRepo.GetAllAsync();

        foreach (var account in accounts)
        {
            var balance = transactions
                .Where(t => t.VirtualAccountId == account.Id)
                .Sum(t => t.Amount);

            Accounts.Add(new AccountWithBalance
            {
                Account = account,
                CurrentBalance = account.StartingBalance + (decimal)balance
            });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        await _accountRepo.DeleteAsync(id);
        return RedirectToPage();
    }

    public class AccountWithBalance
    {
        public VirtualAccount Account { get; set; } = new();
        public decimal CurrentBalance { get; set; }
    }
}
