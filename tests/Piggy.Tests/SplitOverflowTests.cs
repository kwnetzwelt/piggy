using Moq;
using Piggy.Data;
using Piggy.Models;
using Piggy.Pages.Transactions;
using Microsoft.AspNetCore.Mvc;

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
            ExternalId = "tx-1",
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

        _transactionRepoMock.Setup(r => r.GetByExternalIdAsync("tx-1"))
            .ReturnsAsync(transaction);
        _splitRepoMock.Setup(r => r.GetByIdAsync("tpl-1"))
            .ReturnsAsync(template);
        _settingsRepoMock.Setup(r => r.GetAsync())
            .ReturnsAsync(settings);

        var result = await _tagModel.OnPostSplitAsync("tx-1", "tpl-1");

        Assert.IsType<RedirectToPageResult>(result);

        _transactionRepoMock.Verify(r => r.CreateAsync(
            It.Is<Transaction>(t => t.IsOverflow && t.Amount == 20m)),
            Times.Once);
    }

    [Fact]
    public async Task Split_WithoutOverflow_DoesNotCreateOverflowTransaction()
    {
        var transaction = new Transaction
        {
            ExternalId = "tx-2",
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

        _transactionRepoMock.Setup(r => r.GetByExternalIdAsync("tx-2"))
            .ReturnsAsync(transaction);
        _splitRepoMock.Setup(r => r.GetByIdAsync("tpl-2"))
            .ReturnsAsync(template);
        _settingsRepoMock.Setup(r => r.GetAsync())
            .ReturnsAsync(settings);

        var result = await _tagModel.OnPostSplitAsync("tx-2", "tpl-2");

        Assert.IsType<RedirectToPageResult>(result);

        _transactionRepoMock.Verify(r => r.CreateAsync(
            It.Is<Transaction>(t => t.IsOverflow)),
            Times.Never);
    }

    [Fact]
    public async Task Split_OverflowWithoutAccount_DoesNotCreateOverflowTransaction()
    {
        var transaction = new Transaction
        {
            ExternalId = "tx-3",
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

        _transactionRepoMock.Setup(r => r.GetByExternalIdAsync("tx-3"))
            .ReturnsAsync(transaction);
        _splitRepoMock.Setup(r => r.GetByIdAsync("tpl-3"))
            .ReturnsAsync(template);
        _settingsRepoMock.Setup(r => r.GetAsync())
            .ReturnsAsync(settings);

        var result = await _tagModel.OnPostSplitAsync("tx-3", "tpl-3");

        Assert.IsType<RedirectToPageResult>(result);

        _transactionRepoMock.Verify(r => r.CreateAsync(
            It.Is<Transaction>(t => t.IsOverflow)),
            Times.Never);
    }

    [Fact]
    public async Task UndoSplit_DeletesOverflowTransaction()
    {
        var original = new Transaction
        {
            ExternalId = "tx-undo-1",
            Amount = 100m,
            SplitFrom = "tx-undo-1",
            SplitTemplateId = "tpl-1",
            IsProcessed = true
        };

        var child1 = new Transaction
        {
            ExternalId = "tx-undo-1-split-aaa",
            SplitFrom = "tx-undo-1",
            Amount = 60m,
            VirtualAccountId = "acc-1"
        };

        var overflowChild = new Transaction
        {
            ExternalId = "tx-undo-1-overflow-bbb",
            SplitFrom = "tx-undo-1",
            Amount = 40m,
            VirtualAccountId = "overflow-acc",
            IsOverflow = true
        };

        _transactionRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Transaction> { original, child1, overflowChild });
        _transactionRepoMock.Setup(r => r.GetByExternalIdAsync("tx-undo-1"))
            .ReturnsAsync(original);

        var result = await _tagModel.OnPostUndoAsync("split:tx-undo-1", null);

        Assert.IsType<RedirectToPageResult>(result);

        _transactionRepoMock.Verify(r => r.DeleteByExternalIdAsync("tx-undo-1-split-aaa"), Times.Once);
        _transactionRepoMock.Verify(r => r.DeleteByExternalIdAsync("tx-undo-1-overflow-bbb"), Times.Once);
    }
}
