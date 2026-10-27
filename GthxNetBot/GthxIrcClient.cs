using Gthx.Bot;
using Gthx.Bot.Interfaces;
using IrcDotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace GthxNetBot
{
    public class IrcOptions
    {
        public const string IrcInfo = "IrcInfo";

        public string? Server { get; set; }
        public int? Port { get; set; }
        public bool? UseSsl { get; set; }
        public string? Channels { get; set; }
        public string? Nick { get; set; }
        public string? Password { get; set; }
        public string? RealName { get; set; }
        public string? SaslPassword { get; set; }

        /// <summary>
        /// A CA certificate to trust in addition to the system's CAs, for a test server whose certificate
        /// comes from a private CA: either the path to a PEM or DER file, or the PEM text itself.
        /// Leave unset for public networks.
        /// </summary>
        public string? TrustedCaCertificate { get; set; }

        // How long to wait before exiting after losing the connection, by cause. Every exit is followed
        // by a container restart and a reconnect, so these keep failures from turning into a rapid
        // reconnect loop, which can get the host banned.
        public double? BannedExitDelayMinutes { get; set; }
        public double? SaslFailureExitDelayMinutes { get; set; }
        public double? EarlyFailureExitDelayMinutes { get; set; }
        public double? DisconnectExitDelayMinutes { get; set; }

        /// <summary>
        /// How long the bot must have been registered for a disconnect to count as normal (e.g. a
        /// netsplit or server restart) rather than an early failure.
        /// </summary>
        public double? StableConnectionMinutes { get; set; }
    }

    public class GthxIrcClient : IIrcClient
    {
        private readonly IGthxMessageConduit _gthx;
        private readonly IBotNick _botNick;
        private readonly ILogger<IrcClient> _logger;
        private readonly IBotRunner _runner;
        private readonly IrcOptions _options = new();
        private readonly System.Timers.Timer _whoIsTimer;
        private readonly DateTime _startupTime = DateTime.UtcNow;
        private static readonly Regex _banMessageRegex = new(@"\b([kgzd]-?lined?|banned)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private volatile bool _saslFailed;
        private volatile bool _banned;
        private DateTime? _registeredAt;
        private int _exitRequested;
        private volatile bool _quitting;
        private readonly PosixSignalRegistration _sigtermRegistration;

        private readonly StandardIrcClient _client = new()
        {
            FloodPreventer = new IrcStandardFloodPreventer(4, 2000)
        };

        public DateTime StartupTime => _startupTime;

        public GthxIrcClient(IGthxMessageConduit sender, IBotNick botNick, ILogger<IrcClient> logger, IConfiguration config, IBotRunner runner) 
        {
            _gthx = sender;
            _botNick = botNick;
            _logger = logger;
            _runner = runner;

            // 2 minute timer
            _whoIsTimer = new System.Timers.Timer(2.0 * 60.0 * 1000.0);
            _whoIsTimer.AutoReset = false;
            _whoIsTimer.Elapsed += HandleWhoIsTimer;

            config.GetSection(IrcOptions.IrcInfo).Bind(_options);
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(_options.Server)) missing.Add("Server");
            if (string.IsNullOrWhiteSpace(_options.Channels)) missing.Add("Channels");
            if (string.IsNullOrWhiteSpace(_options.Nick)) missing.Add("Nick");
            if (string.IsNullOrWhiteSpace(_options.RealName)) missing.Add("RealName");
            if (missing.Count > 0)
            {
                // Fail at startup instead of running without ever connecting, which would leave a
                // container that looks healthy but does nothing.
                var names = string.Join(", ", missing.ConvertAll(name => $"{IrcOptions.IrcInfo}:{name}"));
                _logger.LogError("Missing required IRC parameters: {Names}", names);
                throw new InvalidOperationException($"Missing required IRC parameters: {names}");
            }

            _logger.LogError($"GthxNetBot {GthxBot.Version} starting for bot '{_options.Nick}'...");

            // Container Apps (and docker stop) sends SIGTERM when replacing or stopping the bot.
            _sigtermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, HandleSigterm);

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

            // Enable SASL authentication when a password is available. Libera requires SASL
            // for connections originating from SASL-only IP ranges. The SASL username is always
            // the IRC nickname so the connection and the SASL login use a single identity.
            var saslUsername = _options.Nick;
            var saslPassword = !string.IsNullOrWhiteSpace(_options.SaslPassword) ? _options.SaslPassword : _options.Password;
            if (!string.IsNullOrWhiteSpace(saslUsername) && !string.IsNullOrWhiteSpace(saslPassword))
            {
                info.UseSasl = true;
                info.SaslUsername = saslUsername;
                info.SaslPassword = saslPassword;

                _logger.LogInformation("SASL authentication is enabled for nickname '{Nick}'.", saslUsername);

                if (_options.UseSsl == false)
                {
                    _logger.LogWarning("SASL authentication is enabled but TLS (UseSsl) is disabled. Credentials will be sent over an unencrypted connection.");
                }
            }
            else
            {
                _logger.LogWarning("SASL authentication is being skipped because no password was found. Set 'IrcInfo:SaslPassword' (or 'IrcInfo:Password') via user secrets or environment variables to enable SASL login for nickname '{Nick}'.", saslUsername);
            }

            _botNick.BotNick = _options.Nick!;

            var server = _options.Server;
            if (server == null)
            {
                _logger.LogError("No server specified!");
                return;
            }
            Connect(server, _options.Port ?? 6697, info);
        }

        private void Connect(string server, int port, IrcRegistrationInfo registrationInfo)
        {
            _client.Connected += IrcClient_Connected;
            _client.Disconnected += IrcClient_Disconnected;
            _client.Registered += IrcClient_Registered;

            // Diagnostic logging around SASL capability negotiation. These fire during
            // registration (before Connected/Registered), so they must be wired up here.
            _client.ServerCapabilitiesReceived += Client_ServerCapabilitiesReceived;
            _client.ActiveCapabilitiesReceived += Client_ActiveCapabilitiesReceived;
            _client.CapabilityAcknowledged += Client_CapabilityAcknowledged;
            _client.SaslAuthenticationSucceeded += Client_SaslAuthenticationSucceeded;
            _client.SaslAuthenticationFailed += Client_SaslAuthenticationFailed;

            // IrcDotNet accepts any server certificate unless this handler says otherwise.
            _client.ValidateSslCertificate += Client_ValidateSslCertificate;

            using (var connectedEvent = new ManualResetEventSlim(false))
            {
                _logger.LogInformation("Connecting to {server}:{port}", server, port);
                _client.Connected += (sender2, e2) => connectedEvent.Set();

                // Unblock the wait immediately on a failed connect (e.g. TLS handshake failure)
                // instead of waiting for the full timeout, and log the underlying exception.
                var connectFailed = false;
                void ConnectFailedHandler(object? sender2, IrcErrorEventArgs e2)
                {
                    connectFailed = true;
                    LogConnectException("Connection to the IRC server failed", e2.Error);
                    connectedEvent.Set();
                }
                _client.ConnectFailed += ConnectFailedHandler;

                // Connect to given server, enabling TLS unless explicitly disabled via config.
                var useSsl = _options.UseSsl ?? true;
                _client.Connect(server, port, useSsl, registrationInfo);

                // Wait until connection has succeeded, failed, or timed out.
                if (!connectedEvent.Wait(10000))
                {
                    _client.ConnectFailed -= ConnectFailedHandler;
                    _client.Dispose();
                    _logger.LogError("Connection to '{server}' timed out.", server);
                    ExitBot();
                    return;
                }

                _client.ConnectFailed -= ConnectFailedHandler;

                // After a failed TLS handshake the socket is still open, so IsConnected alone isn't enough.
                if (connectFailed || !_client.IsConnected)
                {
                    _client.Dispose();
                    _logger.LogError("Connection to '{server}' did not succeed.", server);
                    ExitBot();
                    return;
                }
            }

            _logger.LogInformation("Now connected to '{server}'.", server);
        }

        private void Client_ValidateSslCertificate(object? sender, IrcValidateSslCertificateEventArgs e)
        {
            e.IsValid = IsServerCertificateValid(e.Certificate, e.SslPolicyErrors);
        }

        private bool IsServerCertificateValid(X509Certificate certificate, SslPolicyErrors errors)
        {
            if (errors == SslPolicyErrors.None)
            {
                return true;
            }

            // A chain error alone may only mean the certificate comes from the configured private CA.
            if (errors == SslPolicyErrors.RemoteCertificateChainErrors && !string.IsNullOrWhiteSpace(_options.TrustedCaCertificate))
            {
                try
                {
                    using var trustedCa = LoadTrustedCa(_options.TrustedCaCertificate);
                    using var serverCertificate = new X509Certificate2(certificate);
                    using var chain = new X509Chain();
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                    chain.ChainPolicy.CustomTrustStore.Add(trustedCa);
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    if (chain.Build(serverCertificate))
                    {
                        return true;
                    }

                    var status = string.Join(", ", chain.ChainStatus.Select(s => s.StatusInformation.Trim()));
                    _logger.LogError("The server's TLS certificate '{subject}' doesn't chain to the trusted CA {ca}: {status}",
                        certificate.Subject, DescribeTrustedCa(_options.TrustedCaCertificate), status);
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Couldn't check the server's TLS certificate against the trusted CA {ca}", DescribeTrustedCa(_options.TrustedCaCertificate));
                    return false;
                }
            }

            _logger.LogError("Rejecting the server's TLS certificate '{subject}': {errors}", certificate.Subject, errors);
            return false;
        }

        private static bool IsPemText(string trustedCa) => trustedCa.TrimStart().StartsWith("-----BEGIN");

        private static X509Certificate2 LoadTrustedCa(string trustedCa)
        {
            return IsPemText(trustedCa)
                ? X509Certificate2.CreateFromPem(trustedCa)
                : X509CertificateLoader.LoadCertificateFromFile(trustedCa);
        }

        private static string DescribeTrustedCa(string trustedCa)
        {
            return IsPemText(trustedCa) ? "(PEM from settings)" : $"in {trustedCa}";
        }

        private void LogConnectException(string context, Exception? error)
        {
            if (error == null)
            {
                _logger.LogError("{context}: no exception details available.", context);
                return;
            }

            // Walk the inner-exception chain so the root cause (e.g. a TLS
            // AuthenticationException or its underlying Win32/socket error) is visible.
            var inner = error.InnerException;
            while (inner != null)
            {
                _logger.LogError("{context}: inner exception {type}: {message}", context, inner.GetType().FullName, inner.Message);
                inner = inner.InnerException;
            }

            _logger.LogError(error, "{context}: {type}: {message}", context, error.GetType().FullName, error.Message);
        }

        private void Client_ServerCapabilitiesReceived(object? sender, EventArgs e)
        {
            var capabilities = sender is IrcClient client
                ? string.Join(", ", client.ServerCapabilities)
                : "(unknown)";
            _logger.LogInformation("SASL/CAP: Server advertised capabilities: {capabilities}", capabilities);
        }

        private void Client_ActiveCapabilitiesReceived(object? sender, ActiveCapabilitiesEventArgs e)
        {
            _logger.LogInformation("SASL/CAP: Active capabilities: {capabilities}", string.Join(", ", e.Capabilities));
        }

        private void Client_CapabilityAcknowledged(object? sender, CapabilityAcknowledgedEventArgs e)
        {
            var capabilities = string.Join(", ", e.Capabilities);
            if (e.Acknowledged)
            {
                _logger.LogInformation("SASL/CAP: Server ACKed capability request: {capabilities}", capabilities);
            }
            else
            {
                _logger.LogWarning("SASL/CAP: Server NAKed capability request: {capabilities}", capabilities);
            }
        }

        private void Client_SaslAuthenticationSucceeded(object? sender, EventArgs e)
        {
            _logger.LogInformation("SASL authentication succeeded.");
        }

        private void Client_SaslAuthenticationFailed(object? sender, SaslAuthenticationFailedEventArgs e)
        {
            _logger.LogError("SASL authentication failed using mechanism {mechanism}: {message}", e.Mechanism, e.Message);

            // Don't carry on unidentified. Some servers (e.g. Libera for SASL-only IP ranges) drop the
            // connection anyway, and the container restart policy would then reconnect within seconds,
            // over and over, which can get the host banned. Disconnect and let ExitBot() delay the exit.
            _saslFailed = true;
            Task.Run(() => _client.Disconnect());
        }

        /// <summary>
        /// Stops the bot after a delay chosen by why the connection ended. The container restart policy
        /// reconnects as soon as the bot exits, so exiting right away after a failure would turn it into
        /// a rapid reconnect loop.
        /// </summary>
        private void ExitBot()
        {
            if (Interlocked.Exchange(ref _exitRequested, 1) == 1)
            {
                return;
            }

            var (delayMinutes, reason) = GetExitDelay();
            var delay = TimeSpan.FromMinutes(delayMinutes);
            _logger.LogError("Exiting in {delay} because {reason}.", delay, reason);
            Task.Delay(delay).ContinueWith(_ => _runner.Exit());
        }

        /// <summary>
        /// Quits IRC and exits right away instead of being killed, so the server frees our nickname at once.
        /// During a deploy the new copy is already connected under a fallback nick, and it takes the
        /// nickname back as soon as it sees this QUIT.
        /// </summary>
        private void HandleSigterm(PosixSignalContext context)
        {
            // Exit through Main so the host is disposed and the logs are flushed.
            context.Cancel = true;

            // Exit now, not after ExitBot()'s reconnect delay, when the QUIT closes the connection.
            if (Interlocked.Exchange(ref _exitRequested, 1) == 1)
            {
                _runner.Exit();
                return;
            }

            _logger.LogWarning("Received SIGTERM. Quitting IRC and exiting.");
            _quitting = true;
            if (_client.IsConnected)
            {
                using var disconnected = new ManualResetEventSlim(false);
                _client.Disconnected += (_, _) => disconnected.Set();
                try
                {
                    _client.Quit("Restarting");
                    // The server closes the connection after the QUIT, normally well within a second.
                    if (!disconnected.Wait(TimeSpan.FromSeconds(5)))
                    {
                        _logger.LogWarning("The server didn't close the connection after QUIT; exiting anyway.");
                    }
                }
                catch (ObjectDisposedException)
                {
                    // The server closed the connection after the QUIT before Quit() finished disconnecting
                    // its socket. That's the outcome we want.
                    _logger.LogDebug("The connection closed while quitting.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Couldn't quit IRC cleanly");
                }
            }

            _runner.Exit();
        }

        private (double DelayMinutes, string Reason) GetExitDelay()
        {
            if (_banned)
            {
                return (_options.BannedExitDelayMinutes ?? 60, "the server reported that we are banned");
            }

            if (_saslFailed)
            {
                return (_options.SaslFailureExitDelayMinutes ?? 15, "SASL authentication failed (check 'IrcInfo:SaslPassword')");
            }

            var stable = TimeSpan.FromMinutes(_options.StableConnectionMinutes ?? 5);
            if (_registeredAt == null || DateTime.UtcNow - _registeredAt < stable)
            {
                return (_options.EarlyFailureExitDelayMinutes ?? 5, "the connection failed or ended soon after connecting");
            }

            return (_options.DisconnectExitDelayMinutes ?? 0.5, "the connection ended after running normally");
        }

        private void IrcClient_Registered(object? sender, EventArgs e)
        {
            _logger.LogInformation($"Registered with the IRC server.");
            _registeredAt = DateTime.UtcNow;

            if (sender is not IrcClient client)
            {
                _logger.LogError("Registered event didn't get a client!");
                return;
            }

            if (_saslFailed)
            {
                _logger.LogWarning("Not joining channels because SASL authentication failed.");
                return;
            }

            if (_botNick.BotNick != _options.Nick)
            {
                _client.WhoIsReplyReceived += Client_WhoIsReplyReceived;
                _client.QueryWhoIs(new List<string?>{_options.Nick});
            }

            //client.LocalUser.NoticeReceived += IrcClient_LocalUser_NoticeReceived;
            client.LocalUser.MessageReceived += IrcClient_LocalUser_MessageReceived;
            client.LocalUser.JoinedChannel += IrcClient_LocalUser_JoinedChannel;
            client.LocalUser.NickNameChanged += NickNameChanged;
            client.LocalUser.Quit += IrcClient_LocalUser_OnQuit;
            client.LocalUser.JoinedChannel += LocalUser_JoinedChannel;
            client.LocalUser.LeftChannel += IrcClient_LocalUser_LeftChannel;

            var channels = _options.Channels!.Split(',');
            foreach (var channel in channels)
            {
                _logger.LogInformation("Joining {channel}", channel);
                client.Channels.Join(channel);
            }
        }

        private void Client_WhoIsReplyReceived(object? sender, IrcUserEventArgs e)
        {
            if (e.User.HostName == null)
            {
                // Nobody has the nickname; the 401 reply before this one already took it back
                return;
            }

            _logger.LogInformation($"WhoIs response for {e.User.NickName}: {e.User.UserName}@{e.User.HostName} ({e.User.RealName})");

            // Check again later, unless we already have our nickname back
            _whoIsTimer.Enabled = _client.LocalUser.NickName != _options.Nick;
        }

        private void HandleWhoIsTimer(object? sender, ElapsedEventArgs e)
        {
            if (_client.LocalUser.NickName == _options.Nick)
            {
                return;
            }

            _logger.LogDebug("WhoIs timer expired. Querying again...");
            _client.QueryWhoIs(new List<string?> { _options.Nick });
        }

        private void LocalUser_JoinedChannel(object? sender, IrcChannelEventArgs e)
        {
            _logger.LogInformation("Joined channel {channel}", e.Channel.Name);
            e.Channel.UserJoined += Channel_UserJoined;
            e.Channel.UserLeft += Channel_UserLeft;
        }

        private void Channel_UserLeft(object? sender, IrcChannelUserEventArgs e)
        {
            _logger.LogInformation("{nickname} left {channel}: {comment}", e.ChannelUser.User.NickName, e.ChannelUser.Channel.Name, e.Comment);
        }

        private void Channel_UserJoined(object? sender, IrcChannelUserEventArgs e)
        {
            _logger.LogInformation("{nickname} joined {channel}: {comment}", e.ChannelUser.User.NickName, e.ChannelUser.Channel.Name, e.Comment);
        }

        private void IrcClient_LocalUser_OnQuit(object? sender, IrcCommentEventArgs e)
        {
            _logger.LogInformation($"User quit: {e.Comment}");
        }

        private void IrcClient_LocalUser_LeftChannel(object? sender, IrcChannelEventArgs e)
        {
            _logger.LogDebug("User left channel: {nickname}", e.Channel.Name);
        }

        private void NickNameChanged(object? sender, EventArgs e)
        {
            // TODO: Figure out if and when this gets called. At the moment, it isn't getting called even
            //       when I KNOW the nickname has changed.
            _logger.LogWarning("{sender}: NickName changed to {newNickName}", sender, _client.LocalUser.NickName);
            _botNick.BotNick = _client.LocalUser.NickName;
        }

        private void IrcClient_LocalUser_JoinedChannel(object? sender, IrcChannelEventArgs e)
        {
            _logger.LogInformation("{sender}: Joined Channel: {Channel}:{Comment}", sender, e.Channel, e.Comment);
            e.Channel.MessageReceived += Channel_MessageReceived;
        }

        private void Channel_MessageReceived(object? sender, IrcMessageEventArgs e)
        {
            // TODO: Switch to using CtcpClient instead of just IrcClient so it automatically handles such things
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
                var parameter = ctcp[5..];
                // TODO: Is this right? Should be PONG?
                _client.LocalUser.SendNotice(fromUser, $"\u0001PING {parameter}\u0001");
            }
            else if (ctcp.StartsWith("VERSION"))
            {
                _client.LocalUser.SendNotice(fromUser, $"\u0001VERSION Gthx Bot Version {GthxBot.Version}\u0001");
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
            if (_quitting)
            {
                _logger.LogInformation("Disconnected from the IRC server after quitting");
                return;
            }

            _logger.LogError("Disconnected from the IRC server");
            ExitBot();
        }

        private void IrcClient_Connected(object? sender, EventArgs e)
        {
            _logger.LogInformation("Connected to IRC server!");

            if (sender is not IrcClient client)
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

            if (e.Code == (int)IrcProtocolErrorEnum.YoureBanned)
            {
                _banned = true;
            }

            if (e.Code == (int)IrcProtocolErrorEnum.NickInUse)
            {
                if (_botNick.BotNick.EndsWith("__"))
                {
                    _logger.LogError("Already tried 2 renames. Giving up.");
                    _client.Disconnect();
                }
                else
                {
                    var newNick = _botNick.BotNick + "_";
                    _logger.LogError($"Nickname {_botNick.BotNick} in use. Trying {newNick}");
                    _client.LocalUser.SetNickName(newNick);
                    // TODO: Doesn't seem like we should have to call this. I expect
                    // that the NickChangedHandler will get called, but maybe that doesn't
                    // work for our local user.
                    _botNick.BotNick = newNick;
                }
            }
            else if (e.Code == (int)IrcProtocolErrorEnum.NoSuchNick &&
                     _botNick.BotNick != _options.Nick &&
                     e.Parameters.Contains(_options.Nick))
            {
                // The WHOIS check found nobody using our preferred nickname. IrcDotNet still raises
                // WhoIsReplyReceived afterwards with the details it cached earlier, so take it back here.
                _logger.LogWarning("Nobody is using our preferred nickname {nick}.", _options.Nick);
                HandlePreferredNameAvailable();
            }
            else if (e.Code == (int)IrcProtocolErrorEnum.MotdMissing)
            {
                // Just ignore
            }
        }

        private void SimpleBot_RawMessageReceived(object? sender, IrcRawMessageEventArgs e)
        {
#if true
            _logger.LogDebug("Raw Message Received: {RawContent} {Message}", e.RawContent, e.IrcMessage);

            // When whoever has our preferred nickname quits or changes nick, take it back right away
            // instead of waiting for the next WHOIS check.
            var sourceNick = (e.Message.Source as IrcUser)?.NickName;
            if (sourceNick != null && sourceNick == _options.Nick && _botNick.BotNick != _options.Nick)
            {
                if (e.Message.Command == "QUIT")
                {
                    _logger.LogWarning("{nick} quit, which opened our preferred nickname.", sourceNick);
                    HandlePreferredNameAvailable();
                }
                else if (e.Message.Command == "NICK" && e.Message.Parameters.Count > 0)
                {
                    HandleNicknameChanged(sourceNick, e.Message.Parameters[0]);
                }
            }
#else
            _logger.LogDebug("RawContent: {RawContent}", e.RawContent);
            _logger.LogDebug("Message:");
            _logger.LogDebug($"    Source: {e.IrcMessage.Source}");
            _logger.LogDebug($"    Prefix: {e.IrcMessage.Prefix}");
            _logger.LogDebug($"   Command: {e.IrcMessage.Command}");
            _logger.LogDebug($"    Params: {string.Join(",",e.IrcMessage.Parameters)}");
#endif
        }

        /// <summary>
        /// Handles when a user in the channel changes their nickname
        /// </summary>
        /// <param name="fromNick">Previous nickname of a user</param>
        /// <param name="toNick">New nickname of a user</param>
        private void HandleNicknameChanged(string fromNick, string toNick)
        {
            _logger.LogInformation($"User renamed from {fromNick} => {toNick}");
            if (fromNick == _options.Nick)
            {
                _logger.LogError("Nickname changed {fromNick} => {toNick} opened our preferred nickname!", fromNick, toNick);
                HandlePreferredNameAvailable();
            }
        }

        private void HandlePreferredNameAvailable()
        {
            _logger.LogWarning($"Renaming ourselves from {_client.LocalUser.NickName} to our preferred nickname {_options.Nick}");
            _client.LocalUser.SetNickName(_options.Nick);
        }

        private void Client_MotdReceived(object? sender, EventArgs e)
        {
            _logger.LogInformation("MOTD received: {message}", _client.MessageOfTheDay);
        }

        private void Client_ErrorMessageReceived(object? sender, IrcErrorMessageEventArgs e)
        {
            _logger.LogError("Error message received: {message}", e.Message);

            // e.g. "Closing Link: ... (K-Lined)" when the server bans us
            if (e.Message != null && _banMessageRegex.IsMatch(e.Message))
            {
                _banned = true;
            }
        }

        private void Client_Error(object? sender, IrcErrorEventArgs e)
        {
            LogConnectException("IRC error", e.Error);
            // If we got here, the read has stopped and we're dead in the water. 
            // TODO: Try to reconnect instead of exit?
            ExitBot();
        }

        public bool SendAction(string channel, string action)
        {
            _client.LocalUser.SendMessage(channel, $"\u0001ACTION {action}\u0001");
            return true;
        }

        public Task<List<string>> GetUsersInChannelAsync(string channel)
        {
            // IrcDotNet keeps each joined channel's member list up to date from NAMES, JOIN, PART, etc.
            var ircChannel = _client.Channels.FirstOrDefault(c => string.Equals(c.Name, channel, StringComparison.OrdinalIgnoreCase));
            var users = ircChannel?.Users.Select(u => u.User.NickName).ToList() ?? new List<string>();
            return Task.FromResult(users);
        }

        public bool IsUserOnline(string nick)
        {
            return _client.Channels.Any(c => c.Users.Any(u => string.Equals(u.User.NickName, nick, StringComparison.OrdinalIgnoreCase)));
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
