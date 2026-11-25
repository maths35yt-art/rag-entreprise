using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RagApi.Data;
using RagApi.Services;

namespace RagApi.Tests.Support;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public ApiFactory()
    {
        _connection.Open();
    }

    public FakeRagClient Rag { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<RagDbContext>>();
            services.RemoveAll<RagDbContext>();
            services.AddDbContext<RagDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<IRagClient>();
            services.AddSingleton<IRagClient>(Rag);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }

        base.Dispose(disposing);
    }
}