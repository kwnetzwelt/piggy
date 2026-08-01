# Account Groups Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a managed "group" concept for virtual accounts — accounts belong to at most one optional group, groups are managed on a dedicated page, and each group's combined balance shows as a card on the dashboard.

**Architecture:** Add an `AccountGroup` Mongo collection (id, name, color) with its own repository, add a nullable `GroupId` reference to `VirtualAccount`, add `/Groups` CRUD pages following the existing `/Accounts` pattern, add a group dropdown to the account create/edit forms, and compute per-group totals in the dashboard model.

**Tech Stack:** ASP.NET Core 10.0 Razor Pages, MongoDB.Driver 2.28.0, Bootstrap 5.3.3, Moq + xunit.

## Global Constraints

- .NET 10.0, MongoDB.Driver 2.28.0, no new NuGet packages
- Follow existing patterns: repositories are Scoped DI services; models use `[BsonId]`, `[BsonElement]`, `[BsonIgnoreIfNull]`
- Server-side validation via `ModelState.AddModelError("Field", "msg")` in page handlers (no data annotations)
- Currency formatted as `"{value:N2} EUR"`; positive = `amount-positive`, negative = `amount-negative`
- Bootstrap 5.3.3, `bi` icons, `_ValidationScriptsPartial` included on form pages
- Every task ends with `dotnet build` (or `dotnet test`) passing and a commit

## File Structure

| File | Purpose |
|------|---------|
| `src/Piggy/Models/AccountGroup.cs` | **Create** — Group model (id, name, color, timestamps) |
| `src/Piggy/Data/IAccountGroupRepository.cs` | **Create** — Group repository interface |
| `src/Piggy/Data/AccountGroupRepository.cs` | **Create** — Group repository implementation |
| `src/Piggy/Data/IMongoDbContext.cs` | **Modify** — Add `AccountGroups` collection |
| `src/Piggy/Data/MongoDbContext.cs` | **Modify** — Add `AccountGroups` property + export/import |
| `src/Piggy/Models/VirtualAccount.cs` | **Modify** — Add nullable `GroupId` |
| `src/Piggy/Data/IVirtualAccountRepository.cs` | **Modify** — Add `CountByGroupAsync` |
| `src/Piggy/Data/VirtualAccountRepository.cs` | **Modify** — Implement `CountByGroupAsync` |
| `src/Piggy/Program.cs` | **Modify** — Register `IAccountGroupRepository` |
| `src/Piggy/Pages/Groups/Index.cshtml(.cs)` | **Create** — Group list + delete |
| `src/Piggy/Pages/Groups/Create.cshtml(.cs)` | **Create** — New group form |
| `src/Piggy/Pages/Groups/Edit.cshtml(.cs)` | **Create** — Edit group form |
| `src/Piggy/Pages/Shared/_Layout.cshtml` | **Modify** — Add "Groups" nav link |
| `src/Piggy/Pages/Accounts/Create.cshtml(.cs)` | **Modify** — Group dropdown |
| `src/Piggy/Pages/Accounts/Edit.cshtml(.cs)` | **Modify** — Group dropdown |
| `src/Piggy/Pages/Accounts/Index.cshtml(.cs)` | **Modify** — Group badge column |
| `src/Piggy/Pages/Index.cshtml(.cs)` | **Modify** — Group total cards |
| `tests/Piggy.Tests/AccountGroupRepositoryTests.cs` | **Create** — Group repository tests |
| `tests/Piggy.Tests/VirtualAccountRepositoryTests.cs` | **Modify** — `CountByGroupAsync` tests |
| `tests/Piggy.Tests/AccountGroupPageTests.cs` | **Create** — Groups page delete-block tests |
| `tests/Piggy.Tests/IndexModelTests.cs` | **Modify** — constructor + group total tests |

---

### Task 1: AccountGroup Model + Repository + DI

**Files:**
- Create: `src/Piggy/Models/AccountGroup.cs`
- Create: `src/Piggy/Data/IAccountGroupRepository.cs`
- Create: `src/Piggy/Data/AccountGroupRepository.cs`
- Modify: `src/Piggy/Data/IMongoDbContext.cs`
- Modify: `src/Piggy/Data/MongoDbContext.cs`
- Modify: `src/Piggy/Program.cs`
- Test: `tests/Piggy.Tests/AccountGroupRepositoryTests.cs`

**Interfaces:**
- Consumes: `IMongoDbContext` (existing)
- Produces: `IAccountGroupRepository` with `GetAllAsync()`, `GetByIdAsync(string id)`, `CreateAsync(AccountGroup)`, `UpdateAsync(AccountGroup)`, `DeleteAsync(string id)`. `IMongoDbContext.AccountGroups` collection.

- [ ] **Step 1: Create AccountGroup model**

```csharp
// src/Piggy/Models/AccountGroup.cs
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Piggy.Models;

public class AccountGroup
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("color")]
    public string Color { get; set; } = "#6c757d";

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 2: Create IAccountGroupRepository interface**

```csharp
// src/Piggy/Data/IAccountGroupRepository.cs
using Piggy.Models;

