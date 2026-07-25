# Split Overflow Logic Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** When splitting a transaction, if the split template items sum to more than the transaction amount, automatically create an overflow transaction assigned to a configurable overflow virtual account.

**Architecture:** Add a Settings model (singleton document in MongoDB) to store the overflow account. Add a Settings page to configure it. Modify both split handlers (Tag page and Transactions/Index page) to calculate overflow and create an additional overflow transaction. Display overflow info in transaction details. Ensure undo properly cleans up overflow transactions.

**Tech Stack:** ASP.NET Core 10.0 Razor Pages, MongoDB.Driver 2.28.0, Bootstrap 5.3.3

## Global Constraints

- .NET 10.0, MongoDB.Driver 2.28.0
- No authentication on the web app
- MongoDB 7 via Docker Compose
- Existing code patterns: repositories are Scoped services, models use `[BsonId]`/`[BsonElement]` attributes
- No new NuGet packages required

## File Structure

| File | Purpose |
|------|---------|
| `src/Piggy/Models/Settings.cs` | **Create** — Singleton settings model with OverflowAccountId |
| `src/Piggy/Data/ISettingsRepository.cs` | **Create** — Repository interface for settings |
| `src/Piggy/Data/SettingsRepository.cs` | **Create** — MongoDB implementation, ensures single document |
| `src/Piggy/Data/IMongoDbContext.cs` | **Modify** — Add Settings collection |
| `src/Piggy/Data/MongoDbContext.cs` | **Modify** — Add Settings collection property |
| `src/Piggy/Program.cs` | **Modify** — Register ISettingsRepository in DI |
| `src/Piggy/Pages/Settings/Index.cshtml` | **Create** — Settings page view |
| `src/Piggy/Pages/Settings/Index.cshtml.cs` | **Create** — Settings page code-behind |
| `src/Piggy/Pages/Shared/_Layout.cshtml` | **Modify** — Add Settings nav link |
| `src/Piggy/Pages/Transactions/Tag.cshtml.cs` | **Modify** — Add overflow logic to OnPostSplitAsync |
| `src/Piggy/Pages/Transactions/Index.cshtml.cs` | **Modify** — Add overflow logic to OnPostSplitAsync |
| `src/Piggy/Pages/Transactions/Tag.cshtml` | **Modify** — Show overflow info in undo bar |
| `src/Piggy/Pages/Transactions/Index.cshtml` | **Modify** — Show overflow info in split badge |
| `src/Piggy/Models/Transaction.cs` | **Modify** — Add IsOverflow field |
| `tests/Piggy.Tests/SettingsRepositoryTests.cs` | **Create** — Tests for settings repository |
| `tests/Piggy.Tests/SplitOverflowTests.cs` | **Create** — Tests for overflow logic |

---

### Task 1: Settings Model + Repository

**Files:**
- Create: `src/Piggy/Models/Settings.cs`
- Create: `src/Piggy/Data/ISettingsRepository.cs`
- Create: `src/Piggy/Data/SettingsRepository.cs`
- Modify: `src/Piggy/Data/IMongoDbContext.cs`
- Modify: `src/Piggy/Data/MongoDbContext.cs`
- Modify: `src/Piggy/Program.cs`
- Test: `tests/Piggy.Tests/SettingsRepositoryTests.cs`

**Interfaces:**
- Consumes: `IMongoDbContext` (existing)
- Produces: `ISettingsRepository` with `GetAsync()` and `UpdateAsync(Settings)` methods. Returns a single Settings document (singleton pattern — always one document in the `settings` collection).

- [ ] **Step 1: Create Settings model**

```csharp
// src/Piggy/Models/Settings.cs
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Piggy.Models;

public class Settings
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("overflowAccountId")]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? OverflowAccountId { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 2: Create ISettingsRepository interface**

```csharp
// src/Piggy/Data/ISettingsRepository.cs
using Piggy.Models;

namespace Piggy.Data;

