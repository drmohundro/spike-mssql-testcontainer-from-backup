using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace TestContainerSample.Tests;

public class MsSqlTestContainerTests : IAsyncLifetime
{
    private const string Password = "Password1234!";

    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilderWithBackup()
        .WithPassword(Password)
        .Build();

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
    }

    public Task DisposeAsync() => _sqlContainer.DisposeAsync().AsTask();

    [Fact]
    public async Task TestDatabaseConnection()
    {
        var connectionString = _sqlContainer.GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand("USE SampleDatabase; SELECT COUNT(*) FROM dbo.Stuff", connection);
        var result = await command.ExecuteScalarAsync();

        Assert.NotNull(result);
        Assert.IsType<int>(result);
    }
}
