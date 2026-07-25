using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Import;

public class IndexModel : PageModel
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IVirtualAccountRepository _accountRepo;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ITransactionRepository transactionRepo, IVirtualAccountRepository accountRepo, ILogger<IndexModel> logger)
    {
        _transactionRepo = transactionRepo;
        _accountRepo = accountRepo;
        _logger = logger;
    }

    public List<VirtualAccount> Accounts { get; set; } = new();
    public ImportResult? Result { get; set; }

    public async Task OnGetAsync()
    {
        Accounts = await _accountRepo.GetAllAsync();
    }

    public async Task<IActionResult> OnPostAsync(IFormFile? csvFile, string? virtualAccountId)
    {
        Accounts = await _accountRepo.GetAllAsync();

        if (csvFile == null || csvFile.Length == 0)
        {
            Result = new ImportResult { Success = false, Message = "Please select a CSV file." };
            return Page();
        }

        try
        {
            using var reader = new StreamReader(csvFile.OpenReadStream());
            var content = await reader.ReadToEndAsync();
            var transactions = ParseCsv(content);

            var imported = 0;
            var skipped = 0;

            foreach (var transaction in transactions)
            {
                if (!string.IsNullOrEmpty(virtualAccountId))
                    transaction.VirtualAccountId = virtualAccountId;

                var exists = await _transactionRepo.ExistsByExternalIdAsync(transaction.ExternalId);
                if (!exists)
                {
                    await _transactionRepo.CreateAsync(transaction);
                    imported++;
                }
                else
                {
                    skipped++;
                }
            }

            Result = new ImportResult
            {
                Success = true,
                Message = $"Imported {imported} transactions, skipped {skipped} duplicates",
                Imported = imported,
                Skipped = skipped
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse CSV file");
            Result = new ImportResult { Success = false, Message = $"Failed to parse CSV: {ex.Message}" };
        }

        return Page();
    }

    private List<Transaction> ParseCsv(string csv)
    {
        var result = new List<Transaction>();
        var lines = SplitCsvLines(csv);

        if (lines.Count < 2)
            return result;

        var headers = ParseCsvLine(lines[0]);
        var dateIdx = Array.FindIndex(headers, h => h.Contains("Booking Date") || h.Contains("Datum"));
        var partnerIdx = Array.FindIndex(headers, h => h.Contains("Partner Name") || h.Contains("Beschreibung"));
        var typeIdx = Array.FindIndex(headers, h => h.Equals("Type", StringComparison.OrdinalIgnoreCase));
        var referenceIdx = Array.FindIndex(headers, h => h.Contains("Payment Reference") || h.Contains("Verwendungszweck"));
        var amountIdx = Array.FindIndex(headers, h => h.Contains("Amount"));
        var currencyIdx = Array.FindIndex(headers, h => h.Contains("Currency") || h.Contains("Währung"));

        if (dateIdx < 0 || amountIdx < 0)
        {
            _logger.LogWarning("Could not find required columns. Headers: {Headers}", string.Join(", ", headers));
            return result;
        }

        for (var i = 1; i < lines.Count; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            var fields = ParseCsvLine(line);
            if (fields.Length <= Math.Max(dateIdx, amountIdx))
                continue;

            try
            {
                var dateStr = fields[dateIdx].Trim();
                var amountStr = fields[amountIdx].Trim();
                var partner = partnerIdx >= 0 && partnerIdx < fields.Length ? fields[partnerIdx].Trim() : "";
                var type = typeIdx >= 0 && typeIdx < fields.Length ? fields[typeIdx].Trim() : "";
                var reference = referenceIdx >= 0 && referenceIdx < fields.Length ? fields[referenceIdx].Trim() : "";
                var currency = currencyIdx >= 0 && currencyIdx < fields.Length ? fields[currencyIdx].Trim() : "EUR";

                if (string.IsNullOrEmpty(dateStr) || string.IsNullOrEmpty(amountStr))
                    continue;

                if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    if (!DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    {
                        _logger.LogWarning("Could not parse date: {Date}", dateStr);
                        continue;
                    }
                }

                amountStr = amountStr.Replace(",", ".");
                if (!decimal.TryParse(amountStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                {
                    _logger.LogWarning("Could not parse amount: {Amount}", amountStr);
                    continue;
                }

                var description = !string.IsNullOrEmpty(partner) ? partner : type;
                if (!string.IsNullOrEmpty(reference))
                    description = $"{description} {reference}".Trim();

                var externalId = $"csv-{date:yyyyMMdd}-{amount}-{description.GetHashCode():X8}";

                result.Add(new Transaction
                {
                    ExternalId = externalId,
                    Date = date,
                    Description = description,
                    Amount = amount,
                    Currency = currency
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to parse line {Line}: {Error}", i, ex.Message);
            }
        }

        return result;
    }

    private List<string> SplitCsvLines(string csv)
    {
        var lines = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var c in csv)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                current.Append(c);
            }
            else if (c == '\n' && !inQuotes)
            {
                var line = current.ToString().TrimEnd('\r');
                if (!string.IsNullOrWhiteSpace(line))
                    lines.Add(line);
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        var lastLine = current.ToString().TrimEnd('\r');
        if (!string.IsNullOrWhiteSpace(lastLine))
            lines.Add(lastLine);

        return lines;
    }

    private string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }
}

public class ImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Imported { get; set; }
    public int Skipped { get; set; }
}