public interface ISettingsRepository
{
    Task<Settings> GetAsync();
    Task UpdateAsync(Settings settings);
}
```

- [ ] **Step 3: Create SettingsRepository implementation**

```csharp
// src/Piggy/Data/SettingsRepository.cs
using MongoDB.Driver;
using Piggy.Models;

namespace Piggy.Data;

public class SettingsRepository : ISettingsRepository
{
    private readonly IMongoCollection<Settings> _settings;

    public SettingsRepository(IMongoDbContext context)
    {
        _settings = context.Settings;
    }

    public async Task<Settings> GetAsync()
    {
        var settings = await _settings.Find(_ => true).FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new Settings { Id = "singleton" };
            await _settings.InsertOneAsync(settings);
        }
        return settings;
    }

    public async Task UpdateAsync(Settings settings)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        await _settings.ReplaceOneAsync(
            s => s.Id == settings.Id,
            settings,
            new ReplaceOptions { IsUpsert = true });
    }
}
```

- [ ] **Step 4: Add Settings collection to IMongoDbContext**

```csharp
// src/Piggy/Data/IMongoDbContext.cs
using MongoDB.Driver;
using Piggy.Models;

namespace Piggy.Data;

public interface IMongoDbContext
{
    IMongoCollection<VirtualAccount> VirtualAccounts { get; }
    IMongoCollection<Transaction> Transactions { get; }
    IMongoCollection<SplitTemplate> SplitTemplates { get; }
    IMongoCollection<Settings> Settings { get; }
}
```

- [ ] **Step 5: Add Settings collection to MongoDbContext**

```csharp
// src/Piggy/Data/MongoDbContext.cs
// Add this property after the existing ones:
    public IMongoCollection<Settings> Settings =>
        _database.GetCollection<Settings>("settings");
```

- [ ] **Step 6: Register ISettingsRepository in Program.cs**

Add after line 22:
```csharp
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
```

- [ ] **Step 7: Write tests for SettingsRepository**

```csharp
// tests/Piggy.Tests/SettingsRepositoryTests.cs
using MongoDB.Driver;
using Moq;
using Piggy.Data;
using Piggy.Models;
using Xunit;

namespace Piggy.Tests;

public class SettingsRepositoryTests
{
    private readonly Mock<IMongoDbContext> _contextMock;
    private readonly Mock<IMongoCollection<Settings>> _collectionMock;
    private readonly SettingsRepository _repository;

    public SettingsRepositoryTests()
    {
        _contextMock = new Mock<IMongoDbContext>();
        _collectionMock = new Mock<IMongoCollection<Settings>>();
        _contextMock.Setup(c => c.Settings).Returns(_collectionMock.Object);
        _repository = new SettingsRepository(_contextMock.Object);
    }

    [Fact]
    public async Task GetAsync_ReturnsSettings()
    {
        var settings = new Settings { Id = "singleton", OverflowAccountId = "acc1" };
        var asyncCursor = new Mock<IAsyncCursor<Settings>>();
        asyncCursor.SetupSequence(c => c.MoveNextAsync(default))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        asyncCursor.Setup(c => c.Current).Returns(new[] { settings });

        _collectionMock.Setup(c => c.Find(It.IsAny<FilterDefinition<Settings>>(), null))
            .Returns(asyncCursor.Object);

        var result = await _repository.GetAsync();

        Assert.Equal("singleton", result.Id);
        Assert.Equal("acc1", result.OverflowAccountId);
    }

