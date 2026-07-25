# Piggy - Virtual Account Transaction Manager Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a web app that connects to N26 bank via Go CLI and allows users to tag transactions to local virtual accounts.

**Architecture:** ASP.NET Core 10.0 Razor Pages + MongoDB + Go CLI (guitmz/n26) in a Docker container. Single container with MongoDB in docker-compose.

**Tech Stack:** .NET 10.0, MongoDB.Driver, Bootstrap 5, Docker

## Global Constraints

- .NET SDK 10.0
- MongoDB 7
- Docker Compose
- Go CLI (guitmz/n26) installed in Docker container
- No authentication on the web app (for now)
- Transaction-level tagging (not space-level)

---

## File Structure

```
piggy/
├── src/
│   └── Piggy/
│       ├── Piggy.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── Models/
│       │   ├── VirtualAccount.cs
│       │   ├── Transaction.cs
│       │   └── N26Settings.cs
│       ├── Data/
│       │   ├── MongoDbContext.cs
│       │   ├── VirtualAccountRepository.cs
│       │   ├── TransactionRepository.cs
│       │   └── N26SettingsRepository.cs
│       ├── Services/
│       │   └── N26Service.cs
│       ├── Pages/
│       │   ├── _ViewImports.cshtml
│       │   ├── _ViewStart.cshtml
│       │   ├── Shared/
│       │   │   └── _Layout.cshtml
│       │   ├── Index.cshtml
│       │   ├── Index.cshtml.cs
│       │   ├── Accounts/
│       │   │   ├── Index.cshtml
│       │   │   ├── Index.cshtml.cs
│       │   │   ├── Create.cshtml
│       │   │   ├── Create.cshtml.cs
│       │   │   ├── Edit.cshtml
│       │   │   └── Edit.cshtml.cs
│       │   ├── Transactions/
│       │   │   ├── Index.cshtml
│       │   │   └── Index.cshtml.cs
│       │   ├── Import/
│       │   │   ├── Index.cshtml
│       │   │   └── Index.cshtml.cs
│       │   └── Settings/
│       │       ├── Index.cshtml
│       │       └── Index.cshtml.cs
│       └── wwwroot/
│           └── css/
│               └── site.css
├── Dockerfile
├── docker-compose.yml
└── docs/
    └── superpowers/
        └── specs/
            └── 2026-07-25-piggy-virtual-accounts-design.md
```

---

## Task 1: Project Setup and Docker

**Files:**
- Create: `src/Piggy/Piggy.csproj`
- Create: `src/Piggy/Program.cs`
- Create: `src/Piggy/appsettings.json`
- Create: `Dockerfile`
- Create: `docker-compose.yml`

**Interfaces:**
- Produces: Runnable ASP.NET Core app on port 5000 with MongoDB on 27017

- [ ] **Step 1: Create .NET project**

Run: `dotnet new webapi -n Piggy -o src/Piggy --no-https`

- [ ] **Step 2: Update Piggy.csproj to use .NET 10.0**

Edit `src/Piggy/Piggy.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>Piggy</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MongoDB.Driver" Version="2.28.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Configure appsettings.json**

Edit `src/Piggy/appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "piggy"
  }
}
```

- [ ] **Step 4: Update Program.cs with MongoDB**

Edit `src/Piggy/Program.cs`:
```csharp
using MongoDB.Driver;
using Piggy.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var mongoSettings = builder.Configuration.GetSection("MongoDb");
var connectionString = mongoSettings["ConnectionString"];
var databaseName = mongoSettings["DatabaseName"];

builder.Services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(databaseName);
});

builder.Services.AddScoped<VirtualAccountRepository>();
builder.Services.AddScoped<TransactionRepository>();
builder.Services.AddScoped<N26SettingsRepository>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
```

- [ ] **Step 5: Create Dockerfile**

Create `Dockerfile`:
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY src/Piggy/Piggy.csproj src/Piggy/
RUN dotnet restore src/Piggy/Piggy.csproj
COPY src/Piggy/ src/Piggy/
RUN dotnet publish src/Piggy/Piggy.csproj -c Release -o /app/publish

# Go builder stage
FROM golang:1.22 AS go-builder
RUN go install github.com/guitmz/n26/cmd/n26@latest

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
COPY --from=go-builder /go/bin/n26 /usr/local/bin/n26
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Piggy.dll"]
```

- [ ] **Step 6: Create docker-compose.yml**

Create `docker-compose.yml`:
```yaml
services:
  piggy:
    build: .
    ports:
      - "5000:8080"
    environment:
      - MongoDb__ConnectionString=mongodb://mongo:27017
      - MongoDb__DatabaseName=piggy
    depends_on:
      - mongo

  mongo:
    image: mongo:7
    ports:
      - "27017:27017"
    volumes:
      - mongo-data:/data/db

volumes:
  mongo-data:
```

- [ ] **Step 7: Build and test Docker**

Run: `docker compose build`
Run: `docker compose up -d`
Expected: App runs on http://localhost:5000, shows default weather forecast

- [ ] **Step 8: Commit**

```bash
git init
git add .
git commit -m "feat: project setup with Docker and MongoDB"
```

---

## Task 2: Data Models and MongoDB Context

