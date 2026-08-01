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