    [Fact]
    public async Task UpdateAsync_CallsReplaceOne()
    {
        var settings = new Settings { Id = "singleton", OverflowAccountId = "acc2" };

        await _repository.UpdateAsync(settings);

        _collectionMock.Verify(c => c.ReplaceOneAsync(
            It.IsAny<FilterDefinition<Settings>>(),
            settings,
            It.IsAny<ReplaceOptions>(),
            default), Times.Once);
    }
}
```

- [ ] **Step 8: Run tests**

Run: `dotnet test tests/Piggy.Tests/`
Expected: All tests pass

- [ ] **Step 9: Commit**

```bash
git add src/Piggy/Models/Settings.cs src/Piggy/Data/ISettingsRepository.cs src/Piggy/Data/SettingsRepository.cs src/Piggy/Data/IMongoDbContext.cs src/Piggy/Data/MongoDbContext.cs src/Piggy/Program.cs tests/Piggy.Tests/SettingsRepositoryTests.cs
git commit -m "feat: add Settings model and repository for overflow account configuration"
```

---

### Task 2: Settings Page

**Files:**
- Create: `src/Piggy/Pages/Settings/Index.cshtml`
- Create: `src/Piggy/Pages/Settings/Index.cshtml.cs`
- Modify: `src/Piggy/Pages/Shared/_Layout.cshtml`

**Interfaces:**
- Consumes: `ISettingsRepository`, `IVirtualAccountRepository` (existing)
- Produces: `/Settings` page that displays current overflow account and allows changing it via dropdown

- [ ] **Step 1: Create Settings page code-behind**

```csharp
// src/Piggy/Pages/Settings/Index.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Settings;

public class IndexModel : PageModel
{
    private readonly ISettingsRepository _settingsRepo;
    private readonly IVirtualAccountRepository _accountRepo;

    public IndexModel(ISettingsRepository settingsRepo, IVirtualAccountRepository accountRepo)
    {
        _settingsRepo = settingsRepo;
        _accountRepo = accountRepo;
    }

    public List<VirtualAccount> Accounts { get; set; } = new();
    public string? CurrentOverflowAccountId { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public string? OverflowAccountId { get; set; }

    public async Task OnGetAsync()
    {
        Accounts = await _accountRepo.GetAllAsync();
        var settings = await _settingsRepo.GetAsync();
        CurrentOverflowAccountId = settings.OverflowAccountId;
        OverflowAccountId = settings.OverflowAccountId;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var settings = await _settingsRepo.GetAsync();
        settings.OverflowAccountId = string.IsNullOrEmpty(OverflowAccountId) ? null : OverflowAccountId;
        await _settingsRepo.UpdateAsync(settings);

        SuccessMessage = "Settings saved.";
        return RedirectToPage();
    }
}
```

- [ ] **Step 2: Create Settings page view**

```html
<!-- src/Piggy/Pages/Settings/Index.cshtml -->
@page
@model Piggy.Pages.Settings.IndexModel
@{
    ViewData["Title"] = "Settings";
}

<h2 class="mb-4">Settings</h2>

@if (!string.IsNullOrEmpty(Model.SuccessMessage))
{
    <div class="alert alert-success">@Model.SuccessMessage</div>
}

<div class="card">
    <div class="card-body">
        <h5 class="card-title">Split Overflow Account</h5>
        <p class="text-muted">When a split template's total exceeds the transaction amount, the overflow difference is assigned to this account.</p>