**Files:**
- Create: `src/Piggy/Models/VirtualAccount.cs`
- Create: `src/Piggy/Models/Transaction.cs`
- Create: `src/Piggy/Models/N26Settings.cs`
- Create: `src/Piggy/Data/MongoDbContext.cs`

**Interfaces:**
- Produces: MongoDB collections for VirtualAccount, Transaction, N26Settings

- [ ] **Step 1: Create VirtualAccount model**

Create `src/Piggy/Models/VirtualAccount.cs`:
```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Piggy.Models;

public class VirtualAccount
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("startingBalance")]
    public decimal StartingBalance { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 2: Create Transaction model**

Create `src/Piggy/Models/Transaction.cs`:
```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Piggy.Models;

public class Transaction
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("n26Id")]
    public string N26Id { get; set; } = string.Empty;

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("currency")]
    public string Currency { get; set; } = "EUR";

    [BsonElement("date")]
    public DateTime Date { get; set; }

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("spaceId")]
    public string SpaceId { get; set; } = string.Empty;

    [BsonElement("spaceName")]
    public string SpaceName { get; set; } = string.Empty;

    [BsonElement("virtualAccountId")]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? VirtualAccountId { get; set; }

    [BsonElement("importedAt")]
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isProcessed")]
    public bool IsProcessed { get; set; }
}
```

- [ ] **Step 3: Create N26Settings model**

Create `src/Piggy/Models/N26Settings.cs`:
```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Piggy.Models;

public class N26Settings
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = "default";

    [BsonElement("username")]
    public string Username { get; set; } = string.Empty;

    [BsonElement("password")]
    public string Password { get; set; } = string.Empty;

    [BsonElement("deviceToken")]
    public string DeviceToken { get; set; } = string.Empty;

    [BsonElement("lastSyncAt")]
    [BsonIgnoreIfNull]
    public DateTime? LastSyncAt { get; set; }
}
```

- [ ] **Step 4: Create MongoDbContext**

Create `src/Piggy/Data/MongoDbContext.cs`:
```csharp
using MongoDB.Driver;
using Piggy.Models;

namespace Piggy.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IMongoClient client, string databaseName)
    {
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<VirtualAccount> VirtualAccounts =>
        _database.GetCollection<VirtualAccount>("virtualAccounts");

    public IMongoCollection<Transaction> Transactions =>
        _database.GetCollection<Transaction>("transactions");

    public IMongoCollection<N26Settings> N26Settings =>
        _database.GetCollection<N26Settings>("n26Settings");
}
```

- [ ] **Step 5: Update Program.cs to register MongoDbContext**

Edit `src/Piggy/Program.cs`, replace the three repository registrations with:
```csharp
builder.Services.AddSingleton<MongoDbContext>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var databaseName = mongoSettings["DatabaseName"]!;
    return new MongoDbContext(client, databaseName);
});
```

- [ ] **Step 6: Build and verify**

Run: `dotnet build src/Piggy/Piggy.csproj`
Expected: Build succeeds with no errors

- [ ] **Step 7: Commit**

```bash
git add src/Piggy/Models/ src/Piggy/Data/MongoDbContext.cs
git commit -m "feat: add data models and MongoDB context"
```

---

## Task 3: Repository Layer

**Files:**
- Create: `src/Piggy/Data/VirtualAccountRepository.cs`
- Create: `src/Piggy/Data/TransactionRepository.cs`
- Create: `src/Piggy/Data/N26SettingsRepository.cs`

**Interfaces:**
- Consumes: MongoDbContext, Models
- Produces: CRUD operations for each entity

- [ ] **Step 1: Create VirtualAccountRepository**

Create `src/Piggy/Data/VirtualAccountRepository.cs`:
```csharp
using MongoDB.Driver;
using Piggy.Models;

namespace Piggy.Data;

public class VirtualAccountRepository
{
    private readonly MongoDbContext _context;

    public VirtualAccountRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<List<VirtualAccount>> GetAllAsync()
    {
        return await _context.VirtualAccounts.Find(_ => true).ToListAsync();
    }

    public async Task<VirtualAccount?> GetByIdAsync(string id)
    {
        return await _context.VirtualAccounts.Find(a => a.Id == id).FirstOrDefaultAsync();
    }

    public async Task CreateAsync(VirtualAccount account)
    {
        account.CreatedAt = DateTime.UtcNow;
        account.UpdatedAt = DateTime.UtcNow;
        await _context.VirtualAccounts.InsertOneAsync(account);
    }

    public async Task UpdateAsync(VirtualAccount account)
    {
        account.UpdatedAt = DateTime.UtcNow;
        await _context.VirtualAccounts.ReplaceOneAsync(a => a.Id == account.Id, account);
    }

    public async Task DeleteAsync(string id)
    {
        await _context.VirtualAccounts.DeleteOneAsync(a => a.Id == id);
    }
}
```

- [ ] **Step 2: Create TransactionRepository**

Create `src/Piggy/Data/TransactionRepository.cs`:
```csharp
using MongoDB.Driver;
using Piggy.Models;

namespace Piggy.Data;

public class TransactionRepository
{
    private readonly MongoDbContext _context;

