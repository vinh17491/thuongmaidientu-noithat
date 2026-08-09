using Xunit;

namespace ThuongMaiDienTu.Tests;

[CollectionDefinition(SqlServerConfigurationCollection.Name, DisableParallelization = true)]
public sealed class SqlServerConfigurationCollection
{
    public const string Name = "SQL Server test configuration";
}

[Collection(SqlServerConfigurationCollection.Name)]
public sealed class SqlServerTestConfigurationTests
{
    [Fact]
    public void GetConnection_PrefersEnvironmentVariable()
    {
        const string configured =
            "Server=integration-host;Database=thuongmaidientu;Trusted_Connection=True;TrustServerCertificate=True";

        WithTemporaryEnvironmentValue(configured, () =>
        {
            var result = SqlServerTestConfiguration.GetConnection();

            Assert.Equal(configured, result.ConnectionString);
            Assert.Equal(SqlServerTestConnectionSource.Environment, result.Source);
        });
    }

    [Fact]
    public void GetConnection_UsesLocalDevelopmentFallbackWhenEnvironmentIsMissing()
    {
        WithTemporaryEnvironmentValue(null, () =>
        {
            var result = SqlServerTestConfiguration.GetConnection();

            Assert.Equal(
                SqlServerTestConfiguration.LocalDevelopmentFallback,
                result.ConnectionString);
            Assert.Equal(
                SqlServerTestConnectionSource.LocalDevelopmentFallback,
                result.Source);
        });
    }

    private static void WithTemporaryEnvironmentValue(string? value, Action assertion)
    {
        var original = Environment.GetEnvironmentVariable(
            SqlServerTestConfiguration.EnvironmentVariableName);

        try
        {
            Environment.SetEnvironmentVariable(
                SqlServerTestConfiguration.EnvironmentVariableName,
                value);
            assertion();
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                SqlServerTestConfiguration.EnvironmentVariableName,
                original);
        }
    }
}
