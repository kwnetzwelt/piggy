using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Transactions;

public class TagModel : PageModel
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IVirtualAccountRepository _accountRepo;
    private readonly ISplitTemplateRepository _splitRepo;
    private readonly ISettingsRepository _settingsRepo;

    public TagModel(ITransactionRepository transactionRepo, IVirtualAccountRepository accountRepo, ISplitTemplateRepository splitRepo, ISettingsRepository settingsRepo)
    {
        _transactionRepo = transactionRepo;
        _accountRepo = accountRepo;
        _splitRepo = splitRepo;
        _settingsRepo = settingsRepo;
    }

    public Transaction? CurrentTransaction { get; set; }
    public List<VirtualAccount> Accounts { get; set; } = new();
    public List<VirtualAccount> TopAccounts { get; set; } = new();
    public List<VirtualAccount> OtherAccounts { get; set; } = new();
    public List<SplitTemplate> SplitTemplates { get; set; } = new();
    public string? SuggestedAccountId { get; set; }
    public int CurrentIndex { get; set; }
    public int TotalCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Skipped { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? LastAction { get; set; }

    public async Task OnGetAsync()
    {
        Accounts = await _accountRepo.GetAllAsync();
        SplitTemplates = await _splitRepo.GetAllAsync();
        var untagged = await _transactionRepo.GetUntaggedAsync();

        if (!string.IsNullOrEmpty(Skipped))
        {
            var skippedIds = Skipped.Split(',');
            untagged = untagged.Where(t => !skippedIds.Contains(t.ExternalId)).ToList();
        }

        TotalCount = untagged.Count;

        if (untagged.Any())
        {
            CurrentTransaction = untagged[0];
            CurrentIndex = 1;
            await LoadSuggestion();
            RankAccounts();
        }
    }

    public async Task<IActionResult> OnPostPickAsync(string transactionId, string accountId)
    {
        var transaction = await _transactionRepo.GetByExternalIdAsync(transactionId);
        if (transaction != null)
        {
            transaction.VirtualAccountId = accountId;
            transaction.IsProcessed = true;
            await _transactionRepo.UpdateAsync(transaction);
        }
        return RedirectToPage(new { LastAction = $"tagged:{transactionId}" });
    }

    public async Task<IActionResult> OnPostSplitAsync(string transactionId, string templateId)
    {
        var transaction = await _transactionRepo.GetByExternalIdAsync(transactionId);
        var template = await _splitRepo.GetByIdAsync(templateId);

        if (transaction == null || template == null)
            return RedirectToPage();

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

        return RedirectToPage(new { LastAction = $"split:{transactionId}" });
    }

    public async Task<IActionResult> OnPostUndoAsync(string undoAction, string? skipped)
    {
        if (string.IsNullOrEmpty(undoAction))
            return RedirectToPage(new { Skipped = skipped });

        var parts = undoAction.Split(':');
        var action = parts[0];
        var externalId = parts.Length > 1 ? parts[1] : null;

        if (string.IsNullOrEmpty(externalId))
            return RedirectToPage(new { Skipped = skipped });

        if (action == "tagged")
        {
            var transaction = await _transactionRepo.GetByExternalIdAsync(externalId);
            if (transaction != null)
            {
                transaction.VirtualAccountId = null;
                transaction.IsProcessed = false;
                await _transactionRepo.UpdateAsync(transaction);
            }
        }
        else if (action == "split")
        {
            var allTransactions = await _transactionRepo.GetAllAsync();
            var splitChildren = allTransactions.Where(t => t.SplitFrom == externalId).ToList();
            foreach (var child in splitChildren)
            {
                await _transactionRepo.DeleteByExternalIdAsync(child.ExternalId);
            }

            var original = await _transactionRepo.GetByExternalIdAsync(externalId);
            if (original != null)
            {
                original.SplitFrom = null;
                original.SplitTemplateId = null;
                original.VirtualAccountId = null;
                original.IsProcessed = false;
                await _transactionRepo.UpdateAsync(original);
            }
        }

        return RedirectToPage(new { Skipped = skipped });
    }

    public IActionResult OnPostSkipAsync(string transactionId)
    {
        var currentSkipped = string.IsNullOrEmpty(Skipped) ? "" : Skipped + ",";
        var newSkipped = currentSkipped + transactionId;
        return RedirectToPage(new { Skipped = newSkipped });
    }

    private async Task LoadSuggestion()
    {
        if (CurrentTransaction == null) return;
        SuggestedAccountId = await _transactionRepo.GetSuggestedAccountIdAsync(CurrentTransaction.Description);
    }

    private void RankAccounts()
    {
        if (CurrentTransaction == null) return;

        var desc = CurrentTransaction.Description ?? "";
        var descLower = desc.ToLowerInvariant();

        var scored = Accounts.Select(a => new
        {
            Account = a,
            Score = ComputeScore(a, descLower)
        })
        .OrderByDescending(x => x.Score)
        .ThenBy(x => x.Account.Name)
        .ToList();

        TopAccounts = scored.Where(x => x.Score > 0).Take(5).Select(x => x.Account).ToList();

        if (TopAccounts.Count < 5)
        {
            var remaining = scored.Where(x => x.Score <= 0).Select(x => x.Account).ToList();
            TopAccounts.AddRange(remaining.Take(5 - TopAccounts.Count));
        }

        var topIds = TopAccounts.Select(a => a.Id).ToHashSet();
        OtherAccounts = scored.Where(x => !topIds.Contains(x.Account.Id)).Select(x => x.Account).ToList();
    }

    private int ComputeScore(VirtualAccount account, string descLower)
    {
        int score = 0;
        var nameLower = account.Name.ToLowerInvariant();

        if (SuggestedAccountId == account.Id)
            score += 100;

        if (account.MatchStrings != null)
        {
            foreach (var matchString in account.MatchStrings)
            {
                if (descLower.Contains(matchString.ToLowerInvariant()))
                {
                    score += 100;
                    break;
                }
            }
        }

        if (descLower.Contains(nameLower))
            score += 50;
        else if (nameLower.Contains(descLower))
            score += 40;

        var nameWords = nameLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var descWords = descLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var meaningfulNameWords = nameWords.Where(w => w.Length > 3).ToList();
        var meaningfulDescWords = descWords.Where(w => w.Length > 3).ToList();
        var commonWords = meaningfulNameWords.Intersect(meaningfulDescWords).ToList();

        score += commonWords.Count * 20;

        if (score == 0)
        {
            foreach (var nameWord in meaningfulNameWords)
            {
                foreach (var descWord in meaningfulDescWords)
                {
                    if (nameWord.Contains(descWord) || descWord.Contains(nameWord))
                    {
                        score += 15;
                        break;
                    }
                }
                if (score > 0) break;
            }
        }

        if (account.Color != "#6c757d")
            score += 5;

        return score;
    }
}
