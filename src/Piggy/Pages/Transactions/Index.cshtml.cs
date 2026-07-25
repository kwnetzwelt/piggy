using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Transactions;

public class IndexModel : PageModel
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IVirtualAccountRepository _accountRepo;
    private readonly ISplitTemplateRepository _splitRepo;
    private readonly ISettingsRepository _settingsRepo;

    private const int PageSize = 50;

    public IndexModel(ITransactionRepository transactionRepo, IVirtualAccountRepository accountRepo, ISplitTemplateRepository splitRepo, ISettingsRepository settingsRepo)
    {
        _transactionRepo = transactionRepo;
        _accountRepo = accountRepo;
        _splitRepo = splitRepo;
        _settingsRepo = settingsRepo;
    }

    public List<Transaction> Transactions { get; set; } = new();
    public List<VirtualAccount> Accounts { get; set; } = new();
    public List<SplitTemplate> SplitTemplates { get; set; } = new();
    public Dictionary<string, string> Suggestions { get; set; } = new();

    public List<MonthGroup> MonthGroups { get; set; } = new();
    public List<string> AvailableMonths { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SpaceFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Month { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task OnGetAsync()
    {
        Accounts = await _accountRepo.GetAllAsync();
        SplitTemplates = await _splitRepo.GetAllAsync();
        var allTransactions = await _transactionRepo.GetAllAsync();

        AvailableMonths = allTransactions
            .Select(t => t.Date.ToString("yyyy-MM"))
            .Distinct()
            .OrderByDescending(m => m)
            .ToList();

        if (Filter == "untagged")
            allTransactions = allTransactions.Where(t => t.VirtualAccountId == null).ToList();
        else if (Filter == "tagged")
            allTransactions = allTransactions.Where(t => t.VirtualAccountId != null).ToList();

        if (!string.IsNullOrEmpty(SpaceFilter))
            allTransactions = allTransactions.Where(t => t.SpaceId == SpaceFilter).ToList();

        if (!string.IsNullOrEmpty(Month))
            allTransactions = allTransactions.Where(t => t.Date.ToString("yyyy-MM") == Month).ToList();

        allTransactions = allTransactions.OrderByDescending(t => t.Date).ToList();

        TotalCount = allTransactions.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
        CurrentPage = Math.Clamp(PageNumber, 1, TotalPages);

        Transactions = allTransactions
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        var childrenByParent = Transactions
            .Where(t => t.SplitFrom != null && t.SplitFrom != t.ExternalId)
            .GroupBy(t => t.SplitFrom)
            .ToDictionary(g => g.Key, g => g.ToList());

        var childIds = childrenByParent.Values.SelectMany(c => c).Select(t => t.ExternalId).ToHashSet();

        var ordered = new List<Transaction>();
        foreach (var tx in Transactions)
        {
            if (childIds.Contains(tx.ExternalId))
                continue;

            ordered.Add(tx);
            if (childrenByParent.TryGetValue(tx.ExternalId, out var children))
                ordered.AddRange(children);
        }

        Transactions = ordered;

        MonthGroups = Transactions
            .GroupBy(t => t.Date.ToString("MMMM yyyy"))
            .Select(g => new MonthGroup
            {
                Month = g.Key,
                Transactions = g.ToList()
            })
            .ToList();

        if (Filter == "untagged")
        {
            foreach (var tx in Transactions.Where(t => t.VirtualAccountId == null))
            {
                var suggested = await _transactionRepo.GetSuggestedAccountIdAsync(tx.Description);
                if (suggested != null)
                    Suggestions[tx.ExternalId] = suggested;
            }
        }
    }

    public async Task<IActionResult> OnPostTagAsync(string transactionId, string? virtualAccountId)
    {
        var transaction = await _transactionRepo.GetByExternalIdAsync(transactionId);
        if (transaction != null)
        {
            transaction.VirtualAccountId = string.IsNullOrEmpty(virtualAccountId) ? null : virtualAccountId;
            transaction.IsProcessed = !string.IsNullOrEmpty(virtualAccountId);
            await _transactionRepo.UpdateAsync(transaction);
        }

        return RedirectToPage(new { Filter, SpaceFilter, Month, PageNumber });
    }

    public async Task<IActionResult> OnPostSplitAsync(string transactionId, string templateId)
    {
        var transaction = await _transactionRepo.GetByExternalIdAsync(transactionId);
        var template = await _splitRepo.GetByIdAsync(templateId);

        if (transaction == null || template == null)
            return RedirectToPage(new { Filter, SpaceFilter, Month, PageNumber });

        var settings = await _settingsRepo.GetAsync();
        var templateTotal = template.Items.Sum(i => i.Amount);
        var overflow = templateTotal - transaction.Amount;

        var splitSnapshot = template.Items.Select(i => new SplitTemplateItem
        {
            VirtualAccountId = i.VirtualAccountId,
            Amount = i.Amount,
            Description = i.Description
        }).ToList();

        foreach (var item in template.Items)
        {
            var splitTx = new Transaction
            {
                ExternalId = $"{transaction.ExternalId}-split-{Guid.NewGuid().ToString("N")[..8]}",
                Amount = item.Amount,
                Currency = transaction.Currency,
                Date = transaction.Date,
                Description = item.Description ?? $"{transaction.Description} - {template.Name}",
                SpaceId = transaction.SpaceId,
                SpaceName = transaction.SpaceName,
                VirtualAccountId = item.VirtualAccountId,
                ImportedAt = DateTime.UtcNow,
                IsProcessed = true,
                SplitFrom = transaction.ExternalId,
                SplitTemplateId = templateId
            };
            await _transactionRepo.CreateAsync(splitTx);
        }

        if (overflow > 0 && !string.IsNullOrEmpty(settings.OverflowAccountId))
        {
            var overflowTx = new Transaction
            {
                ExternalId = $"{transaction.ExternalId}-overflow-{Guid.NewGuid().ToString("N")[..8]}",
                Amount = overflow,
                Currency = transaction.Currency,
                Date = transaction.Date,
                Description = $"Overflow: {transaction.Description} - {template.Name}",
                SpaceId = transaction.SpaceId,
                SpaceName = transaction.SpaceName,
                VirtualAccountId = settings.OverflowAccountId,
                ImportedAt = DateTime.UtcNow,
                IsProcessed = true,
                IsOverflow = true,
                SplitFrom = transaction.ExternalId,
                SplitTemplateId = templateId
            };
            await _transactionRepo.CreateAsync(overflowTx);
        }

        transaction.SplitFrom = transaction.ExternalId;
        transaction.SplitTemplateId = templateId;
        transaction.SplitItems = splitSnapshot;
        transaction.IsProcessed = true;
        await _transactionRepo.UpdateAsync(transaction);

        return RedirectToPage(new { Filter, SpaceFilter, Month, PageNumber });
    }

    public async Task<IActionResult> OnPostDeleteAsync(string transactionId)
    {
        await _transactionRepo.DeleteByExternalIdAsync(transactionId);
        return RedirectToPage(new { Filter, SpaceFilter, Month, PageNumber });
    }

    public async Task<IActionResult> OnPostBulkDeleteAsync(string transactionIds)
    {
        if (!string.IsNullOrEmpty(transactionIds))
        {
            var ids = transactionIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var id in ids)
            {
                await _transactionRepo.DeleteByExternalIdAsync(id);
            }
        }
        return RedirectToPage(new { Filter, SpaceFilter, Month, PageNumber });
    }
}

public class MonthGroup
{
    public string Month { get; set; } = string.Empty;
    public List<Transaction> Transactions { get; set; } = new();
}
