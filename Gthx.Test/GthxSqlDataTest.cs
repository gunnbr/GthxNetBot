using Gthx.Data;
using GthxData;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Linq;

namespace Gthx.Test
{
    [TestFixture]
    public class GthxSqlDataTest
    {
        private GthxSqlData _Data;
        private GthxDataContext _Db;

        [SetUp]
        public void Init()
        {
            var connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=GthxSqlData.UnitTest;Integrated Security=True;";
            var optionsBuilder = new DbContextOptionsBuilder<GthxDataContext>();
            optionsBuilder.UseSqlServer(connectionString);
//            _Db = new GthxDataContext(optionsBuilder.Options);
            _Db.Database.EnsureCreated();
            _Data = new GthxSqlData(_Db);
        }

        [TearDown]
        public void Cleanup()
        {
            _Db.Ref.RemoveRange(_Db.Ref.Where(f => true));
            _Db.Factoid.RemoveRange(_Db.Factoid.Where(f => true));
            _Db.FactoidHistory.RemoveRange(_Db.FactoidHistory.Where(f => true));
            _Db.SaveChanges();

            _Data = null;
        }

        [Test]
        public void GthxData_TestRefCount()
        {
            var testFactoid = "newFactoid";
            var testUser = "user1";
            var testValue = "a new factoid";

            // Verify initial conditions:
            // 1) No Ref for the factoid exists
            var refData = _Db.Ref.FirstOrDefault(r => r.Item == testFactoid);
            Assert.IsNull(refData, "Test factoid already in the Ref table");

            var factoidData = _Data.GetFactoid(testFactoid);
            // 2) That the factoid doesn't exist and that
            //    if none exists, the return value is null
            Assert.IsNull(factoidData, "Test factoid already exists");

            // Verify that referencing a factoid that doesn't exist doesn't
            // add a reference for it
            refData = _Db.Ref.FirstOrDefault(r => r.Item == testFactoid);
            Assert.IsNull(refData, "Asking about an unknown factoid added a reference");

            var success = _Data.AddFactoid(testUser, testFactoid, false, testValue, true);

            factoidData = _Data.GetFactoid(testFactoid);
            refData = _Db.Ref.FirstOrDefault(r => r.Item == testFactoid);
            Assert.NotNull(refData, "After asking about a factoid, it still doesn't exist in the Ref table");
            Assert.AreEqual(1, refData.Count, "Incorrect ref count after first ask");

            factoidData = _Data.GetFactoid(testFactoid);
            factoidData = _Data.GetFactoid(testFactoid);
            refData = _Db.Ref.FirstOrDefault(r => r.Item == testFactoid);
            Assert.AreEqual(3, refData.Count, "Incorrect ref count after third ask");
        }

        [Test]
        public void GthxData_TestFactoidHistory()
        {
            // Verify the factoid history and refcount get properly updated
            var testFactoid = "history";
            var testUser1 = "historyUser";
            var testUser2 = "historyPerson";
            var testUser3 = "historyTroll";
            var testValue1 = "a good thing to test";
            var testValue2 = "about the past";
            var testValue3 = "boring";

            var history = _Data.GetFactoidInfo(testFactoid);
            Assert.IsNull(history);

            _Data.AddFactoid(testUser1, testFactoid, false, testValue1, true);
            _Data.AddFactoid(testUser2, testFactoid, false, testValue2, false);
            _Data.AddFactoid(testUser3, testFactoid, false, testValue3, true);
            _Data.GetFactoid(testFactoid);
            _Data.GetFactoid(testFactoid);
            _Data.GetFactoid(testFactoid);

            history = _Data.GetFactoidInfo(testFactoid);
            Assert.AreEqual(3, history.RefCount);

            Assert.AreEqual(testUser3, history.InfoList[0].User);
            Assert.AreEqual(testFactoid, history.InfoList[0].Item);
            Assert.AreEqual(testValue3, history.InfoList[0].Value);

            Assert.AreEqual(testUser3, history.InfoList[1].User);
            Assert.AreEqual(testFactoid, history.InfoList[1].Item);
            Assert.IsNull(history.InfoList[1].Value);

            Assert.AreEqual(testUser2, history.InfoList[2].User);
            Assert.AreEqual(testFactoid, history.InfoList[2].Item);
            Assert.AreEqual(testValue2, history.InfoList[2].Value);

            Assert.AreEqual(testUser1, history.InfoList[3].User);
            Assert.AreEqual(testFactoid, history.InfoList[3].Item);
            Assert.AreEqual(testValue1, history.InfoList[3].Value);
        }

        [Test]
        public void GthxData_TestTell()
        {
            // Verify that GetTell() also clears the returned tells

            var toUser = "tellUser";
            var fromUser = "fromUser";
            var message = "Be sure to test tells";

            var tells = _Db.Tell.Where(t => t.Recipient == toUser);
            Assert.AreEqual(0, tells.Count(), "Tell exists at the start of the test");

            var tellData = _Data.GetTell(toUser);
            Assert.AreEqual(0, tellData.Count(), "Tell Data exists at the start of the test");

            _Data.AddTell(fromUser, toUser, message);

            tells = _Db.Tell.Where(t => t.Recipient == toUser);
            Assert.AreEqual(1, tells.Count(), "Tell not added to the DB");

            tellData = _Data.GetTell(toUser);
            Assert.AreEqual(1, tellData.Count(), "Tell not returned when it should be");
            Assert.AreEqual(toUser, tellData[0].Recipient);
            Assert.AreEqual(fromUser, tellData[0].Author);
            Assert.AreEqual(message, tellData[0].Message);

            tells = _Db.Tell.Where(t => t.Recipient == toUser);
            Assert.AreEqual(0, tells.Count(), "Tell still exists after being returned");

            tellData = _Data.GetTell(toUser);
            Assert.AreEqual(0, tellData.Count(), "Tell Data still exists after being returned");
        }
    }
}
