using Gthx.Bot;
using NUnit.Framework;
using System;
using System.IO;
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
using NUnit.Framework.Internal;
using Serilog;

namespace Gthx.Test
{
    public class UtilTestStartup
    {
        private readonly IConfiguration _config;

        public UtilTestStartup(IConfiguration config)
        {
            this._config = config;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddConsole()).AddTransient<UtilTests>();
            services.AddSingleton<IWebReader, MockWebReader>();
            services.AddSingleton<IGthxUtil, GthxUtil>();
        }
    }

    /// <summary>
    /// Tests for the methods in GthxUtil.cs
    /// </summary>
    [TestFixture]
    public class UtilTests
    {
        private readonly IGthxUtil _util;
        private readonly IConfigurationRoot _config;
        private readonly TestServer _server;

        public UtilTests()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(_config)
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                Log.Information("Serilog enabled for GthxUtilTests");
                _server = new TestServer(new WebHostBuilder().UseConfiguration(_config).UseStartup<UtilTestStartup>().UseSerilog());
                _util = _server.Host.Services.GetService<IGthxUtil>();
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

        /// <summary>
        /// Test time string formatting
        /// </summary>
        [Test]
        public void TestTimeSinceString()
        {
            // Verify that 0 seconds works and includes an 's' at the end
            var timeNow = DateTime.UtcNow;
            var sinceString = _util.TimeBetweenString(timeNow, timeNow);
            Assert.AreEqual("0 seconds", sinceString);

            // Verify that 1 second does not include the 's'
            var futureTime = timeNow + new TimeSpan(0, 0, 1);
            sinceString = _util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("1 second", sinceString);

            // And that 2 seconds and beyond include the 's' again
            futureTime = timeNow + new TimeSpan(0, 0, 2);
            sinceString = _util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("2 seconds", sinceString);

            // Test minutes
            futureTime = timeNow + new TimeSpan(0, 1, 0);
            sinceString = _util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("1 minute", sinceString);

            futureTime = timeNow + new TimeSpan(0, 2, 0);
            sinceString = _util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("2 minutes", sinceString);

            futureTime = timeNow + new TimeSpan(0, 2, 15);
            sinceString = _util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("2 minutes, 15 seconds", sinceString);

            // Test hours
            futureTime = timeNow + new TimeSpan(1, 0, 0);
            sinceString = _util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("1 hour", sinceString);

            futureTime = timeNow + new TimeSpan(2, 0, 0);
            sinceString = _util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("2 hours", sinceString);

            futureTime = timeNow + new TimeSpan(3, 16, 41);
            sinceString = _util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("3 hours, 16 minutes, 41 seconds", sinceString);

            // Test days
            futureTime = timeNow + new TimeSpan(1, 0, 0, 0);
            sinceString = _util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("1 day", sinceString);

            futureTime = timeNow + new TimeSpan(2, 0, 0, 0);
            sinceString = _util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("2 days", sinceString);

            futureTime = timeNow + new TimeSpan(3, 11, 58, 5);
            sinceString = _util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("3 days, 11 hours, 58 minutes, 5 seconds", sinceString);

            // Test years
            futureTime = timeNow + new TimeSpan(365, 0, 0, 0);
            sinceString = _util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("1 year", sinceString);

            futureTime = timeNow + new TimeSpan(730, 0, 0, 0);
            sinceString = _util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("2 years", sinceString);

            futureTime = timeNow + new TimeSpan(1095, 8, 0, 0);
            sinceString = _util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("3 years, 8 hours", sinceString);
        }
    }
}