namespace Piggy.Data;

public interface IAccountGroupRepository
{
    Task<List<AccountGroup>> GetAllAsync();
    Task<AccountGroup?> GetByIdAsync(string id);
    Task CreateAsync(AccountGroup group);
    Task UpdateAsync(AccountGroup group);
    Task DeleteAsync(string id);
}
```

- [ ] **Step 3: Create AccountGroupRepository implementation**

```csharp
// src/Piggy/Data/AccountGroupRepository.cs
using MongoDB.Driver;
using Piggy.Models;

namespace Piggy.Data;

public class AccountGroupRepository : IAccountGroupRepository
{
    private readonly IMongoDbContext _context;

    public AccountGroupRepository(IMongoDbContext context)
    {
        _context = context;
    }

    public async Task<List<AccountGroup>> GetAllAsync()
    {
        return await _context.AccountGroups.Find(_ => true).ToListAsync();
    }

    public async Task<AccountGroup?> GetByIdAsync(string id)
    {
        return await _context.AccountGroups.Find(g => g.Id == id).FirstOrDefaultAsync();
    }

    public async Task CreateAsync(AccountGroup group)
    {
        group.CreatedAt = DateTime.UtcNow;
        group.UpdatedAt = DateTime.UtcNow;
        await _context.AccountGroups.InsertOneAsync(group);
    }

    public async Task UpdateAsync(AccountGroup group)
    {
        group.UpdatedAt = DateTime.UtcNow;
        await _context.AccountGroups.ReplaceOneAsync(g => g.Id == group.Id, group);
    }

    public async Task DeleteAsync(string id)
    {
        await _context.AccountGroups.DeleteOneAsync(g => g.Id == id);
    }
}
```

- [ ] **Step 4: Add AccountGroups to IMongoDbContext**

In `src/Piggy/Data/IMongoDbContext.cs`, add the collection property (after the `TOTPSecrets` line):

```csharp
    IMongoCollection<AccountGroup> AccountGroups { get; }
```

- [ ] **Step 5: Add AccountGroups to MongoDbContext + export/import**

Add the collection property to `src/Piggy/Data/MongoDbContext.cs`:

```csharp
    public IMongoCollection<AccountGroup> AccountGroups =>
        _database.GetCollection<AccountGroup>("accountGroups");
```

Replace the entire `ExportAsync` method with (adds `accountGroups` to the loaded list and the document):

```csharp
    public async Task<string> ExportAsync()
    {
        var virtualAccounts = await VirtualAccounts.Find(new BsonDocument()).ToListAsync();
        var transactions = await Transactions.Find(new BsonDocument()).ToListAsync();
        var splitTemplates = await SplitTemplates.Find(new BsonDocument()).ToListAsync();
        var settings = await Settings.Find(new BsonDocument()).ToListAsync();
        var totpSecrets = await TOTPSecrets.Find(new BsonDocument()).ToListAsync();
        var accountGroups = await AccountGroups.Find(new BsonDocument()).ToListAsync();

        var doc = new BsonDocument
        {
            ["virtualAccounts"] = new BsonArray(virtualAccounts.Select(v => v.ToBsonDocument())),
            ["transactions"] = new BsonArray(transactions.Select(t => t.ToBsonDocument())),
            ["splitTemplates"] = new BsonArray(splitTemplates.Select(s => s.ToBsonDocument())),
            ["settings"] = new BsonArray(settings.Select(s => s.ToBsonDocument())),
            ["totpSecrets"] = new BsonArray(totpSecrets.Select(s => s.ToBsonDocument())),
            ["accountGroups"] = new BsonArray(accountGroups.Select(g => g.ToBsonDocument()))
        };

        return doc.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { Indent = true });
    }
```

Replace the `collections` dictionary inside `ImportAsync` with (adds `accountGroups`):

```csharp
        var collections = new Dictionary<string, IMongoCollection<BsonDocument>>
        {
            ["virtualAccounts"] = _database.GetCollection<BsonDocument>("virtualAccounts"),
            ["transactions"] = _database.GetCollection<BsonDocument>("transactions"),
            ["splitTemplates"] = _database.GetCollection<BsonDocument>("splitTemplates"),
            ["settings"] = _database.GetCollection<BsonDocument>("settings"),
            ["totpSecrets"] = _database.GetCollection<BsonDocument>("totpSecrets"),
            ["accountGroups"] = _database.GetCollection<BsonDocument>("accountGroups")
        };
```

- [ ] **Step 6: Register IAccountGroupRepository in Program.cs**

In `src/Piggy/Program.cs`, add after the `ITOTPSecretRepository` registration (line 74):

```csharp
builder.Services.AddScoped<IAccountGroupRepository, AccountGroupRepository>();
```

- [ ] **Step 7: Write AccountGroupRepository tests**

```csharp
// tests/Piggy.Tests/AccountGroupRepositoryTests.cs
using MongoDB.Driver;
using Moq;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Tests;

