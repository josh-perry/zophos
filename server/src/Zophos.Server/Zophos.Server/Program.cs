using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zophos.Data.Db;
using Zophos.Data.Repositories;
using Zophos.Data.Repositories.Contracts;
using Zophos.Server;
using Zophos.Server.Services;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = CreateHostBuilder(args);
        await builder.RunConsoleAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json");
                config.AddEnvironmentVariables();

                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            })
            .ConfigureServices((context, services) =>
            {
                services.AddOptions();
                services.AddDbContext<GameServerDbContext>(options =>
                {
                    options.UseSqlite(context.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("No connection string set!"));
                });
                
                services.AddTransient<IHostedService, Server>();

                services.AddTransient<IPlayerRepository, PlayerRepository>();
                
                services.AddTransient<IPlayerRegistrationService, PlayerRegistrationService>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                logging.AddConsole();
            });
    }
}