# GthxNetBot
[![CI](https://github.com/gunnbr/GthxNetBot/actions/workflows/ci.yml/badge.svg)](https://github.com/gunnbr/GthxNetBot/actions/workflows/ci.yml)
[![Build and deploy to Azure](https://github.com/gunnbr/GthxNetBot/actions/workflows/deploy.yml/badge.svg?branch=master)](https://github.com/gunnbr/GthxNetBot/actions/workflows/deploy.yml)

gthx is an IRC bot for the RepRap and 3D printing channels on Libera (`#reprap` and others). It remembers
factoids ("extruder is the part that pushes the plastic"), passes messages on, reminds you of things,
tells you when someone was last seen, and counts how often YouTube, Thingiverse, Printables and MakerWorld links come up.

This is gthx rewritten in C# (.NET 10) from the original Python. It runs in Azure Container Apps and is
deployed from this repository. [ROADMAP.md](ROADMAP.md) has the current plans and status, and
[History.txt](History.txt) the release notes.

## Using the bot

In a channel, address the bot by its nick (`gthx: `, `gthx, ` and so on). In a private message there's no
need to, but it does no harm. Some commands work without addressing the bot, as noted.

| Command | What it does |
|---|---|
| `gthx: extruder is the part that pushes the plastic` | Sets a factoid. `are` works too (`gthx: hotends are hot`). Setting it again replaces it. |
| `gthx: extruder is also a motor` | Adds another value to a factoid. |
| `extruder?` or `extruder!` | Says the factoid, without addressing the bot. All its values are joined with "and also". |
| `extruder? \| someuser` | Says the factoid to someone. The `?` and the spaces are optional: `extruder \| someuser` and `extruder\|someuser` work too. |
| `gthx: forget extruder` | Forgets a factoid. |
| `info extruder` | How often the factoid was asked for, and who set or deleted it when. |
| `gthx: tell someuser the parts arrived` | Passes the message on the next time `someuser` says something. |
| `gthx: remind me in 2 hours to check the print` | Reminds you later (see below). |
| `gthx: list reminders` | Your reminders that haven't come due, with their numbers. |
| `gthx: cancel reminder 42` | Cancels one of your reminders. |
| `seen someuser?` | When and where `someuser` last spoke, and what they said. Not updated by private messages. |
| `gthx: google pla temperature for someuser` | Gives `someuser` a search link. |
| `status?` | The bot's version, uptime and mood. |
| `lurkers?` | How many people in the channel have never said anything. |

A factoid value can start with `<reply>` (the bot says just the rest) or `<action>` (the bot does it as a
`/me` action), and `!who` and `!channel` in it are replaced by the asker's nick and the channel. Some
factoids are locked and can't be changed or forgotten.

A reminder's time is relative: `30 minutes`, `an hour`, `2h`, `1h30m`, `1 day and 3 hours`, `2 weeks`
(units are seconds, minutes, hours, days, weeks, months and years; a month is 30 days), from a minute to a year ahead. Each nick can have 10 waiting,
of up to 400 characters each. When one comes due, the bot posts it right away if you've said something
in the last 5 minutes and are still online (in the channel you asked in, or by private message if you've
left it). Otherwise it waits and reminds you the next time you speak. Reminders are kept in the database,
so they survive restarts.

When someone posts a link, the bot replies with its title and how many times it has been linked:

| Site | Example link | Title from |
|---|---|---|
| YouTube | `https://youtu.be/<id>`, `https://www.youtube.com/watch?v=<id>` | YouTube's oEmbed API |
| Thingiverse | `https://www.thingiverse.com/thing:<number>` | Not fetched: thingiverse.com refuses the bot's requests, and its API's license terms aren't acceptable ([#39](https://github.com/gunnbr/GthxNetBot/issues/39)). The reply gives the thing's number, or a title saved before. |
| Printables | `https://www.printables.com/model/1863827-heavy-duty-keyboard-holder` | The name at the end of the link, or from an earlier link to the same model |
| MakerWorld | `https://makerworld.com/en/models/2459711-articulated-dragon` | The same as Printables |

## How it runs

- **Production** is one container in Azure Container Apps (resource group `rg-gthx-prod`), using an Azure SQL
  database and signing in to IRC with SASL over TLS. Secrets are in Key Vault. [infra/README.md](infra/README.md)
  describes the resources, the secrets, the one-time setup and how to read the logs.
- **Deploys:** every push to `master` runs the tests, builds the image into the registry and deploys it, after
  approval for the `production` environment. Pushes that change only Markdown files don't deploy. During a
  deploy the new container joins as `gthx_` and takes the `gthx` nick as soon as the old one quits.
- **Version numbers** come from [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning):
  `3.0.<commits since version.json changed>+<commit>`, shown by `status?`, CTCP VERSION and the startup log
  line. Bump the major or minor version in `version.json`.
- **Logs** go to the console as JSON, which Azure collects in Log Analytics; `scripts/gthx-logs` reads them.
  Warnings and errors are also emailed, batched every 10 seconds. The startup line is logged as an error on
  purpose, so every restart sends an email.
- **Reconnects:** the bot exits whenever its connection ends, and the container is restarted. To avoid rapid
  reconnect loops (which can get a host banned), it waits first: 30 seconds after a normal run, 5 minutes if
  the connection failed soon after connecting, 15 minutes after a SASL failure and an hour after a ban.

## Repository layout

| Folder | Contents |
|---|---|
| `GthxNetBot` | The program: configuration, logging, the IRC connection (`GthxIrcClient`) and the console test mode. |
| `Gthx.Bot` | The bot itself. Each command or link type is a module in `Gthx.Bot/Modules`. |
| `Gthx.Data` | Submodule: the database code and migrations, from [gunnbr/GthxNetData](https://github.com/gunnbr/GthxNetData). |
| `IrcDotNet` | Submodule: [gunnbr's fork of IrcDotNet](https://github.com/gunnbr/IrcDotNet), with SASL, TCP keepalive and other fixes. The original project is no longer maintained. |
| `Gthx.Test` | NUnit tests. |
| `GthxNetBot.AppHost` | .NET Aspire host for running the bot with a SQL Server container from Visual Studio. |
| `infra` | The Azure deployment (Bicep) and its setup script. |
| `testing` | The local test IRC server's NickServ (`services`), TLS certificate (`tls`) and soak traffic (`traffic`). |
| `scripts` | `gthx-logs` for reading the Azure logs. |

## Getting the source

The repository uses submodules, so clone it with:

    git clone --recurse-submodules https://github.com/gunnbr/GthxNetBot.git

or, in an existing clone:

    git submodule update --init

## Building and testing

You need the .NET 10 SDK, and Docker for the tests and the container image.

    dotnet build
    dotnet test Gthx.Test/Gthx.Test.csproj
    docker build -t gthxnetbot:dev .

The SQL Server tests start a SQL Server container with Testcontainers. No test needs the internet: web
pages and titles come from mocks. Most projects build into a shared `bin/` folder at the root, which can
collect stale files from older builds; if something fails to load, delete `bin/` and build again (see
ROADMAP.md, phase 12).

## Running locally

Never point a test bot at Libera or another public network: test runs got us banned there once. Use a
local IRC server. The one this project uses is InspIRCd with Anope services; `testing/services` and
`testing/tls` set up NickServ (for SASL) and a TLS certificate from a private CA.

### Docker Compose: the bot and a SQL Server

`compose.yaml` runs the bot alongside a SQL Server container, whose data is kept in the `gthx-sql-data`
volume. The database is created and migrated when the bot starts.

    cp gthx.env.example gthx.env                       # then edit: server, nick, channels, SASL password, email
    echo 'SQL_SA_PASSWORD=<strong password>' > .env    # SQL Server sa password
    docker compose up -d --build
    docker compose logs -f gthxbot

`gthx.env` and `.env` hold secrets; both are ignored by git and kept out of the Docker build. SQL Server is
published on `127.0.0.1:14333` for looking at the data with a SQL client.

To try the bot without IRC, run the console mode against the same database (address the bot as `gnetbot`):

    docker compose run --rm --no-deps gthxbot --console

### Other ways to run it

- **A test bot next to production:** `compose.gthxtestbot.yaml` runs the bot as `gthxtestbot`, with its own
  nick and database settings (`cp gthxtestbot.env.example gthxtestbot.env`).
- **The production settings on Docker:** `compose.gthx.yaml` runs the bot as container `gthx` with TLS and
  SASL (`cp gthx.production.env.example gthx.production.env`). It was used to rehearse the Libera cutover,
  and is a fallback if Azure is unavailable.
- **Soak traffic:** `testing/traffic` runs three simulated users that talk to the bot on the test server and
  log any request that gets no reply.
- **Visual Studio with .NET Aspire:** set `GthxNetBot.AppHost` as the startup project and press F5. Aspire
  starts a SQL Server container, creates and migrates the database, and starts the bot. It's set up to use
  Podman (`DOTNET_ASPIRE_CONTAINER_RUNTIME=podman` in its `launchSettings.json`); run `podman machine start`
  first, or remove that setting to use Docker.
- **An existing database:** `docker run -d --env-file ./gthx.env --restart unless-stopped gthxnetbot:dev`,
  with `ConnectionStrings__GthxDb` set in `gthx.env`.

## Configuration

Settings come from `GthxNetBot/appsettings.json`, overridden by environment variables (`IrcInfo__Nick` for
`IrcInfo:Nick`, and so on). The `*.env.example` files list the usual ones.

| Setting | Meaning |
|---|---|
| `ConnectionStrings:GthxDb` | The SQL Server connection string. In Azure the bot signs in with its managed identity, so there's no password. |
| `IrcInfo:Server`, `Port`, `UseSsl` | The IRC server. Use TLS (port 6697) on public networks. |
| `IrcInfo:Nick`, `RealName`, `Channels` | The bot's nick and real name, and the channels to join (comma-separated). |
| `IrcInfo:SaslPassword` | The NickServ password for the nick, used to log in with SASL. If SASL fails, the bot doesn't join any channels. |
| `IrcInfo:TrustedCaCertificate` | One more CA to trust for the server's TLS certificate (a file path or the PEM text), for a test server with a private certificate. Leave unset for Libera. |
| `IrcInfo:BannedExitDelayMinutes`, `SaslFailureExitDelayMinutes`, `EarlyFailureExitDelayMinutes`, `DisconnectExitDelayMinutes`, `StableConnectionMinutes` | The waits before exiting after a lost connection (60, 15, 5 and 0.5 minutes), and how long a connection must last to count as a normal run (5 minutes). |
| `EmailConfiguration:*` | `MailServer`, `Port`, `UserName`, `Password`, `FromName`, `ToEmail` and `EmailSubject` for the error emails. Email is off unless all are set. |
| `Serilog` | Log levels and outputs ([Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration)). |

## Making changes

- **New commands or link types** go in `Gthx.Bot/Modules`: add a class that implements `IGthxModule`,
  following the existing modules, and register it in `GthxBotExtensions.cs`. Modules run in that order
  until one says it handled the message; the factoid module is last.
- **Database changes** are made in [gunnbr/GthxNetData](https://github.com/gunnbr/GthxNetData) (see its
  README for adding a migration), then the `Gthx.Data` submodule is moved to the new commit. The bot applies
  any new migrations when it starts.
- **Pull requests:** `master` is protected, so changes go through a pull request, and the CI tests must pass
  before it can be merged. Merging deploys (after approval). Dependabot opens monthly pull requests for
  package, GitHub Actions and `Gthx.Data` updates.
- **Secrets** (`*.env` files, `testing/services/anope.env`, the TLS keys in `testing/tls/out`) are ignored by
  git. Never commit them.
