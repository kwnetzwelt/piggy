using Moq;
using Piggy.Data;
using Piggy.Models;
using TransactionsIndexModel = Piggy.Pages.Transactions.IndexModel;

namespace Piggy.Tests;

public class TransactionsIndexModelTests
{
    [Fact]
    public async Task OnGetAsync_UntaggedFilter_ExcludesSplitTransactions()
    {
        var mockTransactionRepo = new Mock<ITransactionRepository>();
        var mockAccountRepo = new Mock<IVirtualAccountRepository>();
        var mockSplitRepo = new Mock<ISplitTemplateRepository>();
        var mockSettingsRepo = new Mock<ISettingsRepository>();

        var transactions = new List<Transaction>
        {
            new() { Id = "t1", ExternalId = "e1", VirtualAccountId = null, Amount = -20m, Date = DateTime.Now, Description = "Untagged" },
            new() { Id = "t2", ExternalId = "e2", VirtualAccountId = null, SplitFrom = "e2", Amount = -30m, Date = DateTime.Now, Description = "Split" }
        };

        mockAccountRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<VirtualAccount>());
        mockSplitRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<SplitTemplate>());
        mockTransactionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(transactions);
        mockTransactionRepo.Setup(r => r.GetSuggestedAccountIdAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

        var model = new TransactionsIndexModel(mockTransactionRepo.Object, mockAccountRepo.Object, mockSplitRepo.Object, mockSettingsRepo.Object)
        {
            Filter = "untagged"
        };

        await model.OnGetAsync();

        var listed = model.Transactions.Select(t => t.ExternalId).ToList();
        Assert.Contains("e1", listed);
        Assert.DoesNotContain("e2", listed);
    }
}
