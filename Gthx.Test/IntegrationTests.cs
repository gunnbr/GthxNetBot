using Gthx.Bot;
using Gthx.Bot.Interfaces;
using Gthx.Data;
using Gthx.Test.Mocks;
using GthxData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gthx.Test;

public class IntegrationTestsStartup
{
    private readonly IConfiguration _config;

    public IntegrationTestsStartup(IConfiguration config)
    {
        _config = config;
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(configure => configure.AddConsole().AddSerilog()).AddTransient<GthxBot>();

        var useMariaDb = false;
        var dbType = _config.GetConnectionString("GthxDb_Type");
        if (dbType == "mariadb")
        {
            Log.Information("Using MariaDB mode");
        }
        services.AddDbContext<GthxDataContext>(options =>
        {
            if (useMariaDb)
            {
                options.UseMySql(_config.GetConnectionString("GthxDb"),
                    new MariaDbServerVersion(new Version(10, 3, 29)), x => x.MigrationsAssembly("MariaDbMigrations.Migrations"));
            }
            else
            {
                options.UseSqlServer(_config.GetConnectionString("GthxDb"), x => x.MigrationsAssembly("SqlServerMigrations.Migrations"));
            }
        }, ServiceLifetime.Transient);

        services.TryAddSingleton<IGthxData, GthxSqlData>();
        services.TryAddSingleton<IWebReader, WebReader>();
        services.TryAddSingleton<IGthxUtil, GthxUtil>();
        services.TryAddSingleton<MockIrcClient>();
        services.TryAddSingleton<IIrcClient>(sp => sp.GetRequiredService<MockIrcClient>());
        services.TryAddSingleton<IBotNick>(sp => sp.GetRequiredService<MockIrcClient>());
        services.TryAddSingleton<GthxMessageConduit>();
        services.TryAddSingleton<IGthxMessageConduit>(s => s.GetRequiredService<GthxMessageConduit>());
        services.TryAddSingleton<IGthxMessageConsumer>(s => s.GetRequiredService<GthxMessageConduit>());
        services.AddGthxBot();
        services.TryAddSingleton<GthxBot>();
        services.TryAddSingleton(_config);
    }
}

[TestFixture]
public class IntegrationTests
{
    private readonly TestServer _server;
    private readonly GthxDataContext _Db;
    private readonly GthxBot _gthx;
    private readonly MockIrcClient _client;
    private readonly IBotNick _botNick;
    private readonly GthxSqlData _data;
    private readonly IConfigurationRoot _config;

