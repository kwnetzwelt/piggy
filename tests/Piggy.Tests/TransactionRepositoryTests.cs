using MongoDB.Driver;
using Moq;
using Piggy.Data;
using Piggy.Models;

namespace Piggy.Tests;

public class TransactionRepositoryTests
{
    private readonly Mock<IMongoDbContext> _mockContext;
    private readonly Mock<IMongoCollection<Transaction>> _mockCollection;
    private readonly Mock<IVirtualAccountRepository> _mockAccountRepo;
    private readonly TransactionRepository _repository;

    public TransactionRepositoryTests()
    {
        _mockContext = new Mock<IMongoDbContext>();
        _mockCollection = new Mock<IMongoCollection<Transaction>>();
        _mockAccountRepo = new Mock<IVirtualAccountRepository>();
        _mockContext.Setup(c => c.Transactions).Returns(_mockCollection.Object);
        _repository = new TransactionRepository(_mockContext.Object, _mockAccountRepo.Object);
    }

    [Fact]
    public void Constructor_InitializesWithDbContext()
    {
        var repo = new TransactionRepository(_mockContext.Object, _mockAccountRepo.Object);
        Assert.NotNull(repo);
    }

    [Fact]
    public async Task CreateAsync_SetsImportedAt()
    {
        var transaction = new Transaction { ExternalId = "tx_1", Amount = 100 };

        _mockCollection.Setup(c => c.InsertOneAsync(
            It.IsAny<Transaction>(),
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _repository.CreateAsync(transaction);

        Assert.True(transaction.ImportedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateManyAsync_CallsInsertManyAsync()
    {
        var transactions = new List<Transaction>
        {
            new() { ExternalId = "tx_1", Amount = 100 },
            new() { ExternalId = "tx_2", Amount = 200 }
        };

        _mockCollection.Setup(c => c.InsertManyAsync(
            It.IsAny<IEnumerable<Transaction>>(),
            It.IsAny<InsertManyOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _repository.CreateManyAsync(transactions);

        _mockCollection.Verify(c => c.InsertManyAsync(
            It.IsAny<IEnumerable<Transaction>>(),
            It.IsAny<InsertManyOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CallsReplaceOneAsync()
    {
        var transaction = new Transaction { Id = "123", ExternalId = "tx_1", Amount = 100 };

        _mockCollection.Setup(c => c.ReplaceOneAsync(
            It.IsAny<FilterDefinition<Transaction>>(),
            It.IsAny<Transaction>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));

        await _repository.UpdateAsync(transaction);

        _mockCollection.Verify(c => c.ReplaceOneAsync(
            It.IsAny<FilterDefinition<Transaction>>(),
            It.IsAny<Transaction>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