    public TransactionRepository(MongoDbContext context)
    {
        _context = context;
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
            .Find(t => t.VirtualAccountId == null)
            .SortByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<Transaction?> GetByN26IdAsync(string n26Id)
    {
        return await _context.Transactions
            .Find(t => t.N26Id == n26Id)
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

    public async Task<bool> ExistsByN26IdAsync(string n26Id)
    {
        return await _context.Transactions.Find(t => t.N26Id == n26Id).AnyAsync();
    }
}
```

- [ ] **Step 3: Create N26SettingsRepository**

Create `src/Piggy/Data/N26SettingsRepository.cs`:
```csharp
using MongoDB.Driver;
using Piggy.Models;

namespace Piggy.Data;

public class N26SettingsRepository
{
    private readonly MongoDbContext _context;

    public N26SettingsRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<N26Settings?> GetAsync()
    {
        return await _context.N26Settings.Find(s => s.Id == "default").FirstOrDefaultAsync();
    }

    public async Task CreateOrUpdateAsync(N26Settings settings)
    {
        var existing = await _context.N26Settings.Find(s => s.Id == "default").FirstOrDefaultAsync();
        if (existing == null)
        {
            settings.Id = "default";
            await _context.N26Settings.InsertOneAsync(settings);
        }
        else
        {
            settings.Id = "default";
            await _context.N26Settings.ReplaceOneAsync(s => s.Id == "default", settings);
        }
    }
}
```

- [ ] **Step 4: Build and verify**

Run: `dotnet build src/Piggy/Piggy.csproj`
Expected: Build succeeds with no errors

- [ ] **Step 5: Commit**

```bash
git add src/Piggy/Data/*Repository.cs
git commit -m "feat: add repository layer for CRUD operations"
```

---

## Task 4: N26 Service (Go CLI Integration)

**Files:**
- Create: `src/Piggy/Services/N26Service.cs`

**Interfaces:**
- Consumes: N26SettingsRepository, TransactionRepository
- Produces: ImportTransactionsAsync(), GetTransactionsFromN26()

- [ ] **Step 1: Create N26Service**

Create `src/Piggy/Services/N26Service.cs`:
```csharp
using System.Diagnostics;
using System.Text.Json;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Services;

public class N26Service
{
    private readonly N26SettingsRepository _settingsRepo;
    private readonly TransactionRepository _transactionRepo;
    private readonly ILogger<N26Service> _logger;

    public N26Service(
        N26SettingsRepository settingsRepo,
        TransactionRepository transactionRepo,
        ILogger<N26Service> logger)
    {
        _settingsRepo = settingsRepo;
        _transactionRepo = transactionRepo;
        _logger = logger;
    }

    public async Task<ImportResult> ImportTransactionsAsync()
    {
        var settings = await _settingsRepo.GetAsync();
        if (settings == null)
        {
            return new ImportResult { Success = false, Message = "N26 credentials not configured" };
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "n26",
                Arguments = "transactions json",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Environment =
                {
                    ["N26_USERNAME"] = settings.Username,
                    ["N26_PASSWORD"] = settings.Password,
                    ["N26_DEVICE_TOKEN"] = settings.DeviceToken
                }
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            _logger.LogError("N26 CLI error: {Error}", error);
            return new ImportResult { Success = false, Message = $"N26 CLI error: {error}" };
        }

        var transactions = ParseTransactions(output);
        var imported = 0;
        var skipped = 0;

        foreach (var transaction in transactions)
        {
            var exists = await _transactionRepo.ExistsByN26IdAsync(transaction.N26Id);
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

        settings.LastSyncAt = DateTime.UtcNow;
        await _settingsRepo.CreateOrUpdateAsync(settings);

        return new ImportResult
        {
            Success = true,
            Message = $"Imported {imported} transactions, skipped {skipped} duplicates",
            Imported = imported,
            Skipped = skipped
        };
    }

    private List<Transaction> ParseTransactions(string json)
    {
        var result = new List<Transaction>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("transactions", out var transactionsArray))
            {
                foreach (var item in transactionsArray.EnumerateArray())
                {
                    var transaction = new Transaction
                    {
                        N26Id = item.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                        Amount = item.TryGetProperty("amount", out var amount) ? amount.GetDecimal() : 0,
                        Currency = item.TryGetProperty("currency", out var currency) ? currency.GetString() ?? "EUR" : "EUR",
                        Date = item.TryGetProperty("visibleTS", out var ts) ? DateTimeOffset.FromUnixTimeMilliseconds(ts.GetInt64()).DateTime : DateTime.MinValue,
                        Description = item.TryGetProperty("merchantName", out var merchant) ? merchant.GetString() ?? "" : "",
                        SpaceId = item.TryGetProperty("spaceId", out var spaceId) ? spaceId.GetString() ?? "" : "",
                        SpaceName = item.TryGetProperty("spaceName", out var spaceName) ? spaceName.GetString() ?? "" : ""
                    };

                    if (string.IsNullOrEmpty(transaction.Description))
                    {
                        transaction.Description = item.TryGetProperty("partnerName", out var partner) ? partner.GetString() ?? "" : "";
                    }

                    result.Add(transaction);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse N26 transactions");
        }

        return result;
    }
}

public class ImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Imported { get; set; }
    public int Skipped { get; set; }
}
```

- [ ] **Step 2: Register N26Service in Program.cs**

Edit `src/Piggy/Program.cs`, add after repository registrations:
```csharp
builder.Services.AddScoped<N26Service>();
```

Also add the using at the top:
```csharp
using Piggy.Services;
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build src/Piggy/Piggy.csproj`
Expected: Build succeeds with no errors

- [ ] **Step 4: Commit**

```bash
git add src/Piggy/Services/N26Service.cs
git commit -m "feat: add N26 service with Go CLI integration"
```

---

## Task 5: Layout and Navigation

**Files:**
- Create: `src/Piggy/Pages/_ViewImports.cshtml`
- Create: `src/Piggy/Pages/_ViewStart.cshtml`
- Create: `src/Piggy/Pages/Shared/_Layout.cshtml`
- Create: `src/Piggy/wwwroot/css/site.css`

**Interfaces:**
- Produces: Shared layout with navigation

- [ ] **Step 1: Create _ViewImports**

Create `src/Piggy/Pages/_ViewImports.cshtml`:
```cshtml
@using Piggy
@using Piggy.Models
@namespace Piggy.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

- [ ] **Step 2: Create _ViewStart**

Create `src/Piggy/Pages/_ViewStart.cshtml`:
```cshtml
@{
    Layout = "_Layout";
}
```

- [ ] **Step 3: Create _Layout**

Create `src/Piggy/Pages/Shared/_Layout.cshtml`:
```cshtml
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - Piggy</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link href="~/css/site.css" rel="stylesheet" />
</head>
<body>
    <nav class="navbar navbar-expand-lg navbar-dark bg-dark">
        <div class="container">
            <a class="navbar-brand" asp-page="/Index">
                <i class="bi bi-piggy-bank"></i> Piggy
            </a>
            <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav">
                <span class="navbar-toggler-icon"></span>
            </button>
            <div class="collapse navbar-collapse" id="navbarNav">
                <ul class="navbar-nav">
                    <li class="nav-item">
                        <a class="nav-link" asp-page="/Index">Dashboard</a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" asp-page="/Accounts/Index">Accounts</a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" asp-page="/Transactions/Index">Transactions</a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" asp-page="/Import/Index">Import</a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" asp-page="/Settings/Index">Settings</a>
                    </li>
                </ul>
            </div>
        </div>
    </nav>

    <main class="container mt-4">
        @RenderBody()
    </main>

    <footer class="container mt-4 mb-4">
        <hr />
        <p class="text-muted text-center">Piggy - Virtual Account Manager</p>
    </footer>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

- [ ] **Step 4: Create site.css**

Create `src/Piggy/wwwroot/css/site.css`:
```css
body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
}

.summary-card {
    border-left: 4px solid;
    transition: transform 0.2s;
}

.summary-card:hover {
    transform: translateY(-2px);
}

.amount-positive {
    color: #198754;
}

.amount-negative {
    color: #dc3545;
}

.transaction-row {
    cursor: default;
}

.tag-select {
    min-width: 150px;
}
```

- [ ] **Step 5: Build and verify**

Run: `dotnet build src/Piggy/Piggy.csproj`
Expected: Build succeeds with no errors

- [ ] **Step 6: Commit**

```bash
git add src/Piggy/Pages/Shared/ src/Piggy/Pages/_ViewImports.cshtml src/Piggy/Pages/_ViewStart.cshtml src/Piggy/wwwroot/
git commit -m "feat: add shared layout and navigation"
```

---

## Task 6: Dashboard Page

**Files:**
- Create: `src/Piggy/Pages/Index.cshtml`
- Create: `src/Piggy/Pages/Index.cshtml.cs`

**Interfaces:**
- Consumes: VirtualAccountRepository, TransactionRepository
- Produces: Dashboard with summary cards and recent transactions

- [ ] **Step 1: Create Index.cshtml.cs**

Create `src/Piggy/Pages/Index.cshtml.cs`:
```csharp
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages;

public class IndexModel : PageModel
{
    private readonly VirtualAccountRepository _accountRepo;
    private readonly TransactionRepository _transactionRepo;

    public IndexModel(VirtualAccountRepository accountRepo, TransactionRepository transactionRepo)
    {
        _accountRepo = accountRepo;
        _transactionRepo = transactionRepo;
    }

    public List<AccountWithBalance> Accounts { get; set; } = new();
    public List<Transaction> RecentTransactions { get; set; } = new();
    public int TotalTransactions { get; set; }
    public int UntaggedCount { get; set; }
    public DateTime? LastSync { get; set; }

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
        UntaggedCount = transactions.Count(t => t.VirtualAccountId == null);
    }

    public class AccountWithBalance
    {
        public VirtualAccount Account { get; set; } = new();
        public decimal CurrentBalance { get; set; }
    }
}
```

- [ ] **Step 2: Create Index.cshtml**

Create `src/Piggy/Pages/Index.cshtml`:
```cshtml
@page
@model IndexModel
@{
    ViewData["Title"] = "Dashboard";
}

<div class="row mb-4">
    <div class="col-md-4">
        <div class="card">
            <div class="card-body">
                <h5 class="card-title text-muted">Total Transactions</h5>
                <h2>@Model.TotalTransactions</h2>
            </div>
        </div>
    </div>
    <div class="col-md-4">
        <div class="card">
            <div class="card-body">
                <h5 class="card-title text-muted">Untagged</h5>
                <h2 class="@(Model.UntaggedCount > 0 ? "text-warning" : "")">@Model.UntaggedCount</h2>
            </div>
        </div>
    </div>
    <div class="col-md-4">
        <div class="card">
            <div class="card-body">
                <h5 class="card-title text-muted">Last Sync</h5>
                <h6>@(Model.LastSync?.ToString("g") ?? "Never")</h6>
            </div>
        </div>
    </div>
</div>

<h3 class="mb-3">Virtual Accounts</h3>
<div class="row mb-4">
    @if (!Model.Accounts.Any())
    {
        <div class="col-12">
            <div class="alert alert-info">
                No virtual accounts yet. <a asp-page="/Accounts/Create">Create one</a>.
            </div>
        </div>
    }
    @foreach (var item in Model.Accounts)
    {
        <div class="col-md-4 mb-3">
            <div class="card summary-card" style="border-left-color: @(item.CurrentBalance >= 0 ? "#198754" : "#dc3545")">
                <div class="card-body">
                    <h6 class="card-title">@item.Account.Name</h6>
                    <h4 class="@(item.CurrentBalance >= 0 ? "amount-positive" : "amount-negative")">
                        @item.CurrentBalance.ToString("N2") EUR
                    </h4>
                    <small class="text-muted">Starting: @item.Account.StartingBalance.ToString("N2") EUR</small>
                </div>
            </div>
        </div>
    }
</div>

<h3 class="mb-3">Recent Transactions</h3>
@if (!Model.RecentTransactions.Any())
{
    <div class="alert alert-info">
        No transactions yet. <a asp-page="/Import/Index">Import from N26</a>.
    </div>
}
else
{
    <table class="table table-hover">
        <thead>
            <tr>
                <th>Date</th>
                <th>Description</th>
                <th>Amount</th>
                <th>Space</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var tx in Model.RecentTransactions)
            {
                <tr>
                    <td>@tx.Date.ToString("d")</td>
                    <td>@tx.Description</td>
                    <td class="@(tx.Amount >= 0 ? "amount-positive" : "amount-negative")">
                        @tx.Amount.ToString("N2") EUR
                    </td>
                    <td>@(tx.SpaceName ?? "Main")</td>
                </tr>
            }
        </tbody>
    </table>
}
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build src/Piggy/Piggy.csproj`
Expected: Build succeeds with no errors

- [ ] **Step 4: Commit**

```bash
git add src/Piggy/Pages/Index.cshtml src/Piggy/Pages/Index.cshtml.cs
git commit -m "feat: add dashboard page with summary cards"
```

---

## Task 7: Virtual Accounts CRUD

**Files:**
- Create: `src/Piggy/Pages/Accounts/Index.cshtml`
- Create: `src/Piggy/Pages/Accounts/Index.cshtml.cs`
- Create: `src/Piggy/Pages/Accounts/Create.cshtml`
- Create: `src/Piggy/Pages/Accounts/Create.cshtml.cs`
- Create: `src/Piggy/Pages/Accounts/Edit.cshtml`
- Create: `src/Piggy/Pages/Accounts/Edit.cshtml.cs`

**Interfaces:**
- Consumes: VirtualAccountRepository, TransactionRepository
- Produces: List, Create, Edit pages for virtual accounts

- [ ] **Step 1: Create Accounts/Index.cshtml.cs**

Create `src/Piggy/Pages/Accounts/Index.cshtml.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Accounts;

public class IndexModel : PageModel
{
    private readonly VirtualAccountRepository _accountRepo;
    private readonly TransactionRepository _transactionRepo;

    public IndexModel(VirtualAccountRepository accountRepo, TransactionRepository transactionRepo)
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
```

- [ ] **Step 2: Create Accounts/Index.cshtml**

Create `src/Piggy/Pages/Accounts/Index.cshtml`:
```cshtml
@page
@model Piggy.Pages.Accounts.IndexModel
@{
    ViewData["Title"] = "Virtual Accounts";
}

<div class="d-flex justify-content-between align-items-center mb-4">
    <h2>Virtual Accounts</h2>
    <a asp-page="Create" class="btn btn-primary">
        <i class="bi bi-plus"></i> New Account
    </a>
</div>

@if (!Model.Accounts.Any())
{
    <div class="alert alert-info">
        No virtual accounts yet. Create your first one to start tracking!
    </div>
}
else
{
    <table class="table">
        <thead>
            <tr>
                <th>Name</th>
                <th>Starting Balance</th>
                <th>Current Balance</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var item in Model.Accounts)
            {
                <tr>
                    <td><strong>@item.Account.Name</strong></td>
                    <td>@item.Account.StartingBalance.ToString("N2") EUR</td>
                    <td class="@(item.CurrentBalance >= 0 ? "amount-positive" : "amount-negative")">
                        @item.CurrentBalance.ToString("N2") EUR
                    </td>
                    <td>
                        <a asp-page="Edit" asp-route-id="@item.Account.Id" class="btn btn-sm btn-outline-primary">
                            <i class="bi bi-pencil"></i>
                        </a>
                        <form method="post" asp-page-handler="Delete" class="d-inline">
                            <input type="hidden" name="id" value="@item.Account.Id" />
                            <button type="submit" class="btn btn-sm btn-outline-danger" onclick="return confirm('Delete this account?')">
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

- [ ] **Step 3: Create Accounts/Create.cshtml.cs**

Create `src/Piggy/Pages/Accounts/Create.cshtml.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Accounts;

public class CreateModel : PageModel
{
    private readonly VirtualAccountRepository _accountRepo;

    public CreateModel(VirtualAccountRepository accountRepo)
    {
        _accountRepo = accountRepo;
    }

    [BindProperty]
    public VirtualAccount Account { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _accountRepo.CreateAsync(Account);
        return RedirectToPage("Index");
    }
}
```

- [ ] **Step 4: Create Accounts/Create.cshtml**

Create `src/Piggy/Pages/Accounts/Create.cshtml`:
```cshtml
@page
@model Piggy.Pages.Accounts.CreateModel
@{
    ViewData["Title"] = "Create Virtual Account";
}

<h2>Create Virtual Account</h2>

<div class="row">
    <div class="col-md-6">
        <form method="post">
            <div class="mb-3">
                <label asp-for="Account.Name" class="form-label">Name</label>
                <input asp-for="Account.Name" class="form-control" placeholder="e.g., ISP, House, Car" />
                <span asp-validation-for="Account.Name" class="text-danger"></span>
            </div>
            <div class="mb-3">
                <label asp-for="Account.StartingBalance" class="form-label">Starting Balance (EUR)</label>
                <input asp-for="Account.StartingBalance" class="form-control" type="number" step="0.01" />
                <span asp-validation-for="Account.StartingBalance" class="text-danger"></span>
                <small class="text-muted">The current value in this virtual account</small>
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

- [ ] **Step 5: Create Accounts/Edit.cshtml.cs**

Create `src/Piggy/Pages/Accounts/Edit.cshtml.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Accounts;

public class EditModel : PageModel
{
    private readonly VirtualAccountRepository _accountRepo;

    public EditModel(VirtualAccountRepository accountRepo)
    {
        _accountRepo = accountRepo;
    }

    [BindProperty]
    public VirtualAccount Account { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var account = await _accountRepo.GetByIdAsync(id);
        if (account == null)
        {
            return NotFound();
        }

        Account = account;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _accountRepo.UpdateAsync(Account);
        return RedirectToPage("Index");
    }
}
```

- [ ] **Step 6: Create Accounts/Edit.cshtml**

Create `src/Piggy/Pages/Accounts/Edit.cshtml`:
```cshtml
@page "{id}"
@model Piggy.Pages.Accounts.EditModel
@{
    ViewData["Title"] = "Edit Virtual Account";
}

<h2>Edit Virtual Account</h2>

<div class="row">
    <div class="col-md-6">
        <form method="post">
            <input type="hidden" asp-for="Account.Id" />
            <input type="hidden" asp-for="Account.CreatedAt" />
            <div class="mb-3">
                <label asp-for="Account.Name" class="form-label">Name</label>
                <input asp-for="Account.Name" class="form-control" />
                <span asp-validation-for="Account.Name" class="text-danger"></span>
            </div>
            <div class="mb-3">
                <label asp-for="Account.StartingBalance" class="form-label">Starting Balance (EUR)</label>
                <input asp-for="Account.StartingBalance" class="form-control" type="number" step="0.01" />
                <span asp-validation-for="Account.StartingBalance" class="text-danger"></span>
                <small class="text-muted">Adjust this if your starting balance was incorrect</small>
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

- [ ] **Step 7: Build and verify**

Run: `dotnet build src/Piggy/Piggy.csproj`
Expected: Build succeeds with no errors

- [ ] **Step 8: Commit**

```bash
git add src/Piggy/Pages/Accounts/
git commit -m "feat: add virtual accounts CRUD pages"
```

---

## Task 8: Transactions Page with Tagging

**Files:**
- Create: `src/Piggy/Pages/Transactions/Index.cshtml`
- Create: `src/Piggy/Pages/Transactions/Index.cshtml.cs`

**Interfaces:**
- Consumes: TransactionRepository, VirtualAccountRepository
- Produces: Transaction list with dropdown for tagging

- [ ] **Step 1: Create Transactions/Index.cshtml.cs**

Create `src/Piggy/Pages/Transactions/Index.cshtml.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Transactions;

public class IndexModel : PageModel
{
    private readonly TransactionRepository _transactionRepo;
    private readonly VirtualAccountRepository _accountRepo;

    public IndexModel(TransactionRepository transactionRepo, VirtualAccountRepository accountRepo)
    {
        _transactionRepo = transactionRepo;
        _accountRepo = accountRepo;
    }

    public List<Transaction> Transactions { get; set; } = new();
    public List<VirtualAccount> Accounts { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SpaceFilter { get; set; }

    public async Task OnGetAsync()
    {
        Accounts = await _accountRepo.GetAllAsync();
        var allTransactions = await _transactionRepo.GetAllAsync();

        if (Filter == "untagged")
        {
            allTransactions = allTransactions.Where(t => t.VirtualAccountId == null).ToList();
        }
        else if (Filter == "tagged")
        {
            allTransactions = allTransactions.Where(t => t.VirtualAccountId != null).ToList();
        }

        if (!string.IsNullOrEmpty(SpaceFilter))
        {
            allTransactions = allTransactions.Where(t => t.SpaceId == SpaceFilter).ToList();
        }

        Transactions = allTransactions;
    }

    public async Task<IActionResult> OnPostTagAsync(string transactionId, string? virtualAccountId)
    {
        var transaction = await _transactionRepo.GetByN26IdAsync(transactionId);
        if (transaction != null)
        {
            transaction.VirtualAccountId = string.IsNullOrEmpty(virtualAccountId) ? null : virtualAccountId;
            transaction.IsProcessed = !string.IsNullOrEmpty(virtualAccountId);
            await _transactionRepo.UpdateAsync(transaction);
        }

        return RedirectToPage(new { Filter, SpaceFilter });
    }
}
```

- [ ] **Step 2: Create Transactions/Index.cshtml**

Create `src/Piggy/Pages/Transactions/Index.cshtml`:
```cshtml
@page
@model Piggy.Pages.Transactions.IndexModel
@{
    ViewData["Title"] = "Transactions";
}

<h2 class="mb-4">Transactions</h2>

<div class="row mb-4">
    <div class="col-md-4">
        <a asp-page="Index" class="btn @(Model.Filter == null ? "btn-primary" : "btn-outline-primary") btn-sm">All</a>
        <a asp-page="Index" asp-route-Filter="untagged" class="btn @(Model.Filter == "untagged" ? "btn-warning" : "btn-outline-warning") btn-sm">Untagged</a>
        <a asp-page="Index" asp-route-Filter="tagged" class="btn @(Model.Filter == "tagged" ? "btn-success" : "btn-outline-success") btn-sm">Tagged</a>
    </div>
</div>

@if (!Model.Transactions.Any())
{
    <div class="alert alert-info">
        No transactions found. <a asp-page="/Import/Index">Import from N26</a>.
    </div>
}
else
{
    <table class="table table-hover">
        <thead>
            <tr>
                <th>Date</th>
                <th>Description</th>
                <th>Amount</th>
                <th>Space</th>
                <th>Virtual Account</th>
                <th>Action</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var tx in Model.Transactions)
            {
                <tr class="@(tx.VirtualAccountId == null ? "table-warning" : "")">
                    <td>@tx.Date.ToString("d")</td>
                    <td>@tx.Description</td>
                    <td class="@(tx.Amount >= 0 ? "amount-positive" : "amount-negative")">
                        @tx.Amount.ToString("N2") EUR
                    </td>
                    <td>@(tx.SpaceName ?? "Main")</td>
                    <td>
                        <form method="post" asp-page-handler="Tag" class="d-flex">
                            <input type="hidden" name="transactionId" value="@tx.N26Id" />
                            <select name="virtualAccountId" class="form-select form-select-sm tag-select" onchange="this.form.submit()">
                                <option value="">-- Select --</option>
                                @foreach (var account in Model.Accounts)
                                {
                                    <option value="@account.Id" selected="@(tx.VirtualAccountId == account.Id)">
                                        @account.Name
                                    </option>
                                }
                            </select>
                        </form>
                    </td>
                    <td>
                        @if (tx.VirtualAccountId != null)
                        {
                            <span class="badge bg-success">Tagged</span>
                        }
                    </td>
                </tr>
            }
        </tbody>
    </table>
}
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build src/Piggy/Piggy.csproj`
Expected: Build succeeds with no errors

- [ ] **Step 4: Commit**

```bash
git add src/Piggy/Pages/Transactions/
git commit -m "feat: add transactions page with tagging dropdown"
```

---

## Task 9: Import Page

**Files:**
- Create: `src/Piggy/Pages/Import/Index.cshtml`
- Create: `src/Piggy/Pages/Import/Index.cshtml.cs`

**Interfaces:**
- Consumes: N26Service
- Produces: Import page with trigger button and result display

- [ ] **Step 1: Create Import/Index.cshtml.cs**

Create `src/Piggy/Pages/Import/Index.cshtml.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Services;

namespace Piggy.Pages.Import;

public class IndexModel : PageModel
{
    private readonly N26Service _n26Service;

    public IndexModel(N26Service n26Service)
    {
        _n26Service = n26Service;
    }

    public ImportResult? Result { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Result = await _n26Service.ImportTransactionsAsync();
        return Page();
    }
}
```

- [ ] **Step 2: Create Import/Index.cshtml**

Create `src/Piggy/Pages/Import/Index.cshtml`:
```cshtml
@page
@model Piggy.Pages.Import.IndexModel
@{
    ViewData["Title"] = "Import from N26";
}

<h2 class="mb-4">Import from N26</h2>

<div class="row">
    <div class="col-md-8">
        <div class="card mb-4">
            <div class="card-body">
                <h5 class="card-title">Sync Transactions</h5>
                <p class="card-text">
                    Click the button below to import new transactions from your N26 account.
                    You will need to approve the login on your N26 phone app within 60 seconds.
                </p>

                @if (Model.Result != null)
                {
                    <div class="alert @(Model.Result.Success ? "alert-success" : "alert-danger")">
                        @Model.Result.Message
                    </div>
                }

                <form method="post">
                    <button type="submit" class="btn btn-primary">
                        <i class="bi bi-cloud-download"></i> Import Transactions
                    </button>
                </form>
            </div>
        </div>

        <div class="card">
            <div class="card-body">
                <h5 class="card-title">How it works</h5>
                <ol>
                    <li>Click "Import Transactions"</li>
                    <li>Approve the login request on your N26 phone app</li>
                    <li>Wait for the import to complete</li>
                    <li>New transactions will be added to your list</li>
                    <li>Go to <a asp-page="/Transactions/Index">Transactions</a> to tag them</li>
                </ol>
            </div>
        </div>
    </div>
</div>
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build src/Piggy/Piggy.csproj`
Expected: Build succeeds with no errors

- [ ] **Step 4: Commit**

```bash
git add src/Piggy/Pages/Import/
git commit -m "feat: add import page for N26 transactions"
```

---

## Task 10: Settings Page

**Files:**
- Create: `src/Piggy/Pages/Settings/Index.cshtml`
- Create: `src/Piggy/Pages/Settings/Index.cshtml.cs`

**Interfaces:**
- Consumes: N26SettingsRepository
- Produces: Settings page for N26 credentials

- [ ] **Step 1: Create Settings/Index.cshtml.cs**

Create `src/Piggy/Pages/Settings/Index.cshtml.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Pages.Settings;

public class IndexModel : PageModel
{
    private readonly N26SettingsRepository _settingsRepo;

    public IndexModel(N26SettingsRepository settingsRepo)
    {
        _settingsRepo = settingsRepo;
    }

    [BindProperty]
    public N26Settings Settings { get; set; } = new();

    public bool IsSaved { get; set; }

    public async Task OnGetAsync()
    {
        var settings = await _settingsRepo.GetAsync();
        if (settings != null)
        {
            Settings = settings;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _settingsRepo.CreateOrUpdateAsync(Settings);
        IsSaved = true;
        return Page();
    }
}
```

- [ ] **Step 2: Create Settings/Index.cshtml**

Create `src/Piggy/Pages/Settings/Index.cshtml`:
```cshtml
@page
@model Piggy.Pages.Settings.IndexModel
@{
    ViewData["Title"] = "Settings";
}

<h2 class="mb-4">Settings</h2>

<div class="row">
    <div class="col-md-6">
        <div class="card">
            <div class="card-body">
                <h5 class="card-title">N26 Configuration</h5>

                @if (Model.IsSaved)
                {
                    <div class="alert alert-success">
                        Settings saved successfully!
                    </div>
                }

                <form method="post">
                    <div class="mb-3">
                        <label asp-for="Settings.Username" class="form-label">Email</label>
                        <input asp-for="Settings.Username" class="form-control" type="email" />
                        <span asp-validation-for="Settings.Username" class="text-danger"></span>
                    </div>
                    <div class="mb-3">
                        <label asp-for="Settings.Password" class="form-label">Password</label>
                        <input asp-for="Settings.Password" class="form-control" type="password" />
                        <span asp-validation-for="Settings.Password" class="text-danger"></span>
                    </div>
                    <div class="mb-3">
                        <label asp-for="Settings.DeviceToken" class="form-label">Device Token (UUID)</label>
                        <input asp-for="Settings.DeviceToken" class="form-control" />
                        <span asp-validation-for="Settings.DeviceToken" class="text-danger"></span>
                        <small class="text-muted">
                            Generate a UUID at <a href="https://www.uuidgenerator.net" target="_blank">uuidgenerator.net</a>
                        </small>
                    </div>
                    <button type="submit" class="btn btn-primary">Save Settings</button>
                </form>
            </div>
        </div>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build src/Piggy/Piggy.csproj`
Expected: Build succeeds with no errors

- [ ] **Step 4: Commit**

```bash
git add src/Piggy/Pages/Settings/
git commit -m "feat: add settings page for N26 credentials"
```

---

## Task 11: Final Testing and Docker Build

**Files:**
- Modify: `Dockerfile` (if needed)
- Modify: `docker-compose.yml` (if needed)

**Interfaces:**
- Produces: Working Docker deployment

- [ ] **Step 1: Build Docker image**

Run: `docker compose build`
Expected: Build succeeds

- [ ] **Step 2: Start containers**

Run: `docker compose up -d`
Expected: Both containers start successfully

- [ ] **Step 3: Test all pages**

Run: `curl http://localhost:5000/`
Expected: Dashboard page loads

Run: `curl http://localhost:5000/accounts`
Expected: Accounts page loads

Run: `curl http://localhost:5000/transactions`
Expected: Transactions page loads

Run: `curl http://localhost:5000/settings`
Expected: Settings page loads

- [ ] **Step 4: Test MongoDB connection**

Run: `docker compose exec mongo mongosh --eval "db.getSiblingDB('piggy').virtualAccounts.find()"`
Expected: Empty result (no accounts yet)

- [ ] **Step 5: Final commit**

```bash
git add .
git commit -m "chore: final Docker configuration and testing"
```

---

## Summary

| Task | Description | Files |
|------|-------------|-------|
| 1 | Project Setup and Docker | 5 files |
| 2 | Data Models and MongoDB Context | 4 files |
| 3 | Repository Layer | 3 files |
| 4 | N26 Service (Go CLI Integration) | 1 file |
| 5 | Layout and Navigation | 4 files |
| 6 | Dashboard Page | 2 files |
| 7 | Virtual Accounts CRUD | 6 files |
| 8 | Transactions Page with Tagging | 2 files |
| 9 | Import Page | 2 files |
| 10 | Settings Page | 2 files |
| 11 | Final Testing and Docker Build | 2 files |

**Total: ~33 files created/modified**
