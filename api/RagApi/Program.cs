using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RagApi.Data;
using RagApi.Options;
using RagApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RagServiceOptions>(
    builder.Configuration.GetSection(RagServiceOptions.SectionName));

builder.Services.AddHttpClient<IRagClient, RagClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<RagServiceOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Chaîne de connexion 'Default' manquante");

builder.Services.AddDbContext<RagDbContext>(options =>
{
    if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IHistoryService, HistoryService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<RagDbContext>().Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();

public partial class Program
{
}