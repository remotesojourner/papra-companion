using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Papra.Companion.Tests;

public class TestAppFactory : WebApplicationFactory<Program>
{
    private readonly string _contentRoot = Directory.CreateTempSubdirectory("papra-companion-tests-").FullName;

    public string ContentRoot => _contentRoot;

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseContentRoot(_contentRoot);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
