using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillBot.Core.Interfaces;
using SkillBot.Infrastructure.Data;

namespace SkillBot.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    // Keep one open connection per factory – SQLite in-memory databases live
    // only as long as their connection stays open.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestWebApplicationFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the production SQLite DbContextOptions registration
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<SkillBotDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Register an in-memory SQLite database via the same provider –
            // this avoids the "two providers" error that InMemory + Sqlite triggers.
            services.AddDbContext<SkillBotDbContext>(options =>
                options.UseSqlite(_connection));

            // Replace real IAgentEngine with a mock so chat tests don't need a live LLM
            var engineDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IAgentEngine));
            if (engineDescriptor != null) services.Remove(engineDescriptor);
            services.AddSingleton<IAgentEngine, MockAgentEngine>();
        });

        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
