using Microsoft.Data.SqlClient;

namespace ThuongMaiDienTu.Tests;

internal static class SqlServerTestConfiguration
{
    internal const string EnvironmentVariableName = "THUONGMAIDIENTU_TEST_CONNECTION";

    // Local Development fallback only. It intentionally uses Windows integrated
    // authentication and contains no username, password, API key, or other secret.
    internal const string LocalDevelopmentFallback =
        // Development-only fallback: this local SQL Server does not expose a TLS
        // certificate, so encryption is explicitly disabled for integration tests.
        "Server=localhost;Database=thuongmaidientu;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True";

    internal static SqlServerTestConnection GetConnection()
    {
        var environmentValue = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        var fromEnvironment = !string.IsNullOrWhiteSpace(environmentValue);
        var connectionString = fromEnvironment
            ? environmentValue!
            : LocalDevelopmentFallback;

        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(
                builder.InitialCatalog,
                "thuongmaidientu",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{EnvironmentVariableName} must target the thuongmaidientu database.");
        }

        return new SqlServerTestConnection(
            connectionString,
            fromEnvironment
                ? SqlServerTestConnectionSource.Environment
                : SqlServerTestConnectionSource.LocalDevelopmentFallback);
    }
}

internal sealed record SqlServerTestConnection(
    string ConnectionString,
    SqlServerTestConnectionSource Source);

internal enum SqlServerTestConnectionSource
{
    Environment,
    LocalDevelopmentFallback
}
