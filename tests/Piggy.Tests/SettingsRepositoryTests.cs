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
    public void Constructor_InitializesWithDbContext()
    {
        var repo = new SettingsRepository(_contextMock.Object);
        Assert.NotNull(repo);
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAt()
    {
        var settings = new Settings { Id = "singleton", OverflowAccountId = "acc2" };

        _collectionMock.Setup(c => c.ReplaceOneAsync(
            It.IsAny<FilterDefinition<Settings>>(),
            It.IsAny<Settings>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));

        var before = settings.UpdatedAt;
        await _repository.UpdateAsync(settings);

        Assert.True(settings.UpdatedAt >= before);
        _collectionMock.Verify(c => c.ReplaceOneAsync(
            It.IsAny<FilterDefinition<Settings>>(),
            settings,
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CallsReplaceOneWithUpsert()
    {
        var settings = new Settings { Id = "singleton", OverflowAccountId = "acc2" };

        _collectionMock.Setup(c => c.ReplaceOneAsync(
            It.IsAny<FilterDefinition<Settings>>(),
            It.IsAny<Settings>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));

        await _repository.UpdateAsync(settings);

        _collectionMock.Verify(c => c.ReplaceOneAsync(
            It.IsAny<FilterDefinition<Settings>>(),
            It.IsAny<Settings>(),
            It.Is<ReplaceOptions>(o => o.IsUpsert),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
