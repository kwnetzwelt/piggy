using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;

namespace Piggy.Pages.Data;

public class IndexModel : PageModel
{
    private readonly IMongoDbContext _context;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IMongoDbContext context, ILogger<IndexModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostExportAsync()
    {
        var json = await _context.ExportAsync();
        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", "piggy-export.json");
    }

    public async Task<IActionResult> OnPostImportAsync(IFormFile? importFile)
    {
        if (importFile == null || importFile.Length == 0)
        {
            ErrorMessage = "Please select a file to import.";
            return Page();
        }

        try
        {
            using var reader = new StreamReader(importFile.OpenReadStream());
            var json = await reader.ReadToEndAsync();
            await _context.ImportAsync(json);
            SuccessMessage = "Data imported successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import data");
            ErrorMessage = $"Import failed: {ex.Message}";
        }

        return Page();
    }
}
