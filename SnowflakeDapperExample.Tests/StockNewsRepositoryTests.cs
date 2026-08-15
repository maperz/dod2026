using Dapper;
using SnowflakeDapperExample.Data;
using SnowflakeDapperExample.Data.Common;
using SnowflakeDapperExample.Models;
using Xunit;

namespace SnowflakeDapperExample.Tests;

public sealed class StockNewsRepositoryTests
{
    private readonly StockNewsRepository _repository;

    public StockNewsRepositoryTests()
    {
        SqlMapper.Settings.UseIncrementalPseudoPositionalParameterNames = true;
        SqlMapper.AddTypeHandler(new GuidTypeHandler());
        EnvironmentFileLoader.Load(EnvironmentFileLoader.GetEnvironmentFilePath("ci.env"));

        var connectionFactory = new SnowflakeConnectionFactory(
            SnowflakeConnectionStringBuilder.BuildFromEnvironment());
        _repository = new StockNewsRepository(connectionFactory);
    }

    [Fact]
    public async Task Create_Update_Read_and_Delete_Stock_News()
    {
        var id = Guid.NewGuid();
        var createRequest = new StockNewsRequest(
            id,
            "MSFT",
            "MSFT opens higher in integration test.",
            "2026-08-15");

        try
        {
            var created = await _repository.CreateAsync(createRequest);

            Assert.Equal(id, created.Id);
            Assert.Equal("MSFT", created.Ticker);
            Assert.Equal(createRequest.Text, created.Text);
            Assert.Equal(DateTime.Parse(createRequest.Date), created.Date);

            var updateRequest = createRequest with
            {
                Text = "MSFT closes higher in integration test."
            };

            var updated = await _repository.UpdateAsync(id, updateRequest);
            var byId = await _repository.GetByIdAsync(id);
            var byTicker = await _repository.GetAllAsync("MSFT");

            Assert.NotNull(updated);
            Assert.NotNull(byId);
            Assert.Equal(updateRequest.Text, byId.Text);
            Assert.Contains(byTicker, row => row.Id == id);
        }
        finally
        {
            await _repository.DeleteAsync(id);
        }

        var deleted = await _repository.GetByIdAsync(id);
        Assert.Null(deleted);
    }
}
