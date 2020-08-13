using Gthx.Bot.Interfaces;
using IrcDotNet;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GthxNetBot
{
    public class GthxIrcClient : IIrcClient
    {
        private readonly IGthxMessageConduit _gthx;
        private readonly IBotNick _botNick;
        private readonly ILogger<IrcClient> _logger;
        private StandardIrcClient _client;

        public GthxIrcClient(IGthxMessageConduit sender, IBotNick botNick, ILogger<IrcClient> logger) 
        {
            _gthx = sender;
            _botNick = botNick;
            _logger = logger;

            Console.WriteLine($"GThxIrcClient: Starting!");
            _logger.LogInformation("Starting GthxIrcClient");
            Start();
        }

        private void Start()
        {
            var info = new IrcUserRegistrationInfo()
            {
                NickName = _botNick.BotNick,
                UserName = _botNick.BotNick,
                RealName = "GthxNetBot"
            };
            var server = "irc.freenode.net";
            Connect(server, info);
        }

        private void Connect(string server, IrcRegistrationInfo registrationInfo)
        {
            // Create new IRC client and connect to given server.
            var client = new StandardIrcClient
            {
                FloodPreventer = new IrcStandardFloodPreventer(4, 2000)
            };

            client.Connected += IrcClient_Connected;
            client.Disconnected += IrcClient_Disconnected;
            client.Registered += IrcClient_Registered;

            // Wait until connection has succeeded or timed out.
            using (var connectedEvent = new ManualResetEventSlim(false))
            {
                _logger.LogInformation("Connecting to {server}", server);
                client.Connected += (sender2, e2) => connectedEvent.Set();
                client.Connect(server, false, registrationInfo);
                if (!connectedEvent.Wait(10000))
                {
                    client.Dispose();
                    _logger.LogError("Connection to '{server}' timed out.", server);
                    return;
                }
            }

            // Save the client now that we're connected
            _client = client;
            _logger.LogInformation("Now connected to '{server}'.", server);
        }

        private void IrcClient_Registered(object? sender, EventArgs e)
        {
            _logger.LogInformation($"Registered with the IRC server.");

            var client = sender as IrcClient;
            if (client == null)
            {
                _logger.LogError("Registered event didn't get a client!");
                return;
            }

            //client.LocalUser.NoticeReceived += IrcClient_LocalUser_NoticeReceived;
            client.LocalUser.MessageReceived += IrcClient_LocalUser_MessageReceived;
            client.LocalUser.JoinedChannel += IrcClient_LocalUser_JoinedChannel;
            //client.LocalUser.LeftChannel += IrcClient_LocalUser_LeftChannel;

            client.Channels.Join("#gthxtest");
        }
        private void IrcClient_LocalUser_JoinedChannel(object sender, IrcChannelEventArgs e)
        {
            Console.WriteLine($"{sender}: Joined Channel: {e.Channel}:{e.Comment}");
            e.Channel.MessageReceived += Channel_MessageReceived;
        }

        private void Channel_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            var wasHandled = HandleCTCP(e.Source.Name, e.Text);
            if (wasHandled)
            {
                return;
            }

            foreach (var target in e.Targets)
            {
                _logger.LogDebug("Received message: {Source} -> {Target}: {Message}", e.Source, target, e.Text);
                _gthx.ReceiveMessage(target.Name, e.Source.Name, e.Text);
            }
        }

        private bool HandleCTCP(string fromUser, string message)
        {
            if (!message.StartsWith('\u0001') || !message.EndsWith('\u0001'))
            {
                return false;
            }

            var ctcp = message.Trim('\u0001');
            _logger.LogInformation("Received CTCP request: {command}", ctcp);

            // TODO: Fix handling of CTCP messages

            if (ctcp.StartsWith("PING "))
            {
                //_client.LocalUser.SendMessage(fromUser, ctcp);
            }
            else if (ctcp.StartsWith("VERSION"))
            {
                //_client.LocalUser.SendMessage(fromUser, $"GNB {GthxBot.Version}");
            }

            return true;
        }

        private void IrcClient_LocalUser_MessageReceived(object? sender, IrcMessageEventArgs e)
        {
#if false // Looks like everything we get here also comes in a channel message, so we don't need to handle here
            var targets = e.Targets.Select(t => t.Name);
            var targetStrings = string.Join(",", targets);

            _logger.LogDebug("Received DM: {Source} -> {Target}: {Message}", e.Source, targetStrings, e.Text);
            _gthx.ReceiveMessage(e.Source.Name, e.Source.Name, e.Text);
#endif
        }

        private void IrcClient_Disconnected(object? sender, EventArgs e)
        {
            _logger.LogWarning("Disconnected from the IRC server");
        }

        private void IrcClient_Connected(object? sender, EventArgs e)
        {
            _logger.LogInformation("Connected to IRC server!");

            var client = sender as IrcClient;
            if (client == null)
            {
                _logger.LogError("Connected with no client!");
                return;
            }

            client.Error += Client_Error;
            client.ErrorMessageReceived += Client_ErrorMessageReceived;
            client.MotdReceived += Client_MotdReceived;
            client.RawMessageReceived += SimpleBot_RawMessageReceived;
            client.ProtocolError += SimpleBot_ProtocolError;
            client.LocalUser.MessageReceived += LocalUser_MessageReceived;

            _logger.LogDebug("Waiting for something to happen...");
        }

        /// <summary>
        /// Handles DMs from other users
        /// </summary>
        /// <param name="sender">IrcLocalUser from whom the message was received</param>
        /// <param name="e">IrcMessageEventArgs</param>
        private void LocalUser_MessageReceived(object? sender, IrcMessageEventArgs e)
        {
            var wasHandled = HandleCTCP(e.Source.Name, e.Text);
            if (wasHandled)
            {
                return;
            }

            var fromUser = sender as IrcLocalUser;
            if (fromUser == null)
            {
                _logger.LogError("LocalUser_MessageReceived: Invalid cast of sender '{sender}", sender);
                return;
            } 

            _logger.LogInformation("LocalUser MessageReceived from {user}: {Message}", e.Source.Name, e.Text);

            _gthx.ReceiveMessage(e.Source.Name, e.Source.Name, e.Text);
        }

        private void SimpleBot_ProtocolError(object? sender, IrcProtocolErrorEventArgs e)
        {
            _logger.LogError("Protocol Error {code}: {message} ({parameters})", e.Code, e.Message, e.Parameters);
        }

        private void SimpleBot_RawMessageReceived(object? sender, IrcRawMessageEventArgs e)
        {
            _logger.LogDebug("Raw Message Received: {RawContent} {message}", e.RawContent, e.Message);
        }

        private void Client_MotdReceived(object? sender, EventArgs e)
        {
            _logger.LogInformation("MOTD received: {message}", _client.MessageOfTheDay);
        }

        private void Client_ErrorMessageReceived(object? sender, IrcErrorMessageEventArgs e)
        {
            _logger.LogWarning("Error message received: {message}", e.Message);
        }

        private void Client_Error(object? sender, IrcErrorEventArgs e)
        {
            _logger.LogError(e.Error, "IRC error!");
        }

        public bool SendAction(string channel, string action)
        {
            _client.LocalUser.SendNotice(channel, action);
            return true;
        }

        public Task<List<string>> GetUsersInChannelAsync(string channel)
        {
            return Task.Run(() =>
            {
                var users = new List<string>
                {
                    "gunnbr",
                    _botNick.BotNick,
                    "LurkerBot"
                };
                return users;
            });
        }

        /// <summary>
        /// Sends a message from gthxbot to the IRC server
        /// </summary>
        /// <param name="channel">Name of the channel/nick to send the message to</param>
        /// <param name="message">The message to send</param>
        /// <returns>True if the message was sent successfully, false otherwise</returns>
        public bool SendMessage(string channel, string message)
        {
            _client.LocalUser.SendMessage(channel, message);

            var toUser = _client.Users.FirstOrDefault(u => u.NickName == channel);
            if (toUser != null)
            {
                _logger.LogDebug("Sending message to {channel}: {message}", channel, message);
            }

            return true;
        }
    }
}
