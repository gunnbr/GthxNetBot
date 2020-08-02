using Gthx.Bot.Interfaces;
using Gthx.Data;
using Gthx.Test;
using Gthx.Test.Mocks;
using GthxData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

public class Startup
{
    private readonly IConfiguration config;

    public Startup(IConfiguration config)
    {
        this.config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {

    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<GthxDataContext>(
            options => options.UseSqlServer(this.config.GetConnectionString("GthxDb")),
            ServiceLifetime.Singleton);
        services.AddSingleton<IIrcClient, MockIrcClient>();
        services.AddSingleton<IGthxData, MockData>();
        services.AddSingleton<IWebReader, MockWebReader>();
        services.AddSingleton<GthxDataContext>();
        Gthx.Bot.Gthx.RegisterServices(services as ServiceCollection);
        var sc = services as ServiceCollection;
        sc.AddLogging(configure => configure.AddConsole()).AddTransient<GthxTests>();
        services.AddSingleton(config);
        services.AddSingleton<Gthx.Bot.Gthx>();
        //services.AddScoped<IGthxData>(provider => (IGthxData)provider.GetService<GthxDataContext>());
    }
}
