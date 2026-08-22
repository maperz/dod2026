using DevOpsDays2026.Data;
using DevOpsDays2026.Models;
using Xunit;

namespace DevOpsDays2026.Tests;

public sealed class StockNewsRepositoryTests : RepositoryTestBase
{
    [Fact]
    public async Task Should_be_able_to_create()
    {
        var connectionFactory = CreateSnowflakeConnectionFactory();
        var repository = new StockNewsRepository(connectionFactory);
        var id = Guid.NewGuid();
        var createRequest = CreateStockNewsRequest(id);

        try
        {
            var created = await repository.CreateAsync(createRequest);

            Assert.Equal(id, created.Id);
            Assert.Equal("MSFT", created.Ticker);
            Assert.Equal(createRequest.Text, created.Text);
            Assert.Equal(DateTime.Parse(createRequest.Date), created.Date);
        }
        finally
        {
            await repository.DeleteAsync(id);
        }
    }

    [Fact]
    public async Task Should_be_able_to_create_and_patch()
    {
        var connectionFactory = CreateSnowflakeConnectionFactory();
        var repository = new StockNewsRepository(connectionFactory);
        var id = Guid.NewGuid();
        var createRequest = CreateStockNewsRequest(id);

        try
        {
            await repository.CreateAsync(createRequest);

            var updateRequest = createRequest with
            {
                Text = "MSFT closes higher in integration test."
            };

            var updated = await repository.UpdateAsync(id, updateRequest);
            var byId = await repository.GetByIdAsync(id);
            var byTicker = await repository.GetAllAsync("MSFT");

            Assert.NotNull(updated);
            Assert.NotNull(byId);
            Assert.Equal(updateRequest.Text, byId.Text);
            Assert.Contains(byTicker, row => row.Id == id);
        }
        finally
        {
            await repository.DeleteAsync(id);
        }
    }

    [Fact]
    public async Task Should_be_able_to_delete_after_creating()
    {
        var connectionFactory = CreateSnowflakeConnectionFactory();
        var repository = new StockNewsRepository(connectionFactory);
        var id = Guid.NewGuid();
        var createRequest = CreateStockNewsRequest(id);

        await repository.CreateAsync(createRequest);
        await repository.DeleteAsync(id);

        var deleted = await repository.GetByIdAsync(id);
        Assert.Null(deleted);
    }

    private static StockNewsRequest CreateStockNewsRequest(Guid id)
    {
        return new StockNewsRequest(
            id,
            "MSFT",
            "MSFT opens higher in integration test.",
            "2026-08-15");
    }
}
