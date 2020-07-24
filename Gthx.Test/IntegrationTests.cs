﻿using Gthx.Core;
using Gthx.Test.Mocks;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gthx.Test
{
    [TestFixture]
    public class IntegrationTests
    {
        // TODO: Add integration test for "seen user*" to verify the asterisk is handled correctly


        [Test]
        public async Task TestLiveYoutubeReferences()
        {
            var client = new MockIrcClient();
            var data = new MockData();
            var webReader = new WebReader();
            var gthx = new Core.Gthx(client, data, webReader);

            var testChannel = "#reprap";
            var testUser = "BobYourUncle";
            gthx.HandleReceivedMessage(testChannel, testUser, $"OMG! Check this out! https://www.youtube.com/watch?v=I7nVrT00ST4");
            await Task.Delay(5000);
            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual("I7nVrT00ST4", data.AddedYoutubeId);
            Assert.AreEqual("Pro Riders Laughing", data.AddedYoutubeTitle);
            Assert.AreEqual($"{testUser} linked to YouTube video \"Pro Riders Laughing\" => 1 IRC mentions", replies.Messages[0]);

            // Test non-Western characters
            testUser = "AndrewJohnson";
            gthx.HandleReceivedMessage(testChannel, testUser, $"Calm down and listen to this: https://www.youtube.com/watch?v=xtAHgrNs7r4");
            await Task.Delay(5000);
            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual("xtAHgrNs7r4", data.AddedYoutubeId);
            Assert.AreEqual("Studio Ghibli - Music Collection (Piano and Violin Duo) 株式会社スタジオジブリ- Relaxing music song", data.AddedYoutubeTitle);
            Assert.AreEqual($"{testUser} linked to YouTube video \"Studio Ghibli - Music Collection (Piano and Violin Duo) 株式会社スタジオジブリ- Relaxing music song\" => 1 IRC mentions", replies.Messages[0]);
        }

        [Test]
        public async Task TestLiveThingiverseReferences()
        {
            var client = new MockIrcClient();
            var data = new MockData();
            var mockReader = new WebReader();
            var gthx = new Core.Gthx(client, data, mockReader);

            // Test fetching a new title that uses the <title> element
            var testChannel = "#reprap";
            var testUser = "RandomNick";
            gthx.HandleReceivedMessage(testChannel, testUser, $"Your daughter would really like this: https://www.thingiverse.com/thing:2810756");
            await Task.Delay(5000);
            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual("2810756", data.AddedThingiverseId);
            Assert.AreEqual("Articulated Butterfly by 8ran", data.AddedThingiverseTitle);
            Assert.AreEqual($"{testUser} linked to \"Articulated Butterfly by 8ran\" on thingiverse => 1 IRC mentions", replies.Messages[0]);
        }
    }
}
