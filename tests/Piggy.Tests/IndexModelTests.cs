using Moq;
using Piggy.Data;
using Piggy.Models;
using Piggy.Pages;

namespace Piggy.Tests;

public class IndexModelTests
{
    private readonly Mock<IVirtualAccountRepository> _mockAccountRepo;
    private readonly Mock<ITransactionRepository> _mockTransactionRepo;
    private readonly Mock<IAccountGroupRepository> _mockGroupRepo;
    private readonly IndexModel _model;

    public IndexModelTests()
    {
        _mockAccountRepo = new Mock<IVirtualAccountRepository>();
        _mockTransactionRepo = new Mock<ITransactionRepository>();
        _mockGroupRepo = new Mock<IAccountGroupRepository>();
        _mockGroupRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AccountGroup>());
        _model = new IndexModel(_mockAccountRepo.Object, _mockTransactionRepo.Object, _mockGroupRepo.Object);
    }

    [Fact]
    public async Task OnGetAsync_LoadsAccounts()
    {
        var accounts = new List<VirtualAccount>
        {
            new() { Id = "1", Name = "Groceries", StartingBalance = 100m }
        };
        var transactions = new List<Transaction>
        {
            new() { Id = "t1", VirtualAccountId = "1", Amount = -50m, Date = DateTime.Now, Description = "Supermarket" }
        };

        _mockAccountRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(accounts);
        _mockTransactionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(transactions);

        await _model.OnGetAsync();

        Assert.Single(_model.Accounts);
        Assert.Equal("Groceries", _model.Accounts[0].Account.Name);
        Assert.Equal(50m, _model.Accounts[0].CurrentBalance);
    }

    [Fact]
    public async Task OnGetAsync_CalculatesCorrectBalance()
    {
        var accounts = new List<VirtualAccount>
        {
            new() { Id = "1", Name = "Savings", StartingBalance = 200m }
        };
        var transactions = new List<Transaction>
        {
            new() { Id = "t1", VirtualAccountId = "1", Amount = -30m, Date = DateTime.Now, Description = "Transfer" },
            new() { Id = "t2", VirtualAccountId = "1", Amount = 50m, Date = DateTime.Now, Description = "Deposit" }
        };

        _mockAccountRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(accounts);
        _mockTransactionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(transactions);

        await _model.OnGetAsync();

        Assert.Equal(220m, _model.Accounts[0].CurrentBalance);
    }

    [Fact]
    public async Task OnGetAsync_LoadsRecentTransactions()
    {
        _mockAccountRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<VirtualAccount>());
        var transactions = new List<Transaction>
        {
            new() { Id = "t1", Amount = -10m, Date = DateTime.Now, Description = "Coffee" }
        };

        _mockTransactionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(transactions);

        await _model.OnGetAsync();

        Assert.Single(_model.RecentTransactions);
    }

    [Fact]
    public async Task OnGetAsync_CountsTotalTransactions()
    {
        _mockAccountRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<VirtualAccount>());
        var transactions = new List<Transaction>
        {
            new() { Id = "t1", Amount = -10m, Date = DateTime.Now, Description = "A" },
            new() { Id = "t2", Amount = -20m, Date = DateTime.Now, Description = "B" },
            new() { Id = "t3", Amount = -30m, Date = DateTime.Now, Description = "C" }
        };

        _mockTransactionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(transactions);

        await _model.OnGetAsync();

        Assert.Equal(3, _model.TotalTransactions);
    }

    [Fact]
    public async Task OnGetAsync_CountsUntaggedTransactions()
    {
        _mockAccountRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<VirtualAccount>());
        var transactions = new List<Transaction>
        {
            new() { Id = "t1", VirtualAccountId = "1", Amount = -10m, Date = DateTime.Now, Description = "Tagged" },
            new() { Id = "t2", VirtualAccountId = null, Amount = -20m, Date = DateTime.Now, Description = "Untagged" },
            new() { Id = "t3", VirtualAccountId = null, Amount = -30m, Date = DateTime.Now, Description = "Untagged2" }
        };

        _mockTransactionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(transactions);

        await _model.OnGetAsync();

        Assert.Equal(2, _model.UntaggedCount);
    }

    [Fact]
    public async Task OnGetAsync_UntaggedCount_ExcludesSplitTransactions()
    {
        _mockAccountRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<VirtualAccount>());
        var transactions = new List<Transaction>
        {
            new() { Id = "t1", VirtualAccountId = "1", Amount = -10m, Date = DateTime.Now, Description = "Tagged" },
            new() { Id = "t2", VirtualAccountId = null, Amount = -20m, Date = DateTime.Now, Description = "Untagged" },
            new() { Id = "t3", VirtualAccountId = null, SplitFrom = "t3", Amount = -30m, Date = DateTime.Now, Description = "Split" }
        };

        _mockTransactionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(transactions);

        await _model.OnGetAsync();

        Assert.Equal(1, _model.UntaggedCount);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsEmptyCollections_WhenNoData()
    {
        _mockAccountRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<VirtualAccount>());
        _mockTransactionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Transaction>());

        await _model.OnGetAsync();

        Assert.Empty(_model.Accounts);
        Assert.Empty(_model.RecentTransactions);
        Assert.Equal(0, _model.TotalTransactions);
        Assert.Equal(0, _model.UntaggedCount);
    }

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
}
