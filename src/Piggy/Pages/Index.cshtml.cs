using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages;

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
    public List<Transaction> RecentTransactions { get; set; } = new();
    public int TotalTransactions { get; set; }
    public int UntaggedCount { get; set; }
    public decimal TotalBalance { get; set; }
    public decimal NegativeBalance { get; set; }

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

        RecentTransactions = transactions.Take(10).ToList();
        TotalTransactions = transactions.Count;
        UntaggedCount = transactions.Count(t => t.VirtualAccountId == null && t.SplitFrom == null);
        TotalBalance = Accounts.Sum(a => a.CurrentBalance);
        NegativeBalance = Accounts.Where(a => a.CurrentBalance < 0).Sum(a => a.CurrentBalance);
    }

    public class AccountWithBalance
    {
        public VirtualAccount Account { get; set; } = new();
        public decimal CurrentBalance { get; set; }
    }
}
