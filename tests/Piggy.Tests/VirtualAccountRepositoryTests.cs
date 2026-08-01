using MongoDB.Driver;
using Moq;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Tests;

public class VirtualAccountRepositoryTests
{
    private readonly Mock<IMongoDbContext> _mockContext;
    private readonly Mock<IMongoCollection<VirtualAccount>> _mockCollection;
    private readonly VirtualAccountRepository _repository;

    public VirtualAccountRepositoryTests()
    {
        _mockContext = new Mock<IMongoDbContext>();
        _mockCollection = new Mock<IMongoCollection<VirtualAccount>>();
        _mockContext.Setup(c => c.VirtualAccounts).Returns(_mockCollection.Object);
        _repository = new VirtualAccountRepository(_mockContext.Object);
    }

    [Fact]
    public void Constructor_InitializesWithDbContext()
    {
        var repo = new VirtualAccountRepository(_mockContext.Object);
        Assert.NotNull(repo);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAtAndUpdatedAt()
    {
        var account = new VirtualAccount { Name = "New Account" };

        _mockCollection.Setup(c => c.InsertOneAsync(
            It.IsAny<VirtualAccount>(),
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _repository.CreateAsync(account);

        Assert.True(account.CreatedAt <= DateTime.UtcNow);
        Assert.True(account.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAt()
    {
        var account = new VirtualAccount { Id = "123", Name = "Updated" };

        _mockCollection.Setup(c => c.ReplaceOneAsync(
            It.IsAny<FilterDefinition<VirtualAccount>>(),
            It.IsAny<VirtualAccount>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));

        await _repository.UpdateAsync(account);

        Assert.True(account.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task DeleteAsync_CallsDeleteOneAsync()
    {
        _mockCollection.Setup(c => c.DeleteOneAsync(
            It.IsAny<FilterDefinition<VirtualAccount>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        await _repository.DeleteAsync("123");

        _mockCollection.Verify(c => c.DeleteOneAsync(
            It.IsAny<FilterDefinition<VirtualAccount>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

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
}
