using Moq;
using Piggy.Data;
using Piggy.Models;
using Piggy.Pages;

namespace Piggy.Tests;

public class IndexModelTests
{
    private readonly Mock<IVirtualAccountRepository> _mockAccountRepo;
    private readonly Mock<ITransactionRepository> _mockTransactionRepo;
    private readonly IndexModel _model;

    public IndexModelTests()
    {
        _mockAccountRepo = new Mock<IVirtualAccountRepository>();
        _mockTransactionRepo = new Mock<ITransactionRepository>();
        _model = new IndexModel(_mockAccountRepo.Object, _mockTransactionRepo.Object);
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
}
