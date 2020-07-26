using Gthx.Bot;
using NUnit.Framework;
using System;

namespace Gthx.Test
{
    /// <summary>
    /// Tests for the methods in Util.cs
    /// </summary>
    [TestFixture]
    public class UtilTests
    {
        /// <summary>
        /// Test time string formatting
        /// </summary>
        [Test]
        public void TestTimeSinceString()
        {
            // Verify that 0 seconds works and includes an 's' at the end
            var timeNow = DateTime.UtcNow;
            var sinceString = Util.TimeBetweenString(timeNow, timeNow);
            Assert.AreEqual("0 seconds", sinceString);

            // Verify that 1 second does not include the 's'
            var futureTime = timeNow + new TimeSpan(0, 0, 1);
            sinceString = Util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("1 second", sinceString);

            // And that 2 seconds and beyond include the 's' again
            futureTime = timeNow + new TimeSpan(0, 0, 2);
            sinceString = Util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("2 seconds", sinceString);

            // Test minutes
            futureTime = timeNow + new TimeSpan(0, 1, 0);
            sinceString = Util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("1 minute", sinceString);

            futureTime = timeNow + new TimeSpan(0, 2, 0);
            sinceString = Util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("2 minutes", sinceString);

            futureTime = timeNow + new TimeSpan(0, 2, 15);
            sinceString = Util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("2 minutes, 15 seconds", sinceString);

            // Test hours
            futureTime = timeNow + new TimeSpan(1, 0, 0);
            sinceString = Util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("1 hour", sinceString);

            futureTime = timeNow + new TimeSpan(2, 0, 0);
            sinceString = Util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("2 hours", sinceString);

            futureTime = timeNow + new TimeSpan(3, 16, 41);
            sinceString = Util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("3 hours, 16 minutes, 41 seconds", sinceString);

            // Test days
            futureTime = timeNow + new TimeSpan(1, 0, 0, 0);
            sinceString = Util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("1 day", sinceString);

            futureTime = timeNow + new TimeSpan(2, 0, 0, 0);
            sinceString = Util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("2 days", sinceString);

            futureTime = timeNow + new TimeSpan(3, 11, 58, 5);
            sinceString = Util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("3 days, 11 hours, 58 minutes, 5 seconds", sinceString);

            // Test years
            futureTime = timeNow + new TimeSpan(365, 0, 0, 0);
            sinceString = Util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("1 year", sinceString);

            futureTime = timeNow + new TimeSpan(730, 0, 0, 0);
            sinceString = Util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("2 years", sinceString);

            futureTime = timeNow + new TimeSpan(1095, 8, 0, 0);
            sinceString = Util.TimeBetweenString(timeNow, futureTime);
            Assert.AreEqual("3 years, 8 hours", sinceString);
        }
    }
}
