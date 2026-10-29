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
    // Tests for the website's admin changes, against SQL Server so collation and ExecuteUpdate behave as in production.
    [TestFixture]
    public class GthxAdminDataTest
    {
        private const string LimitedUser = "gthx_admin_test";
        private const string LimitedPassword = "Limited_password123";

        private DbContextOptions<GthxDataContext> _Options;
        private DbContextOptions<GthxDataContext> _LimitedOptions;
        private GthxDataContext _Db;
        private GthxAdminData _Data;

        [OneTimeSetUp]
        public async Task TestInitialise()
        {
            var containerConnString = await SqlServerTestContainerSetUp.GetConnectionStringAsync();
            var sqlBuilder = new SqlConnectionStringBuilder(containerConnString)
            {
                InitialCatalog = "GthxAdminDataTests"
            };

            _Options = new DbContextOptionsBuilder<GthxDataContext>()
                .UseSqlServer(sqlBuilder.ConnectionString)
                .Options;
            await using var db = new GthxDataContext(_Options);
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            // A login with only the permissions the website's admin identity gets (GthxWebsite's infra/README.md).
            await db.Database.ExecuteSqlRawAsync($"""
                IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '{LimitedUser}')
                    EXEC('CREATE LOGIN {LimitedUser} WITH PASSWORD = ''{LimitedPassword}''');
                CREATE USER {LimitedUser} FOR LOGIN {LimitedUser};
                GRANT SELECT, UPDATE, DELETE ON dbo.Factoid TO {LimitedUser};
                GRANT SELECT, INSERT ON dbo.FactoidHistory TO {LimitedUser};
                """);
            _LimitedOptions = new DbContextOptionsBuilder<GthxDataContext>()
                .UseSqlServer(new SqlConnectionStringBuilder(sqlBuilder.ConnectionString) { UserID = LimitedUser, Password = LimitedPassword }.ConnectionString)
                .Options;
        }

        [OneTimeTearDown]
        public async Task TestTearDown()
        {
            if (_Options != null)
            {
                await using var db = new GthxDataContext(_Options);
                await db.Database.EnsureDeletedAsync();
            }
        }

        // Every test changes data, so each one starts from the same factoids.
        [SetUp]
        public async Task SetUp()
        {
            await using (var db = new GthxDataContext(_Options))
            {
                await db.FactoidHistory.ExecuteDeleteAsync();
                await db.Factoid.ExecuteDeleteAsync();

                AddFactoid(db, "reprap", "a self-replicating 3D printer");
                AddFactoid(db, "RepRap", "also a community");
                AddFactoid(db, "pla", "a plastic", isLocked: true);
                AddFactoid(db, "abs", "another plastic");
                AddFactoid(db, "this is a whole sentence that someone typed", "oops");
                AddFactoid(db, "a slightly shorter accidental sentence", "oops too");
                await db.SaveChangesAsync();
            }

            _Db = new GthxDataContext(_Options);
            _Data = new GthxAdminData(_Db);
        }

        [TearDown]
        public async Task TearDown()
        {
            await _Db.DisposeAsync();
        }

        private static void AddFactoid(GthxDataContext db, string item, string value, bool isLocked = false)
        {
            db.Factoid.Add(new Factoid() { Item = item, Value = value, User = "tester", Timestamp = new DateTime(2020, 1, 1), IsLocked = isLocked });
        }

        // A separate context, so checks see what's in the database rather than what _Db is tracking.
        private GthxDataContext Fresh() => new GthxDataContext(_Options);

        private async Task<int> IdOf(string value)
        {
            await using var db = Fresh();
            return await db.Factoid.Where(f => f.Value == value).Select(f => f.Id).SingleAsync();
        }

        [Test]
        public async Task LongFactoids_LongestFirst_AndLimits()
        {
            var all = await _Data.GetLongFactoidsAsync(30, 50);
            CollectionAssert.AreEqual(
                new[] { "this is a whole sentence that someone typed", "a slightly shorter accidental sentence" },
                all.Select(f => f.Item).ToArray());

            var one = await _Data.GetLongFactoidsAsync(30, 1);
            ClassicAssert.AreEqual(1, one.Count);

            ClassicAssert.IsEmpty(await _Data.GetLongFactoidsAsync(100, 50));
        }

        [Test]
        public async Task Update_ChangesLockedValue_AndRecordsHistory()
        {
            var id = await IdOf("a plastic");

            var updated = await _Data.UpdateFactoidValueAsync(id, "polylactic acid", "admin");

            ClassicAssert.AreEqual("polylactic acid", updated.Value);
            await using var db = Fresh();
            var factoid = await db.Factoid.SingleAsync(f => f.Id == id);
            ClassicAssert.AreEqual("polylactic acid", factoid.Value);
            ClassicAssert.AreEqual("admin", factoid.User);
            ClassicAssert.IsTrue(factoid.IsLocked);
            var history = await db.FactoidHistory.SingleAsync();
            ClassicAssert.AreEqual(("pla", "polylactic acid", "admin"), (history.Item, history.Value, history.User));
        }

        [Test]
        public async Task Update_MissingIdReturnsNull()
        {
            ClassicAssert.IsNull(await _Data.UpdateFactoidValueAsync(-1, "x", "admin"));

            await using var db = Fresh();
            ClassicAssert.IsEmpty(await db.FactoidHistory.ToListAsync());
        }

        [Test]
        public async Task Delete_OneOfSeveralValues_DoesNotRecordForget()
        {
            var id = await IdOf("also a community");

            ClassicAssert.AreEqual(1, await _Data.DeleteFactoidsAsync(new[] { id }, "admin"));

            await using var db = Fresh();
            CollectionAssert.AreEqual(new[] { "a self-replicating 3D printer" },
                await db.Factoid.Where(f => f.Item == "reprap").Select(f => f.Value).ToArrayAsync());
            ClassicAssert.IsEmpty(await db.FactoidHistory.ToListAsync());
        }

        [Test]
        public async Task Delete_LastValues_RecordsForgetOncePerItem()
        {
            await using var before = Fresh();
            var ids = await before.Factoid
                .Where(f => f.Item == "reprap" || f.Item == "pla")
                .Select(f => f.Id)
                .ToListAsync();

            // A missing id is skipped.
            ClassicAssert.AreEqual(3, await _Data.DeleteFactoidsAsync(ids.Append(-1).ToList(), "admin"));

            await using var db = Fresh();
            CollectionAssert.IsEmpty(await db.Factoid.Where(f => f.Item == "reprap" || f.Item == "pla").ToListAsync());
            var history = await db.FactoidHistory.OrderBy(h => h.Item).ToListAsync();
            CollectionAssert.AreEqual(new[] { "pla", "reprap" }, history.Select(h => h.Item.ToLowerInvariant()).ToArray());
            ClassicAssert.IsTrue(history.All(h => h.Value == null && h.User == "admin"));
        }

        [Test]
        public async Task Delete_NothingToDelete()
        {
            ClassicAssert.AreEqual(0, await _Data.DeleteFactoidsAsync(Array.Empty<int>(), "admin"));
            ClassicAssert.AreEqual(0, await _Data.DeleteFactoidsAsync(new[] { -1 }, "admin"));
        }

        [Test]
        public async Task SetLocked_ChangesEveryValue_CaseInsensitive()
        {
            ClassicAssert.AreEqual(2, await _Data.SetItemLockedAsync("REPRAP", true));
            // Already locked, so nothing changes.
            ClassicAssert.AreEqual(0, await _Data.SetItemLockedAsync("reprap", true));

            await using (var db = Fresh())
            {
                ClassicAssert.IsTrue(await db.Factoid.Where(f => f.Item == "reprap").AllAsync(f => f.IsLocked));
            }

            ClassicAssert.AreEqual(1, await _Data.SetItemLockedAsync("pla", false));
            await using (var db = Fresh())
            {
                ClassicAssert.IsFalse(await db.Factoid.Where(f => f.Item == "pla").AnyAsync(f => f.IsLocked));
            }
        }

        [Test]
        public async Task WorksWithOnlyTheAdminPermissions()
        {
            await using var db = new GthxDataContext(_LimitedOptions);
            var data = new GthxAdminData(db);

            var id = await IdOf("a plastic");
            ClassicAssert.AreEqual(2, (await data.GetLongFactoidsAsync(30, 50)).Count);
            ClassicAssert.IsNotNull(await data.UpdateFactoidValueAsync(id, "polylactic acid", "admin"));
            ClassicAssert.AreEqual(1, await data.SetItemLockedAsync("pla", false));
            ClassicAssert.AreEqual(1, await data.DeleteFactoidsAsync(new[] { id }, "admin"));

            // And it can't touch anything else.
            Assert.ThrowsAsync<SqlException>(() => db.Ref.ToListAsync());
        }

        [Test]
        public void RejectsBadArguments()
        {
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _Data.GetLongFactoidsAsync(30, 0));
            Assert.ThrowsAsync<ArgumentException>(() => _Data.UpdateFactoidValueAsync(1, " ", "admin"));
            Assert.ThrowsAsync<ArgumentException>(() => _Data.SetItemLockedAsync("", true));
        }
    }
}