public class AccountGroupRepositoryTests
{
    private readonly Mock<IMongoDbContext> _mockContext;
    private readonly Mock<IMongoCollection<AccountGroup>> _mockCollection;
    private readonly AccountGroupRepository _repository;

    public AccountGroupRepositoryTests()
    {
        _mockContext = new Mock<IMongoDbContext>();
        _mockCollection = new Mock<IMongoCollection<AccountGroup>>();
        _mockContext.Setup(c => c.AccountGroups).Returns(_mockCollection.Object);
        _repository = new AccountGroupRepository(_mockContext.Object);
    }

    [Fact]
    public void Constructor_InitializesWithDbContext()
    {
        var repo = new AccountGroupRepository(_mockContext.Object);
        Assert.NotNull(repo);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAtAndUpdatedAt()
    {
        var group = new AccountGroup { Name = "Savings" };

        _mockCollection.Setup(c => c.InsertOneAsync(
            It.IsAny<AccountGroup>(),
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _repository.CreateAsync(group);

        Assert.True(group.CreatedAt <= DateTime.UtcNow);
        Assert.True(group.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAt()
    {
        var group = new AccountGroup { Id = "123", Name = "Updated" };

        _mockCollection.Setup(c => c.ReplaceOneAsync(
            It.IsAny<FilterDefinition<AccountGroup>>(),
            It.IsAny<AccountGroup>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));

        await _repository.UpdateAsync(group);

        Assert.True(group.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task DeleteAsync_CallsDeleteOneAsync()
    {
        _mockCollection.Setup(c => c.DeleteOneAsync(
            It.IsAny<FilterDefinition<AccountGroup>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        await _repository.DeleteAsync("123");

        _mockCollection.Verify(c => c.DeleteOneAsync(
            It.IsAny<FilterDefinition<AccountGroup>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

- [ ] **Step 8: Run tests**

Run: `dotnet test tests/Piggy.Tests/`
Expected: All tests pass (including the new AccountGroupRepositoryTests)

- [ ] **Step 9: Commit**

```bash
git add src/Piggy/Models/AccountGroup.cs src/Piggy/Data/IAccountGroupRepository.cs src/Piggy/Data/AccountGroupRepository.cs src/Piggy/Data/IMongoDbContext.cs src/Piggy/Data/MongoDbContext.cs src/Piggy/Program.cs tests/Piggy.Tests/AccountGroupRepositoryTests.cs
git commit -m "feat: add AccountGroup model and repository"
```

---

### Task 2: VirtualAccount.GroupId + CountByGroupAsync

**Files:**
- Modify: `src/Piggy/Models/VirtualAccount.cs`
- Modify: `src/Piggy/Data/IVirtualAccountRepository.cs`
- Modify: `src/Piggy/Data/VirtualAccountRepository.cs`
- Test: `tests/Piggy.Tests/VirtualAccountRepositoryTests.cs`

**Interfaces:**
- Consumes: nothing new
- Produces: `VirtualAccount.GroupId` (string?, `[BsonElement("groupId")]`, `[BsonIgnoreIfNull]`); `IVirtualAccountRepository.CountByGroupAsync(string groupId)` returning `Task<long>`.

- [ ] **Step 1: Add GroupId to VirtualAccount model**

In `src/Piggy/Models/VirtualAccount.cs`, add after the `MatchStrings` property (after line 23):

```csharp
    [BsonElement("groupId")]
    [BsonIgnoreIfNull]
    public string? GroupId { get; set; }
```

- [ ] **Step 2: Add CountByGroupAsync to IVirtualAccountRepository**

In `src/Piggy/Data/IVirtualAccountRepository.cs`, add:

```csharp
    Task<long> CountByGroupAsync(string groupId);
```

- [ ] **Step 3: Implement CountByGroupAsync in VirtualAccountRepository**

In `src/Piggy/Data/VirtualAccountRepository.cs`, add:

```csharp
    public async Task<long> CountByGroupAsync(string groupId)
    {
        return await _context.VirtualAccounts.CountDocumentsAsync(a => a.GroupId == groupId);
    }
```

- [ ] **Step 4: Add CountByGroupAsync tests**

Add to `tests/Piggy.Tests/VirtualAccountRepositoryTests.cs`:

```csharp
    [Fact]
    public async Task CountByGroupAsync_ReturnsCount()
    {
        _mockCollection.Setup(c => c.CountDocumentsAsync(
            It.IsAny<FilterDefinition<VirtualAccount>>(),
            It.IsAny<CountOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var count = await _repository.CountByGroupAsync("g1");

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CountByGroupAsync_ReturnsZero_WhenNoAccounts()
    {
        _mockCollection.Setup(c => c.CountDocumentsAsync(
            It.IsAny<FilterDefinition<VirtualAccount>>(),
            It.IsAny<CountOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var count = await _repository.CountByGroupAsync("g1");

        Assert.Equal(0, count);
    }
```

- [ ] **Step 5: Run tests**

Run: `dotnet test tests/Piggy.Tests/`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add src/Piggy/Models/VirtualAccount.cs src/Piggy/Data/IVirtualAccountRepository.cs src/Piggy/Data/VirtualAccountRepository.cs tests/Piggy.Tests/VirtualAccountRepositoryTests.cs
git commit -m "feat: add GroupId to virtual accounts and CountByGroupAsync"
```

---

### Task 3: Groups Management Pages + Nav

**Files:**
- Create: `src/Piggy/Pages/Groups/Index.cshtml.cs`
- Create: `src/Piggy/Pages/Groups/Index.cshtml`
- Create: `src/Piggy/Pages/Groups/Create.cshtml.cs`
- Create: `src/Piggy/Pages/Groups/Create.cshtml`
- Create: `src/Piggy/Pages/Groups/Edit.cshtml.cs`
- Create: `src/Piggy/Pages/Groups/Edit.cshtml`
- Modify: `src/Piggy/Pages/Shared/_Layout.cshtml`
- Test: `tests/Piggy.Tests/AccountGroupPageTests.cs`

**Interfaces:**
- Consumes: `IAccountGroupRepository` (Task 1), `IVirtualAccountRepository.CountByGroupAsync` (Task 2)
- Produces: `/Groups`, `/Groups/Create`, `/Groups/{id}` pages. `Groups.IndexModel.OnPostDeleteAsync(string id)` returns `PageResult` with `ErrorMessage` set when accounts are assigned; otherwise deletes and returns `RedirectToPageResult`.

- [ ] **Step 1: Create Groups/Index code-behind**

```csharp
// src/Piggy/Pages/Groups/Index.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Groups;

public class IndexModel : PageModel
{
    private readonly IAccountGroupRepository _groupRepo;
    private readonly IVirtualAccountRepository _accountRepo;

    public IndexModel(IAccountGroupRepository groupRepo, IVirtualAccountRepository accountRepo)
    {
        _groupRepo = groupRepo;
        _accountRepo = accountRepo;
    }

    public List<GroupWithCount> Groups { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var count = await _accountRepo.CountByGroupAsync(id);
        if (count > 0)
        {
            ErrorMessage = "This group still has virtual accounts assigned. Reassign or unassign them before deleting.";
            await LoadAsync();
            return Page();
        }

        await _groupRepo.DeleteAsync(id);
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var groups = await _groupRepo.GetAllAsync();
        Groups = new List<GroupWithCount>();
        foreach (var group in groups)
        {
            Groups.Add(new GroupWithCount
            {
                Group = group,
                AccountCount = await _accountRepo.CountByGroupAsync(group.Id)
            });
        }
    }

    public class GroupWithCount
    {
        public AccountGroup Group { get; set; } = new();
        public long AccountCount { get; set; }
    }
}
```

- [ ] **Step 2: Create Groups/Index view**

```html
@page
@model Piggy.Pages.Groups.IndexModel
@{
    ViewData["Title"] = "Groups";
}

<div class="d-flex justify-content-between align-items-center mb-4">
    <h2>Groups</h2>
    <a asp-page="Create" class="btn btn-primary">
        <i class="bi bi-plus"></i> New Group
    </a>
</div>

@if (!string.IsNullOrEmpty(Model.ErrorMessage))
{
    <div class="alert alert-danger">@Model.ErrorMessage</div>
}

@if (!Model.Groups.Any())
{
    <div class="alert alert-info">
        No groups yet. Create your first one to start grouping virtual accounts!
    </div>
}
else
{
    <table class="table">
        <thead>
            <tr>
                <th>Name</th>
                <th>Accounts</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var item in Model.Groups)
            {
                <tr>
                    <td>
                        <span class="d-inline-block rounded-circle me-2" style="width:12px;height:12px;background-color:@(item.Group.Color)"></span>
                        <strong>@item.Group.Name</strong>
                    </td>
                    <td>@item.AccountCount</td>
                    <td>
                        <a asp-page="Edit" asp-route-id="@item.Group.Id" class="btn btn-sm btn-outline-primary">
                            <i class="bi bi-pencil"></i>
                        </a>
                        <form method="post" asp-page-handler="Delete" class="d-inline">
                            <input type="hidden" name="id" value="@item.Group.Id" />
                            <button type="submit" class="btn btn-sm btn-outline-danger" onclick="return confirm('Delete this group?')">
                                <i class="bi bi-trash"></i>
                            </button>
                        </form>
                    </td>
                </tr>
            }
        </tbody>
    </table>
}
```

- [ ] **Step 3: Create Groups/Create code-behind**

```csharp
// src/Piggy/Pages/Groups/Create.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Groups;

public class CreateModel : PageModel
{
    private readonly IAccountGroupRepository _groupRepo;

    public CreateModel(IAccountGroupRepository groupRepo)
    {
        _groupRepo = groupRepo;
    }

    [BindProperty]
    public AccountGroup Group { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Group.Name))
        {
            ModelState.AddModelError("Group.Name", "Name is required");
            return Page();
        }

        await _groupRepo.CreateAsync(Group);
        return RedirectToPage("Index");
    }
}
```

- [ ] **Step 4: Create Groups/Create view**

```html
@page
@model Piggy.Pages.Groups.CreateModel
@{
    ViewData["Title"] = "Create Group";
}

<h2>Create Group</h2>

<div class="row">
    <div class="col-md-6">
        <form method="post">
            <div class="mb-3">
                <label asp-for="Group.Name" class="form-label">Name</label>
                <input asp-for="Group.Name" class="form-control" placeholder="e.g., Savings, Monthly" />
                <span asp-validation-for="Group.Name" class="text-danger"></span>
            </div>
            <div class="mb-3">
                <label asp-for="Group.Color" class="form-label">Color</label>
                <div class="d-flex align-items-center gap-2">
                    <input asp-for="Group.Color" type="color" class="form-control form-control-color" />
                    <span asp-validation-for="Group.Color" class="text-danger"></span>
                </div>
            </div>
            <button type="submit" class="btn btn-primary">Create</button>
            <a asp-page="Index" class="btn btn-secondary">Cancel</a>
        </form>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

- [ ] **Step 5: Create Groups/Edit code-behind**

```csharp
// src/Piggy/Pages/Groups/Edit.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Groups;

public class EditModel : PageModel
{
    private readonly IAccountGroupRepository _groupRepo;

    public EditModel(IAccountGroupRepository groupRepo)
    {
        _groupRepo = groupRepo;
    }

    [BindProperty]
    public AccountGroup Group { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var group = await _groupRepo.GetByIdAsync(id);
        if (group == null)
        {
            return NotFound();
        }

        Group = group;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Group.Name))
        {
            ModelState.AddModelError("Group.Name", "Name is required");
            return Page();
        }

        await _groupRepo.UpdateAsync(Group);
        return RedirectToPage("Index");
    }
}
```

- [ ] **Step 6: Create Groups/Edit view**

```html
@page "{id}"
@model Piggy.Pages.Groups.EditModel
@{
    ViewData["Title"] = "Edit Group";
}

<h2>Edit Group</h2>

<div class="row">
    <div class="col-md-6">
        <form method="post">
            <input type="hidden" asp-for="Group.Id" />
            <input type="hidden" asp-for="Group.CreatedAt" />
            <div class="mb-3">
                <label asp-for="Group.Name" class="form-label">Name</label>
                <input asp-for="Group.Name" class="form-control" />
                <span asp-validation-for="Group.Name" class="text-danger"></span>
            </div>
            <div class="mb-3">
                <label asp-for="Group.Color" class="form-label">Color</label>
                <div class="d-flex align-items-center gap-2">
                    <input asp-for="Group.Color" type="color" class="form-control form-control-color" />
                    <span asp-validation-for="Group.Color" class="text-danger"></span>
                </div>
            </div>
            <button type="submit" class="btn btn-primary">Save</button>
            <a asp-page="Index" class="btn btn-secondary">Cancel</a>
        </form>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

- [ ] **Step 7: Add Groups nav link to Layout**

In `src/Piggy/Pages/Shared/_Layout.cshtml`, add after the "Import" nav item (line 34):

```html
                    <li class="nav-item">
                        <a class="nav-link" asp-page="/Groups/Index">Groups</a>
                    </li>
```

- [ ] **Step 8: Write Groups page delete-block tests**

```csharp
// tests/Piggy.Tests/AccountGroupPageTests.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Piggy.Data;
using Piggy.Models;
using Piggy.Pages.Groups;

namespace Piggy.Tests;

public class AccountGroupPageTests
{
    private readonly Mock<IAccountGroupRepository> _groupRepo;
    private readonly Mock<IVirtualAccountRepository> _accountRepo;

    public AccountGroupPageTests()
    {
        _groupRepo = new Mock<IAccountGroupRepository>();
        _accountRepo = new Mock<IVirtualAccountRepository>();
    }

    [Fact]
    public async Task Delete_WithAssignedAccounts_ReturnsPageWithErrorAndDoesNotDelete()
    {
        _accountRepo.Setup(r => r.CountByGroupAsync("g1")).ReturnsAsync(2);
        _groupRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AccountGroup>());
        var model = new IndexModel(_groupRepo.Object, _accountRepo.Object);

        var result = await model.OnPostDeleteAsync("g1");

        Assert.IsType<PageResult>(result);
        Assert.False(string.IsNullOrEmpty(model.ErrorMessage));
        _groupRepo.Verify(r => r.DeleteAsync("g1"), Times.Never);
    }

    [Fact]
    public async Task Delete_WithoutAssignedAccounts_DeletesAndRedirects()
    {
        _accountRepo.Setup(r => r.CountByGroupAsync("g1")).ReturnsAsync(0);
        var model = new IndexModel(_groupRepo.Object, _accountRepo.Object);

        var result = await model.OnPostDeleteAsync("g1");

        Assert.IsType<RedirectToPageResult>(result);
        _groupRepo.Verify(r => r.DeleteAsync("g1"), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_LoadsGroupsWithAccountCounts()
    {
        var groups = new List<AccountGroup>
        {
            new() { Id = "g1", Name = "Savings" },
            new() { Id = "g2", Name = "Monthly" }
        };
        _groupRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(groups);
        _accountRepo.Setup(r => r.CountByGroupAsync("g1")).ReturnsAsync(3);
        _accountRepo.Setup(r => r.CountByGroupAsync("g2")).ReturnsAsync(1);
        var model = new IndexModel(_groupRepo.Object, _accountRepo.Object);

        await model.OnGetAsync();

        Assert.Equal(2, model.Groups.Count);
        Assert.Equal(3, model.Groups[0].AccountCount);
        Assert.Equal(1, model.Groups[1].AccountCount);
    }
}
```

- [ ] **Step 9: Run tests**

Run: `dotnet test tests/Piggy.Tests/`
Expected: All tests pass

- [ ] **Step 10: Verify build**

Run: `dotnet build src/Piggy/`
Expected: Build succeeds

- [ ] **Step 11: Commit**

```bash
git add src/Piggy/Pages/Groups/ src/Piggy/Pages/Shared/_Layout.cshtml tests/Piggy.Tests/AccountGroupPageTests.cs
git commit -m "feat: add Groups management pages"
```

---

### Task 4: Group Dropdown + Badge in Accounts Pages

**Files:**
- Modify: `src/Piggy/Pages/Accounts/Create.cshtml.cs`
- Modify: `src/Piggy/Pages/Accounts/Create.cshtml`
- Modify: `src/Piggy/Pages/Accounts/Edit.cshtml.cs`
- Modify: `src/Piggy/Pages/Accounts/Edit.cshtml`
- Modify: `src/Piggy/Pages/Accounts/Index.cshtml.cs`
- Modify: `src/Piggy/Pages/Accounts/Index.cshtml`

**Interfaces:**
- Consumes: `IAccountGroupRepository` (Task 1), `VirtualAccount.GroupId` (Task 2)
- Produces: Group `<select>` bound to `Account.GroupId` on create/edit; group badge column on the accounts list.

- [ ] **Step 1: Add group dropdown to Accounts/Create code-behind**

Replace the constructor and add `Groups` property in `src/Piggy/Pages/Accounts/Create.cshtml.cs`:

```csharp
public class CreateModel : PageModel
{
    private readonly IVirtualAccountRepository _accountRepo;
    private readonly IAccountGroupRepository _groupRepo;

    public CreateModel(IVirtualAccountRepository accountRepo, IAccountGroupRepository groupRepo)
    {
        _accountRepo = accountRepo;
        _groupRepo = groupRepo;
    }

    [BindProperty]
    public VirtualAccount Account { get; set; } = new();

    [BindProperty]
    public string? MatchStringsInput { get; set; }

    public List<AccountGroup> Groups { get; set; } = new();

    public async Task OnGetAsync()
    {
        Groups = await _groupRepo.GetAllAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Groups = await _groupRepo.GetAllAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        Account.MatchStrings = ParseMatchStrings(MatchStringsInput);
        await _accountRepo.CreateAsync(Account);
        return RedirectToPage("Index");
    }
    // ParseMatchStrings stays unchanged
```

- [ ] **Step 2: Add group dropdown to Accounts/Create view**

In `src/Piggy/Pages/Accounts/Create.cshtml`, add after the Starting Balance block (after line 29):

```html
            <div class="mb-3">
                <label asp-for="Account.GroupId" class="form-label">Group</label>
                <select asp-for="Account.GroupId" class="form-select">
                    <option value="">-- No group --</option>
                    @foreach (var group in Model.Groups)
                    {
                        <option value="@group.Id">@group.Name</option>
                    }
                </select>
            </div>
```

- [ ] **Step 3: Add group dropdown to Accounts/Edit code-behind**

Modify the constructor, add `Groups` property, and load groups in both handlers in `src/Piggy/Pages/Accounts/Edit.cshtml.cs`:

```csharp
public class EditModel : PageModel
{
    private readonly IVirtualAccountRepository _accountRepo;
    private readonly IAccountGroupRepository _groupRepo;

    public EditModel(IVirtualAccountRepository accountRepo, IAccountGroupRepository groupRepo)
    {
        _accountRepo = accountRepo;
        _groupRepo = groupRepo;
    }

    [BindProperty]
    public VirtualAccount Account { get; set; } = new();

    [BindProperty]
    public string? MatchStringsInput { get; set; }

    public List<AccountGroup> Groups { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var account = await _accountRepo.GetByIdAsync(id);
        if (account == null)
        {
            return NotFound();
        }

        Account = account;
        Groups = await _groupRepo.GetAllAsync();
        MatchStringsInput = account.MatchStrings != null ? string.Join(", ", account.MatchStrings) : null;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Groups = await _groupRepo.GetAllAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        Account.MatchStrings = ParseMatchStrings(MatchStringsInput);
        await _accountRepo.UpdateAsync(Account);
        return RedirectToPage("Index");
    }
    // ParseMatchStrings stays unchanged
```

- [ ] **Step 4: Add group dropdown to Accounts/Edit view**

In `src/Piggy/Pages/Accounts/Edit.cshtml`, add after the Starting Balance block (after line 31):

```html
            <div class="mb-3">
                <label asp-for="Account.GroupId" class="form-label">Group</label>
                <select asp-for="Account.GroupId" class="form-select">
                    <option value="">-- No group --</option>
                    @foreach (var group in Model.Groups)
                    {
                        <option value="@group.Id">@group.Name</option>
                    }
                </select>
            </div>
```

- [ ] **Step 5: Load groups in Accounts/Index code-behind**

In `src/Piggy/Pages/Accounts/Index.cshtml.cs`, add the group repo to the constructor, add a `GroupNames` lookup, and populate it in `OnGetAsync`:

```csharp
public class IndexModel : PageModel
{
    private readonly IVirtualAccountRepository _accountRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IAccountGroupRepository _groupRepo;

    public IndexModel(IVirtualAccountRepository accountRepo, ITransactionRepository transactionRepo, IAccountGroupRepository groupRepo)
    {
        _accountRepo = accountRepo;
        _transactionRepo = transactionRepo;
        _groupRepo = groupRepo;
    }

    public List<AccountWithBalance> Accounts { get; set; } = new();
    public Dictionary<string, string> GroupNames { get; set; } = new();

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

        GroupNames = (await _groupRepo.GetAllAsync()).ToDictionary(g => g.Id, g => g.Name);
    }
    // OnPostDeleteAsync and AccountWithBalance stay unchanged
```

- [ ] **Step 6: Add group badge column to Accounts/Index view**

In `src/Piggy/Pages/Accounts/Index.cshtml`, add a `Group` column to the table header and body:

```html
            <tr>
                <th>Name</th>
                <th>Group</th>
                <th>Starting Balance</th>
                <th>Current Balance</th>
                <th>Actions</th>
            </tr>
```

In the row, after the `<strong>@item.Account.Name</strong>` cell's closing `</td>`:

```html
                    <td>
                        @if (item.Account.GroupId != null && Model.GroupNames.TryGetValue(item.Account.GroupId, out var groupName))
                        {
                            <span class="badge bg-secondary">@groupName</span>
                        }
                    </td>
```

- [ ] **Step 7: Verify build**

Run: `dotnet build src/Piggy/`
Expected: Build succeeds

- [ ] **Step 8: Run tests**

Run: `dotnet test tests/Piggy.Tests/`
Expected: All tests pass

- [ ] **Step 9: Commit**

```bash
git add src/Piggy/Pages/Accounts/
git commit -m "feat: add group selection to virtual accounts"
```

---

### Task 5: Dashboard Group Totals

**Files:**
- Modify: `src/Piggy/Pages/Index.cshtml.cs`
- Modify: `src/Piggy/Pages/Index.cshtml`
- Test: `tests/Piggy.Tests/IndexModelTests.cs`

**Interfaces:**
- Consumes: `IAccountGroupRepository` (Task 1), `VirtualAccount.GroupId` (Task 2)
- Produces: `IndexModel.Groups` (`List<GroupWithTotal>`), each with `Group` and `Total`. `IndexModel` constructor now takes `(IVirtualAccountRepository, ITransactionRepository, IAccountGroupRepository)`.

- [ ] **Step 1: Update dashboard code-behind**

Replace `src/Piggy/Pages/Index.cshtml.cs` with:

```csharp
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages;

public class IndexModel : PageModel
{
    private readonly IVirtualAccountRepository _accountRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IAccountGroupRepository _groupRepo;

    public IndexModel(IVirtualAccountRepository accountRepo, ITransactionRepository transactionRepo, IAccountGroupRepository groupRepo)
    {
        _accountRepo = accountRepo;
        _transactionRepo = transactionRepo;
        _groupRepo = groupRepo;
    }

    public List<AccountWithBalance> Accounts { get; set; } = new();
    public List<GroupWithTotal> Groups { get; set; } = new();
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

        var groups = await _groupRepo.GetAllAsync();
        foreach (var group in groups)
        {
            var total = Accounts
                .Where(a => a.Account.GroupId == group.Id)
                .Sum(a => a.CurrentBalance);

            Groups.Add(new GroupWithTotal
            {
                Group = group,
                Total = total
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

    public class GroupWithTotal
    {
        public AccountGroup Group { get; set; } = new();
        public decimal Total { get; set; }
    }
}
```

- [ ] **Step 2: Add group total cards to dashboard view**

In `src/Piggy/Pages/Index.cshtml`, insert a group cards section between the stats row (line 26) and the "Virtual Accounts" heading (line 28):

```html
@if (Model.Groups.Any())
{
    <h3 class="mb-3">Groups</h3>
    <div class="row mb-4">
        @foreach (var group in Model.Groups)
        {
            <div class="col-md-4 mb-3">
                <div class="card summary-card" style="border-left-color:@(group.Group.Color)">
                    <div class="card-body">
                        <h6 class="card-title">
                            <span class="d-inline-block rounded-circle me-1" style="width:10px;height:10px;background-color:@(group.Group.Color)"></span>
                            @group.Group.Name
                        </h6>
                        <h4 class="@(group.Total >= 0 ? "amount-positive" : "amount-negative")">
                            @group.Total.ToString("N2") EUR
                        </h4>
                        <small class="text-muted">@Model.Accounts.Count(a => a.Account.GroupId == group.Group.Id) account(s)</small>
                    </div>
                </div>
            </div>
        }
    </div>
}
```

- [ ] **Step 3: Update IndexModelTests constructor**

In `tests/Piggy.Tests/IndexModelTests.cs`, add a group repo mock and pass it to the model:

```csharp
    private readonly Mock<IVirtualAccountRepository> _mockAccountRepo;
    private readonly Mock<ITransactionRepository> _mockTransactionRepo;
    private readonly Mock<IAccountGroupRepository> _mockGroupRepo;
    private readonly IndexModel _model;

    public IndexModelTests()
    {
        _mockAccountRepo = new Mock<IVirtualAccountRepository>();
        _mockTransactionRepo = new Mock<ITransactionRepository>();
        _mockGroupRepo = new Mock<IAccountGroupRepository>();
        _model = new IndexModel(_mockAccountRepo.Object, _mockTransactionRepo.Object, _mockGroupRepo.Object);
    }
```

Also add a setup to the class constructor so `GetAllAsync` on the group repo returns an empty list by default (so existing tests still pass):

```csharp
        _mockGroupRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AccountGroup>());
```

- [ ] **Step 4: Add group total tests**

Add to `tests/Piggy.Tests/IndexModelTests.cs`:

```csharp
    [Fact]
    public async Task OnGetAsync_CalculatesGroupTotals()
    {
        var accounts = new List<VirtualAccount>
        {
            new() { Id = "1", Name = "Savings A", GroupId = "g1", StartingBalance = 100m },
            new() { Id = "2", Name = "Savings B", GroupId = "g1", StartingBalance = 50m },
            new() { Id = "3", Name = "Monthly", GroupId = "g2", StartingBalance = 200m },
            new() { Id = "4", Name = "Unassigned", StartingBalance = 10m }
        };
        var groups = new List<AccountGroup>
        {
            new() { Id = "g1", Name = "Savings" },
            new() { Id = "g2", Name = "Monthly" }
        };

        _mockAccountRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(accounts);
        _mockTransactionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Transaction>());
        _mockGroupRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(groups);

        await _model.OnGetAsync();

        Assert.Equal(2, _model.Groups.Count);
        Assert.Equal(150m, _model.Groups[0].Total);
        Assert.Equal(200m, _model.Groups[1].Total);
    }

    [Fact]
    public async Task OnGetAsync_EmptyGroup_HasZeroTotal()
    {
        var accounts = new List<VirtualAccount>
        {
            new() { Id = "1", Name = "Monthly", GroupId = "g2", StartingBalance = 100m }
        };
        var groups = new List<AccountGroup>
        {
            new() { Id = "g1", Name = "Savings" },
            new() { Id = "g2", Name = "Monthly" }
        };

        _mockAccountRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(accounts);
        _mockTransactionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Transaction>());
        _mockGroupRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(groups);

        await _model.OnGetAsync();

        Assert.Equal(2, _model.Groups.Count);
        Assert.Equal(0m, _model.Groups[0].Total);
        Assert.Equal(100m, _model.Groups[1].Total);
    }
```

- [ ] **Step 5: Run tests**

Run: `dotnet test tests/Piggy.Tests/`
Expected: All tests pass

- [ ] **Step 6: Verify build**

Run: `dotnet build src/Piggy/`
Expected: Build succeeds

- [ ] **Step 7: Commit**

```bash
git add src/Piggy/Pages/Index.cshtml.cs src/Piggy/Pages/Index.cshtml tests/Piggy.Tests/IndexModelTests.cs
git commit -m "feat: show group totals on dashboard"
```

---

### Task 6: End-to-End Verification

**Files:**
- None (verification only)

**Interfaces:**
- Consumes: All tasks 1-5
- Produces: Confirmation that the feature works end-to-end

- [ ] **Step 1: Build the project**

Run: `dotnet build src/Piggy/`
Expected: Build succeeds with no errors

- [ ] **Step 2: Run all tests**

Run: `dotnet test tests/Piggy.Tests/`
Expected: All tests pass

- [ ] **Step 3: Manual verification checklist**

1. Navigate to **Groups** — create a group "Savings" and a group "Monthly"
2. Go to **Accounts**, create a virtual account and assign it to "Savings" — verify the badge shows on the accounts list
3. Edit an existing account and change its group to "Monthly" — verify it persists
4. On the **Dashboard** — verify group total cards appear above "Virtual Accounts" and each sums the correct accounts
5. Try to delete "Savings" while an account is still assigned — verify an error is shown and the group is not deleted
6. Reassign or unassign the accounts, then delete the group — verify it succeeds and the dashboard cards update
