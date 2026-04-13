using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Lunera.API.Data;
using Lunera.API.Tests.Fixtures;

namespace Lunera.Tests.E2E
{
    public class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public SqlServerFixture DbFixture { get; }

        public ApiWebApplicationFactory()
        {
            DbFixture = new SqlServerFixture();
        }

        public async Task InitializeAsync()
        {
            await DbFixture.InitializeAsync();
        }

        public new async Task DisposeAsync()
        {
            await DbFixture.DisposeAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlServer(DbFixture.ConnectionString);
                });

                services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });

                services.Configure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                });
            });
        }
    }
}