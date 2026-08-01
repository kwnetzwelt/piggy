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
