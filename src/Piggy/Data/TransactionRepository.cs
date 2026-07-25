using MongoDB.Driver;
using Piggy.Models;

namespace Piggy.Data;

public class TransactionRepository : ITransactionRepository
{
    private readonly IMongoDbContext _context;
    private readonly IVirtualAccountRepository _accountRepo;

    public TransactionRepository(IMongoDbContext context, IVirtualAccountRepository accountRepo)
    {
        _context = context;
        _accountRepo = accountRepo;
    }

    public async Task<List<Transaction>> GetAllAsync()
    {
        return await _context.Transactions
            .Find(_ => true)
            .SortByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetByVirtualAccountIdAsync(string virtualAccountId)
    {
        return await _context.Transactions
            .Find(t => t.VirtualAccountId == virtualAccountId)
            .SortByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetUntaggedAsync()
    {
        return await _context.Transactions
            .Find(t => t.VirtualAccountId == null && t.SplitFrom == null)
            .SortByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<Transaction?> GetByExternalIdAsync(string externalId)
    {
        return await _context.Transactions
            .Find(t => t.ExternalId == externalId)
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(Transaction transaction)
    {
        transaction.ImportedAt = DateTime.UtcNow;
        await _context.Transactions.InsertOneAsync(transaction);
    }

    public async Task CreateManyAsync(List<Transaction> transactions)
    {
        await _context.Transactions.InsertManyAsync(transactions);
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        await _context.Transactions.ReplaceOneAsync(t => t.Id == transaction.Id, transaction);
    }

    public async Task<int> GetTotalBalanceForAccountAsync(string virtualAccountId)
    {
        var transactions = await _context.Transactions
            .Find(t => t.VirtualAccountId == virtualAccountId)
            .ToListAsync();
        return (int)transactions.Sum(t => t.Amount);
    }

    public async Task<bool> ExistsByExternalIdAsync(string externalId)
    {
        return await _context.Transactions.Find(t => t.ExternalId == externalId).AnyAsync();
    }

    public async Task<string?> GetSuggestedAccountIdAsync(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        var accounts = await _accountRepo.GetAllAsync();
        var descriptionLower = description.ToLowerInvariant();

        foreach (var account in accounts)
        {
            if (account.MatchStrings != null)
            {
                foreach (var matchString in account.MatchStrings)
                {
                    if (descriptionLower.Contains(matchString.ToLowerInvariant()))
                        return account.Id;
                }
            }
        }

        var tagged = await _context.Transactions
            .Find(t => t.VirtualAccountId != null && t.Description != null)
            .ToListAsync();

        var matches = tagged
            .Where(t => t.Description != null && IsSimilarDescription(descriptionLower, t.Description.ToLowerInvariant()))
            .GroupBy(t => t.VirtualAccountId)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        return matches?.Key;
    }

    private static bool IsSimilarDescription(string input, string existing)
    {
        if (input == existing)
            return true;

        if (existing.Contains(input) || input.Contains(existing))
            return true;

        var inputWords = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var existingWords = existing.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var meaningfulInputWords = inputWords.Where(w => w.Length > 3).ToList();
        var meaningfulExistingWords = existingWords.Where(w => w.Length > 3).ToList();

        if (!meaningfulInputWords.Any() || !meaningfulExistingWords.Any())
            return false;

        var commonWords = meaningfulInputWords.Intersect(meaningfulExistingWords).ToList();
        if (commonWords.Count >= 2 || (commonWords.Count == 1 && meaningfulInputWords.Count <= 3))
            return true;

        foreach (var inputWord in meaningfulInputWords)
        {
            foreach (var existingWord in meaningfulExistingWords)
            {
                if (inputWord.Contains(existingWord) || existingWord.Contains(inputWord))
                    return true;
            }
        }

        return false;
    }

    public async Task DeleteByExternalIdAsync(string externalId)
    {
        await _context.Transactions.DeleteOneAsync(t => t.ExternalId == externalId);
    }
}