    public IntegrationTests()
    {
        _config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(_config)
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            Log.Information("Serilog enabled for IntegrationTests");
            _server = new TestServer(new WebHostBuilder()
                .UseConfiguration(_config)
                .UseStartup<IntegrationTestsStartup>()
                .UseSerilog());
            _Db = _server.Host.Services.GetRequiredService<GthxDataContext>();
            _data = _server.Host.Services.GetService<IGthxData>() as GthxSqlData;
            _client = _server.Host.Services.GetService<IIrcClient>() as MockIrcClient;
            _botNick = _server.Host.Services.GetService<IBotNick>();
            _gthx = _server.Host.Services.GetRequiredService<GthxBot>();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "TestHost terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    [OneTimeSetUp]
    public void TestInitialise()
    {
        _Db.Database.EnsureCreated();
    }

    [OneTimeTearDown]
    public void TestTearDown()
    {
        _Db.Database.EnsureDeleted();
    }

    [Test]
    public async Task TestLiveYoutubeReferences()
    {
        var testChannel = "#reprap";
        var testUser = "BobYourUncle";
        _gthx.HandleReceivedMessage(testChannel, testUser, $"OMG! Check this out! https://www.youtube.com/watch?v=I7nVrT00ST4");
        await Task.Delay(5000);
        var replies = _client.GetReplies();
        Assert.AreEqual(1, replies.Messages.Count);
        Assert.AreEqual(testChannel, replies.Channel);
        Assert.AreEqual($"{testUser} linked to YouTube video \"Pro Riders Laughing\" => 1 IRC mentions", replies.Messages[0]);

        // Reference the same video a second time to verify that the count increases
        testUser = "LeroyJenkins";
        _gthx.HandleReceivedMessage(testChannel, testUser, $"I love this video too! https://www.youtube.com/watch?v=I7nVrT00ST4");
        await Task.Delay(5000);
        replies = _client.GetReplies();
        Assert.AreEqual(1, replies.Messages.Count);
        Assert.AreEqual(testChannel, replies.Channel);
        Assert.AreEqual($"{testUser} linked to YouTube video \"Pro Riders Laughing\" => 2 IRC mentions", replies.Messages[0]);

        // Test non-Western characters
        testUser = "AndrewJohnson";
        _gthx.HandleReceivedMessage(testChannel, testUser, $"Are you hungry? https://www.youtube.com/watch?v=RE9gtTLZ5Ic");
        await Task.Delay(5000);
        replies = _client.GetReplies();
        Assert.AreEqual(1, replies.Messages.Count);
        Assert.AreEqual(testChannel, replies.Channel);
        Assert.AreEqual($"{testUser} linked to YouTube video \"BEST Japanese Fried Rice Recipe (焼き飯 - Yakimeshi)\" => 1 IRC mentions", replies.Messages[0]);

        // Test 2 messages in a row that query the DB to see if there are concurrency issues
        testUser = "gunnbr";
        _gthx.HandleReceivedMessage(testChannel, testUser, $"Check out my latest song! https://www.youtube.com/watch?v=qFoNGyFrjl4");
        _gthx.HandleReceivedMessage(testChannel, testUser, $"crash?");
        await Task.Delay(5000);
        replies = _client.GetReplies();
        Assert.AreEqual(1, replies.Messages.Count);
        Assert.AreEqual(testChannel, replies.Channel);
        Assert.AreEqual($"{testUser} linked to YouTube video \"2022 Weekly Beats Week 2: Forest Adventure\" => 1 IRC mentions", replies.Messages[0]);
    }

    [Test]
    public async Task TestLiveThingiverseReferences()
    {
        // Test fetching a new title that uses the <title> element
        var testChannel = "#reprap";
        var testUser = "RandomNick";
        _gthx.HandleReceivedMessage(testChannel, testUser, $"Your daughter would really like this: https://www.thingiverse.com/thing:2810756");
        await Task.Delay(5000);
        var replies = _client.GetReplies();
        Assert.AreEqual(1, replies.Messages.Count);
        Assert.AreEqual(testChannel, replies.Channel);
        Assert.AreEqual($"{testUser} linked to \"Articulated Butterfly by 8ran\" on thingiverse => 1 IRC mentions", replies.Messages[0]);

        // Test the same reference a second time to verify the count increases
        _gthx.HandleReceivedMessage(testChannel, testUser, $"Did you hear what I said? I love this thing! https://www.thingiverse.com/thing:2810756");
        await Task.Delay(5000);
        replies = _client.GetReplies();
        Assert.AreEqual(1, replies.Messages.Count);
        Assert.AreEqual(testChannel, replies.Channel);
        Assert.AreEqual($"{testUser} linked to \"Articulated Butterfly by 8ran\" on thingiverse => 2 IRC mentions", replies.Messages[0]);
    }

    /// <summary>
    /// Verify that the command to get a factoid returns it after
    /// the factoid is set.
    /// </summary>
    [Test]
    public void TestFactoidSetAndGet()
    {
        var testFactoid = "testFactoid";
        var testValue = "working";
        var testChannel = "#reprap";
        var testUser = "SomeUser";

        _gthx.HandleReceivedMessage(testChannel, testUser, $"{_botNick.BotNick}: {testFactoid} is {testValue}");
        var replies = _client.GetReplies();
        Assert.AreEqual(1, replies.Messages.Count);
        Assert.AreEqual(testChannel, replies.Channel);
        Assert.AreEqual($"{testUser}: Okay.", replies.Messages[0]);

        _gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}?");
        replies = _client.GetReplies();
        Assert.AreEqual(1, replies.Messages.Count);
        Assert.AreEqual(testChannel, replies.Channel);
        Assert.AreEqual($"{testFactoid} is {testValue}", replies.Messages[0]);
    }

    // TODO: Add integration test for "seen user*" to verify the asterisk is handled correctly
    // TODO: Add test that changes the BotNick to verify that GthxBot correctly detects "wasDirectlyAddressed"
    //       using the new name.
}
