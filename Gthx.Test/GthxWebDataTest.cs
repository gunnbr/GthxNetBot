using Gthx.Core;
using Gthx.Data;
using GthxData;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Gthx.Test
{
    // Tests for the website's read-only queries, against SQL Server so LIKE and collation behave as in production.
    [TestFixture]
    public class GthxWebDataTest
    {
        private GthxDataContext _Db;
        private GthxWebData _Data;

        [OneTimeSetUp]
        public async Task TestInitialise()
        {
            var containerConnString = await SqlServerTestContainerSetUp.GetConnectionStringAsync();
            var sqlBuilder = new SqlConnectionStringBuilder(containerConnString)
            {
                InitialCatalog = "GthxWebDataTests"
            };

            var options = new DbContextOptionsBuilder<GthxDataContext>()
                .UseSqlServer(sqlBuilder.ConnectionString)
                .Options;
            _Db = new GthxDataContext(options);
            await _Db.Database.EnsureDeletedAsync();
            await _Db.Database.EnsureCreatedAsync();

            AddFactoid("reprap", "a self-replicating 3D printer");
            AddFactoid("reprap", "also a community");
            AddFactoid("RepRap Wiki", "https://reprap.org/wiki");
            AddFactoid("pla", "a plastic that's easy to print, like a reprap part");
            AddFactoid("100%", "infill all the way");
            AddFactoid("snake_case", "words_with_underscores");
            AddFactoid("[brackets]", "square ones");
            AddFactoid("unreferenced", "never asked about");

            AddRef("pla", 50);
            AddRef("REPRAP", 20);   // Case differs from the factoid's item
            AddRef("100%", 20);
            AddRef("forgotten", 99); // No factoid any more
            AddRef("botsnack", 500); // Not a factoid at all

            await _Db.SaveChangesAsync();
            _Data = new GthxWebData(_Db);
        }

        [OneTimeTearDown]
        public async Task TestTearDown()
        {
            if (_Db != null)
            {
                await _Db.Database.EnsureDeletedAsync();
                await _Db.DisposeAsync();
            }
        }

        private void AddFactoid(string item, string value)
        {
            _Db.Factoid.Add(new Factoid() { Item = item, Value = value, User = "tester", Timestamp = DateTime.UtcNow });
        }

        private void AddRef(string item, int count)
        {
            _Db.Ref.Add(new Ref() { Item = item, Count = count, Timestamp = DateTime.UtcNow });
        }

        [Test]
        public async Task Search_MatchesItemOrValue_CaseInsensitive_ExactItemFirst()
        {
            var results = await _Data.SearchFactoidsAsync("RePrAp", 50);

            CollectionAssert.AreEqual(
                new[] { "reprap", "reprap", "pla", "RepRap Wiki" },
                results.Select(f => f.Item).ToArray());
        }

        [Test]
        public async Task Search_TreatsWildcardsLiterally()
        {
            var percent = await _Data.SearchFactoidsAsync("%", 50);
            CollectionAssert.AreEqual(new[] { "100%" }, percent.Select(f => f.Item).ToArray());

            var underscore = await _Data.SearchFactoidsAsync("_", 50);
            CollectionAssert.AreEqual(new[] { "snake_case" }, underscore.Select(f => f.Item).ToArray());

            var bracket = await _Data.SearchFactoidsAsync("[b", 50);
            CollectionAssert.AreEqual(new[] { "[brackets]" }, bracket.Select(f => f.Item).ToArray());
        }

        [Test]
        public async Task Search_TrimsAndLimits()
        {
            var results = await _Data.SearchFactoidsAsync("  reprap  ", 2);

            ClassicAssert.AreEqual(2, results.Count);
            ClassicAssert.IsTrue(results.All(f => f.Item == "reprap"));
        }

        [Test]
        public async Task Search_BlankReturnsNothing()
        {
            ClassicAssert.IsEmpty(await _Data.SearchFactoidsAsync("   ", 50));
            ClassicAssert.IsEmpty(await _Data.SearchFactoidsAsync(null, 50));
        }

        [Test]
        public async Task TopFactoids_SkipsRefsWithoutFactoids_AndGroupsValues()
        {
            var top = await _Data.GetTopFactoidsAsync(10);

            CollectionAssert.AreEqual(new[] { "pla", "100%", "REPRAP" }, top.Select(t => t.Item).ToArray());
            CollectionAssert.AreEqual(new[] { 50, 20, 20 }, top.Select(t => t.Count).ToArray());
            CollectionAssert.AreEqual(
                new[] { "a self-replicating 3D printer", "also a community" },
                top[2].Factoids.Select(f => f.Value).ToArray());
        }

        [Test]
        public async Task TopFactoids_Limits()
        {
            var top = await _Data.GetTopFactoidsAsync(1);

            ClassicAssert.AreEqual(1, top.Count);
            ClassicAssert.AreEqual("pla", top[0].Item);
        }

        [Test]
        public async Task FactoidCount_CountsDistinctItems()
        {
            ClassicAssert.AreEqual(7, await _Data.GetFactoidCountAsync());
        }

        [Test]
        public void RejectsNonPositiveLimits()
        {
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _Data.SearchFactoidsAsync("x", 0));
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _Data.GetTopFactoidsAsync(0));
        }
    }
}
