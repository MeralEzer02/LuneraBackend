using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using TheSocialMediaV2.API.Data;
using Xunit;

namespace TheSocialMediaV2.API.Tests.Fixtures
{
    public class SqlServerFixture : IAsyncLifetime
    {
        private readonly MsSqlContainer _dbContainer;

        public SqlServerFixture()
        {
            _dbContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("Strong_Password_123!")
                .Build();
        }

        public string ConnectionString => _dbContainer.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();

            using var context = CreateContext();
            await context.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            await _dbContainer.DisposeAsync();
        }

        public AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            return new AppDbContext(options);
        }
    }
}