        <form method="post">
            <div class="mb-3">
                <label for="OverflowAccountId" class="form-label">Overflow Account</label>
                <select asp-for="OverflowAccountId" class="form-select">
                    <option value="">-- None (no overflow handling) --</option>
                    @foreach (var account in Model.Accounts)
                    {
                        <option value="@account.Id" selected="@(account.Id == Model.OverflowAccountId)">
                            @account.Name (@account.StartingBalance.ToString("N2") EUR)
                        </option>
                    }
                </select>
            </div>
            <button type="submit" class="btn btn-primary">Save Settings</button>
        </form>
    </div>
</div>
```

- [ ] **Step 3: Add Settings nav link to Layout**

In `src/Piggy/Pages/Shared/_Layout.cshtml`, add a new nav item after the existing "Import" link:

```html
<li class="nav-item">
    <a class="nav-link" asp-page="/Settings/Index">Settings</a>
</li>
```

- [ ] **Step 4: Verify build**

Run: `dotnet build src/Piggy/`
Expected: Build succeeds

- [ ] **Step 5: Commit**

```bash
git add src/Piggy/Pages/Settings/Index.cshtml src/Piggy/Pages/Settings/Index.cshtml.cs src/Piggy/Pages/Shared/_Layout.cshtml
git commit -m "feat: add Settings page for overflow account configuration"
```

---

### Task 3: Add IsOverflow Field to Transaction Model

**Files:**
- Modify: `src/Piggy/Models/Transaction.cs`

**Interfaces:**
- Consumes: nothing new
- Produces: `IsOverflow` boolean field on Transaction model, used by Tasks 4 and 5

- [ ] **Step 1: Add IsOverflow field to Transaction model**

Add after line 42 (`IsProcessed`):

```csharp
    [BsonElement("isOverflow")]
    public bool IsOverflow { get; set; }
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Piggy/`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add src/Piggy/Models/Transaction.cs
git commit -m "feat: add IsOverflow field to Transaction model"
```

---

### Task 4: Add Overflow Logic to Split Handlers

**Files:**
- Modify: `src/Piggy/Pages/Transactions/Tag.cshtml.cs`
- Modify: `src/Piggy/Pages/Transactions/Index.cshtml.cs`
- Test: `tests/Piggy.Tests/SplitOverflowTests.cs`

**Interfaces:**
- Consumes: `ISettingsRepository` (from Task 1), `IVirtualAccountRepository` (existing)
- Produces: Modified `OnPostSplitAsync` in both Tag and Transactions/Index pages that creates overflow transaction when template total > transaction amount

- [ ] **Step 1: Add ISettingsRepository and IVirtualAccountRepository to TagModel constructor**

Modify the TagModel constructor to inject `ISettingsRepository` and `IVirtualAccountRepository`:

```csharp
public class TagModel : PageModel
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IVirtualAccountRepository _accountRepo;
    private readonly ISplitTemplateRepository _splitRepo;
    private readonly ISettingsRepository _settingsRepo;

    public TagModel(ITransactionRepository transactionRepo, IVirtualAccountRepository accountRepo,
        ISplitTemplateRepository splitRepo, ISettingsRepository settingsRepo)
    {
        _transactionRepo = transactionRepo;
        _accountRepo = accountRepo;
        _splitRepo = splitRepo;
        _settingsRepo = settingsRepo;
    }
```

- [ ] **Step 2: Modify OnPostSplitAsync in Tag.cshtml.cs to add overflow logic**

Replace the current `OnPostSplitAsync` method (lines 71-113) with:

```csharp
    public async Task<IActionResult> OnPostSplitAsync(string transactionId, string templateId)
    {
        var transaction = await _transactionRepo.GetByN26IdAsync(transactionId);
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
                N26Id = $"{transaction.N26Id}-split-{Guid.NewGuid().ToString("N")[..8]}",
                Amount = item.Amount,
                Currency = transaction.Currency,
                Date = transaction.Date,
                Description = item.Description ?? $"{transaction.Description} - {template.Name}",
                SpaceId = transaction.SpaceId,
                SpaceName = transaction.SpaceName,
                VirtualAccountId = item.VirtualAccountId,
                ImportedAt = DateTime.UtcNow,
                IsProcessed = true,
                SplitFrom = transaction.N26Id,
                SplitTemplateId = templateId
            };
            await _transactionRepo.CreateAsync(splitTx);
        }

        if (overflow > 0 && !string.IsNullOrEmpty(settings.OverflowAccountId))
        {
            var overflowTx = new Transaction
            {
                N26Id = $"{transaction.N26Id}-overflow-{Guid.NewGuid().ToString("N")[..8]}",
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
                SplitFrom = transaction.N26Id,
                SplitTemplateId = templateId
            };
            await _transactionRepo.CreateAsync(overflowTx);
        }

        transaction.SplitFrom = transaction.N26Id;
        transaction.SplitTemplateId = templateId;
        transaction.SplitItems = splitSnapshot;
        transaction.IsProcessed = true;
        await _transactionRepo.UpdateAsync(transaction);

        return RedirectToPage(new { LastAction = $"split:{transactionId}" });
    }
```

- [ ] **Step 3: Add ISettingsRepository to IndexModel constructor (Transactions/Index)**

Modify the IndexModel constructor:

```csharp
public class IndexModel : PageModel
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IVirtualAccountRepository _accountRepo;
    private readonly ISplitTemplateRepository _splitRepo;
    private readonly ISettingsRepository _settingsRepo;

    public IndexModel(ITransactionRepository transactionRepo, IVirtualAccountRepository accountRepo,
        ISplitTemplateRepository splitRepo, ISettingsRepository settingsRepo)
    {
        _transactionRepo = transactionRepo;
        _accountRepo = accountRepo;
        _splitRepo = splitRepo;
        _settingsRepo = settingsRepo;
    }
```

- [ ] **Step 4: Modify OnPostSplitAsync in Index.cshtml.cs to add overflow logic**

Replace the current `OnPostSplitAsync` method (lines 78-120) with the same overflow logic pattern, but returning `RedirectToPage(new { Filter, SpaceFilter })` instead:

```csharp
    public async Task<IActionResult> OnPostSplitAsync(string transactionId, string templateId)
    {
        var transaction = await _transactionRepo.GetByN26IdAsync(transactionId);
        var template = await _splitRepo.GetByIdAsync(templateId);

        if (transaction == null || template == null)
            return RedirectToPage(new { Filter, SpaceFilter });

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
                N26Id = $"{transaction.N26Id}-split-{Guid.NewGuid().ToString("N")[..8]}",
                Amount = item.Amount,
                Currency = transaction.Currency,
                Date = transaction.Date,
                Description = item.Description ?? $"{transaction.Description} - {template.Name}",
                SpaceId = transaction.SpaceId,
                SpaceName = transaction.SpaceName,
                VirtualAccountId = item.VirtualAccountId,
                ImportedAt = DateTime.UtcNow,
                IsProcessed = true,
                SplitFrom = transaction.N26Id,
                SplitTemplateId = templateId
            };
            await _transactionRepo.CreateAsync(splitTx);
        }

        if (overflow > 0 && !string.IsNullOrEmpty(settings.OverflowAccountId))
        {
            var overflowTx = new Transaction
            {
                N26Id = $"{transaction.N26Id}-overflow-{Guid.NewGuid().ToString("N")[..8]}",
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
                SplitFrom = transaction.N26Id,
                SplitTemplateId = templateId
            };
            await _transactionRepo.CreateAsync(overflowTx);
        }

        transaction.SplitFrom = transaction.N26Id;
        transaction.SplitTemplateId = templateId;
        transaction.SplitItems = splitSnapshot;
        transaction.IsProcessed = true;
        await _transactionRepo.UpdateAsync(transaction);

        return RedirectToPage(new { Filter, SpaceFilter });
    }
```

- [ ] **Step 5: Write overflow logic tests**

```csharp
// tests/Piggy.Tests/SplitOverflowTests.cs
using Moq;
using Piggy.Data;
using Piggy.Models;
using Piggy.Pages.Transactions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Piggy.Tests;

public class SplitOverflowTests
{
    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly Mock<IVirtualAccountRepository> _accountRepoMock;
    private readonly Mock<ISplitTemplateRepository> _splitRepoMock;
    private readonly Mock<ISettingsRepository> _settingsRepoMock;
    private readonly TagModel _tagModel;

    public SplitOverflowTests()
    {
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _accountRepoMock = new Mock<IVirtualAccountRepository>();
        _splitRepoMock = new Mock<ISplitTemplateRepository>();
        _settingsRepoMock = new Mock<ISettingsRepository>();
        _tagModel = new TagModel(
            _transactionRepoMock.Object,
            _accountRepoMock.Object,
            _splitRepoMock.Object,
            _settingsRepoMock.Object);
    }

    [Fact]
    public async Task Split_WithOverflow_CreatesOverflowTransaction()
    {
        var transaction = new Transaction
        {
            N26Id = "tx-1",
            Amount = 100m,
            Currency = "EUR",
            Date = DateTime.UtcNow,
            Description = "Test"
        };

        var template = new SplitTemplate
        {
            Id = "tpl-1",
            Name = "Test Split",
            Items = new List<SplitTemplateItem>
            {
                new() { VirtualAccountId = "acc-1", Amount = 60m, Description = "Item 1" },
                new() { VirtualAccountId = "acc-2", Amount = 60m, Description = "Item 2" }
            }
        };

        var settings = new Settings { OverflowAccountId = "overflow-acc" };

        _transactionRepoMock.Setup(r => r.GetByN26IdAsync("tx-1"))
            .ReturnsAsync(transaction);
        _splitRepoMock.Setup(r => r.GetByIdAsync("tpl-1"))
            .ReturnsAsync(template);
        _settingsRepoMock.Setup(r => r.GetAsync())
            .ReturnsAsync(settings);

        var result = await _tagModel.OnPostSplitAsync("tx-1", "tpl-1");

        Assert.IsType<RedirectToPageResult>(result);

        // Should create 2 split children + 1 overflow = 3 transactions total
        _transactionRepoMock.Verify(r => r.CreateAsync(
            It.Is<Transaction>(t => t.IsOverflow && t.Amount == 20m)),
            Times.Once);
    }

    [Fact]
    public async Task Split_WithoutOverflow_DoesNotCreateOverflowTransaction()
    {
        var transaction = new Transaction
        {
            N26Id = "tx-2",
            Amount = 100m,
            Currency = "EUR",
            Date = DateTime.UtcNow,
            Description = "Test"
        };

        var template = new SplitTemplate
        {
            Id = "tpl-2",
            Name = "Test Split",
            Items = new List<SplitTemplateItem>
            {
                new() { VirtualAccountId = "acc-1", Amount = 50m, Description = "Item 1" },
                new() { VirtualAccountId = "acc-2", Amount = 50m, Description = "Item 2" }
            }
        };

        var settings = new Settings { OverflowAccountId = "overflow-acc" };

        _transactionRepoMock.Setup(r => r.GetByN26IdAsync("tx-2"))
            .ReturnsAsync(transaction);
        _splitRepoMock.Setup(r => r.GetByIdAsync("tpl-2"))
            .ReturnsAsync(template);
        _settingsRepoMock.Setup(r => r.GetAsync())
            .ReturnsAsync(settings);

        var result = await _tagModel.OnPostSplitAsync("tx-2", "tpl-2");

        Assert.IsType<RedirectToPageResult>(result);

        // Should create only 2 split children, no overflow
        _transactionRepoMock.Verify(r => r.CreateAsync(
            It.Is<Transaction>(t => t.IsOverflow)),
            Times.Never);
    }

    [Fact]
    public async Task Split_OverflowWithoutAccount_DoesNotCreateOverflowTransaction()
    {
        var transaction = new Transaction
        {
            N26Id = "tx-3",
            Amount = 100m,
            Currency = "EUR",
            Date = DateTime.UtcNow,
            Description = "Test"
        };

        var template = new SplitTemplate
        {
            Id = "tpl-3",
            Name = "Test Split",
            Items = new List<SplitTemplateItem>
            {
                new() { VirtualAccountId = "acc-1", Amount = 80m, Description = "Item 1" },
                new() { VirtualAccountId = "acc-2", Amount = 80m, Description = "Item 2" }
            }
        };

        var settings = new Settings { OverflowAccountId = null };

        _transactionRepoMock.Setup(r => r.GetByN26IdAsync("tx-3"))
            .ReturnsAsync(transaction);
        _splitRepoMock.Setup(r => r.GetByIdAsync("tpl-3"))
            .ReturnsAsync(template);
        _settingsRepoMock.Setup(r => r.GetAsync())
            .ReturnsAsync(settings);

        var result = await _tagModel.OnPostSplitAsync("tx-3", "tpl-3");

        Assert.IsType<RedirectToPageResult>(result);

        // Should create only 2 split children, no overflow (no account configured)
        _transactionRepoMock.Verify(r => r.CreateAsync(
            It.Is<Transaction>(t => t.IsOverflow)),
            Times.Never);
    }
}
```

- [ ] **Step 6: Run tests**

Run: `dotnet test tests/Piggy.Tests/`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add src/Piggy/Pages/Transactions/Tag.cshtml.cs src/Piggy/Pages/Transactions/Index.cshtml.cs tests/Piggy.Tests/SplitOverflowTests.cs
git commit -m "feat: add overflow logic to split handlers"
```

---

### Task 5: Display Overflow Info in Transaction Details

**Files:**
- Modify: `src/Piggy/Pages/Transactions/Tag.cshtml`
- Modify: `src/Piggy/Pages/Transactions/Index.cshtml`

**Interfaces:**
- Consumes: `Transaction.IsOverflow` (from Task 3)
- Produces: Visual indicators for overflow transactions in both Tag page undo bar and Transactions list

- [ ] **Step 1: Show overflow info in Tag page undo bar**

In `src/Piggy/Pages/Transactions/Tag.cshtml`, modify the undo alert (lines 13-26) to show overflow info when the last action was a split:

Replace lines 13-26 with:

```html
            <div class="alert alert-success d-flex align-items-center justify-content-between mb-3" role="alert">
                <span>
                    <i class="bi bi-check-circle me-2"></i>
                    @if (isSplit) { <text>Transaction split</text> } else { <text>Transaction tagged</text> }
                </span>
                <form method="post" asp-page-handler="Undo" class="d-inline">
                    <input type="hidden" name="undoAction" value="@Model.LastAction" />
                    <input type="hidden" name="skipped" value="@Model.Skipped" />
                    <button type="submit" class="btn btn-sm btn-outline-danger">
                        <i class="bi bi-arrow-counterclockwise"></i> Undo
                    </button>
                </form>
            </div>
```

This stays the same — the undo logic already handles overflow cleanup (deletes all children where SplitFrom matches). No UI change needed for overflow in the undo bar itself, since the undo handler already cleans up all children including overflow transactions.

- [ ] **Step 2: Show overflow badge in Transactions list**

In `src/Piggy/Pages/Transactions/Index.cshtml`, modify the split badge display (around line 50-53) to also show overflow:

Replace lines 50-53 with:

```html
                        @if (isSplit)
                        {
                            <span class="badge bg-secondary ms-2">split</span>
                        }
                        @if (tx.IsOverflow)
                        {
                            <span class="badge bg-info ms-2">overflow</span>
                        }
```

- [ ] **Step 3: Show overflow amount in Tag page transaction card**

In `src/Piggy/Pages/Transactions/Tag.cshtml`, add overflow display after the Amount row (after line 62). This shows when the user is viewing a transaction that was split with overflow:

After the Amount row in the card, add:

```html
                    @if (Model.CurrentTransaction.SplitFrom != null && Model.CurrentTransaction.IsOverflow)
                    {
                        <div class="row mb-3">
                            <div class="col-sm-3 text-muted">Type</div>
                            <div class="col-sm-9">
                                <span class="badge bg-info">Overflow</span>
                                <span class="text-muted ms-2">from split of @Model.CurrentTransaction.SplitFrom</span>
                            </div>
                        </div>
                    }
```

- [ ] **Step 4: Verify build**

Run: `dotnet build src/Piggy/`
Expected: Build succeeds

- [ ] **Step 5: Commit**

```bash
git add src/Piggy/Pages/Transactions/Tag.cshtml src/Piggy/Pages/Transactions/Index.cshtml
git commit -m "feat: display overflow info in transaction details"
```

---

### Task 6: Verify Undo Handles Overflow Correctly

**Files:**
- Review only: `src/Piggy/Pages/Transactions/Tag.cshtml.cs` (OnPostUndoAsync)

**Interfaces:**
- Consumes: existing undo logic
- Produces: confirmation that undo properly deletes overflow transactions

- [ ] **Step 1: Verify existing undo logic handles overflow**

Read `src/Piggy/Pages/Transactions/Tag.cshtml.cs` lines 115-158 (OnPostUndoAsync).

The current undo logic for split actions:
```csharp
var allTransactions = await _transactionRepo.GetAllAsync();
var splitChildren = allTransactions.Where(t => t.SplitFrom == n26Id).ToList();
foreach (var child in splitChildren)
{
    await _transactionRepo.DeleteByN26IdAsync(child.N26Id);
}
```

This already works for overflow because:
1. Overflow transactions have `SplitFrom = transaction.N26Id` (same as regular split children)
2. The undo finds ALL transactions where `SplitFrom == n26Id`, including overflow transactions
3. It deletes them all with `DeleteByN26IdAsync`

**No code changes needed.** The existing undo logic is already correct for overflow.

- [ ] **Step 2: Add a test to verify undo deletes overflow**

Add to `tests/Piggy.Tests/SplitOverflowTests.cs`:

```csharp
    [Fact]
    public async Task UndoSplit_DeletesOverflowTransaction()
    {
        var original = new Transaction
        {
            N26Id = "tx-undo-1",
            Amount = 100m,
            SplitFrom = "tx-undo-1",
            SplitTemplateId = "tpl-1",
            IsProcessed = true
        };

        var child1 = new Transaction
        {
            N26Id = "tx-undo-1-split-aaa",
            SplitFrom = "tx-undo-1",
            Amount = 60m,
            VirtualAccountId = "acc-1"
        };

        var overflowChild = new Transaction
        {
            N26Id = "tx-undo-1-overflow-bbb",
            SplitFrom = "tx-undo-1",
            Amount = 40m,
            VirtualAccountId = "overflow-acc",
            IsOverflow = true
        };

        _transactionRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Transaction> { original, child1, overflowChild });
        _transactionRepoMock.Setup(r => r.GetByN26IdAsync("tx-undo-1"))
            .ReturnsAsync(original);

