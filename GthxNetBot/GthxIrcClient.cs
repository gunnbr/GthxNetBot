using Gthx.Bot.Interfaces;
using IrcDotNet;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GthxNetBot
{
    public class IrcOptions
    {
        public const string IrcInfo = "IrcInfo";

        public string? Server { get; set; }
        public string? Channels { get; set; }
        public string? Nick { get; set; }
        public string? Password { get; set; }
        public string? RealName { get; set; }
    }

    public class GthxIrcClient : IIrcClient
    {
        private readonly IGthxMessageConduit _gthx;
        private readonly IBotNick _botNick;
        private readonly ILogger<IrcClient> _logger;
        private readonly IrcOptions _options = new IrcOptions();

        private readonly StandardIrcClient _client = new StandardIrcClient
        {
            FloodPreventer = new IrcStandardFloodPreventer(4, 2000)
        };

        public GthxIrcClient(IGthxMessageConduit sender, IBotNick botNick, ILogger<IrcClient> logger, IConfiguration config) 
        {
            _gthx = sender;
            _botNick = botNick;
            _logger = logger;

            config.GetSection(IrcOptions.IrcInfo).Bind(_options);
            if (string.IsNullOrWhiteSpace(_options.Server) ||
                string.IsNullOrWhiteSpace(_options.Channels) ||
                string.IsNullOrWhiteSpace(_options.Nick) ||
                string.IsNullOrWhiteSpace(_options.RealName))
            {
                _logger.LogWarning("Missing required IRC parameters!");
                return;
            }

            _logger.LogInformation("Starting GthxIrcClient");
            Start();
        }

        private void Start()
        {
            var info = new IrcUserRegistrationInfo()
            {
                NickName = _options.Nick,
                UserName = _options.Nick,
                RealName = _options.RealName,
                Password = _options.Password
            };

            _botNick.BotNick = _options.Nick!;

            var server = _options.Server;
            if (server == null)
            {
                _logger.LogError("No server specified!");
                return;
            }
            Connect(server, info);
        }

        private void Connect(string server, IrcRegistrationInfo registrationInfo)
        {
            // Create new IRC client and connect to given server.
            

            _client.Connected += IrcClient_Connected;
            _client.Disconnected += IrcClient_Disconnected;
            _client.Registered += IrcClient_Registered;

            // Wait until connection has succeeded or timed out.
            using (var connectedEvent = new ManualResetEventSlim(false))
            {
                _logger.LogInformation("Connecting to {server}", server);
                _client.Connected += (sender2, e2) => connectedEvent.Set();
                _client.Connect(server, false, registrationInfo);
                if (!connectedEvent.Wait(10000))
                {
                    _client.Dispose();
                    _logger.LogError("Connection to '{server}' timed out.", server);
                    return;
                }
            }

            _logger.LogInformation("Now connected to '{server}'.", server);
        }

        private void IrcClient_Registered(object? sender, EventArgs e)
        {
            _logger.LogInformation($"Registered with the IRC server.");

            if (!(sender is IrcClient client))
            {
                _logger.LogError("Registered event didn't get a client!");
                return;
            }

            //client.LocalUser.NoticeReceived += IrcClient_LocalUser_NoticeReceived;
            client.LocalUser.MessageReceived += IrcClient_LocalUser_MessageReceived;
            client.LocalUser.JoinedChannel += IrcClient_LocalUser_JoinedChannel;
            client.LocalUser.NickNameChanged += NickNameChanged;
            //client.LocalUser.LeftChannel += IrcClient_LocalUser_LeftChannel;

            var channels = _options.Channels!.Split(',');
            foreach (var channel in channels)
            {
                _logger.LogInformation("Joining {channel}", channel);
                client.Channels.Join(channel);
            }
        }

        private void NickNameChanged(object? sender, EventArgs e)
        {
            _logger.LogWarning("{sender}: NickName changed to {args}", sender, e);
        }

        private void IrcClient_LocalUser_JoinedChannel(object? sender, IrcChannelEventArgs e)
        {
            _logger.LogInformation("{sender}: Joined Channel: {Channel}:{Comment}", sender, e.Channel, e.Comment);
            e.Channel.MessageReceived += Channel_MessageReceived;
        }

        private void Channel_MessageReceived(object? sender, IrcMessageEventArgs e)
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
            _logger.LogInformation("Received CTCP request from {user}: {command}", fromUser, ctcp);

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

            if (!(sender is IrcClient client))
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