        var result = await _tagModel.OnPostUndoAsync("split:tx-undo-1", null);

        Assert.IsType<RedirectToPageResult>(result);

        // Verify both regular child and overflow child are deleted
        _transactionRepoMock.Verify(r => r.DeleteByN26IdAsync("tx-undo-1-split-aaa"), Times.Once);
        _transactionRepoMock.Verify(r => r.DeleteByN26IdAsync("tx-undo-1-overflow-bbb"), Times.Once);
    }
```

- [ ] **Step 3: Run tests**

Run: `dotnet test tests/Piggy.Tests/`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add tests/Piggy.Tests/SplitOverflowTests.cs
git commit -m "test: verify undo properly handles overflow transactions"
```

---

### Task 7: End-to-End Verification

**Files:**
- None (verification only)

**Interfaces:**
- Consumes: All tasks 1-6
- Produces: Confirmation that the feature works end-to-end

- [ ] **Step 1: Build the project**

Run: `dotnet build src/Piggy/`
Expected: Build succeeds with no warnings

- [ ] **Step 2: Run all tests**

Run: `dotnet test tests/Piggy.Tests/`
Expected: All tests pass

- [ ] **Step 3: Start Docker Compose**

Run: `docker-compose up -d` from project root
Expected: Both piggy and mongo services start successfully

- [ ] **Step 4: Manual verification checklist**

1. Navigate to Settings page — verify overflow account dropdown appears
2. Select an overflow account and save — verify success message
3. Go to Transactions/Tag, split a transaction where template total > transaction amount
4. Verify overflow transaction is created with correct amount and assigned to overflow account
5. Click Undo — verify overflow transaction is deleted
6. Check Transactions list — verify overflow badge shows on overflow transactions
