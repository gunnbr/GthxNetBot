# GthxNetBot
This is the gthx IRC bot reimplemented in C# instead of the original python.
The main reasons for this are :
* Gthx was originally created to properly handle unicode, but python is a real pain to work with unicode. (Or at least Python2 certainly is!)
* C# is my main language for daily coding, so I'm much more familiar with it and can code things significantly faster
* I have free credit on Azure that I'd like to use and it seems easier to publish .NET apps to Azure than Python
* Gthx replaced the bot it used to track, so all the bot tracking code no longer needs to exist

# TODO
* Figure out how to get scoped lifetime to work for GthxDbContext to fix issue #29
* Finish the build, configure, and run instructions in this doc (which are currently mainly written as reminders for myself)
* Handle received actions and update the last seen
* Handle more CTCP messages
* Switch to use CtcpClient instead of IrcClient so CTCP is handled automatically
* Implement lurkers module

# Setup
## Database
GthxNetBot supports both MariaDB and SQL Server. One of these must be installed with a database created
for the bot and a user granted all permissions on the database.

### SQL Server via .NET Aspire (recommended for development)
For local development you don't need to install SQL Server manually. The `GthxNetBot.AppHost` project uses
.NET Aspire to automatically start a SQL Server container, create the `GthxDb` database, apply EF Core
migrations, and launch the bot wired to that database.

This project uses **Podman** (not Docker Desktop) as the container runtime:
* Install Podman and ensure the Podman machine is running: `podman machine start`
* Aspire is configured to target Podman via the `DOTNET_ASPIRE_CONTAINER_RUNTIME=podman` environment
  variable set in `GthxNetBot.AppHost/Properties/launchSettings.json`.
* The SQL Server data is kept in a persistent named volume (`WithDataVolume()`), so the database survives
  container restarts.

See the [Running](#running) section for how to start it.

## Visual Studio
To use with Visual Studio, install Visual Studio with the following features enabled:
* ASP.NET and web development
* .NET desktop development
* .NET cross-platform development

Then install the .NET 10 SDK

## Command Line
* Install dotnet tools
* Install entity framework tools
* Install .NET 10 SDK

## Via Docker
* Install Docker

# Cloning the source
GthxNetBot uses a source subrepo, so when cloning to build, be sure use the --recurse-submodules when cloning:
    git clone --recurse-submodules https://github.com/gunnbr/GthxNetBot.git
Or if you have cloned without doing this, the subrepo can be initialized by running:

    git submodule update --init

# Adding features
Modifying or adding features is done mainly in the Modules folder in the Gthx.Bot project. To add a new module,
add a new .cs file here that implements IGthxModule, then implement the interface, following the pattern from
the existing modules.

# Building
## Visual Studio
Use Ctrl-Shift-B or the Build->Build Solution menu item to build from Visual Studio

## Command Line
    dotnet build

## Docker
To build and tag with docker, use

    docker build -t gthxnetbot:latest .


# Configuring
Edit `appsettings.json` before running or set environment variables before running

# Running
## Visual Studio with .NET Aspire (recommended)
Make sure Podman is running (`podman machine start`), set `GthxNetBot.AppHost` as the startup project,
then use F5. Aspire will start a SQL Server container, create and migrate the `GthxDb` database, launch the
bot, and open the Aspire dashboard listing both resources. No manual SQL Server startup is required.

## Visual Studio
Use F5 to run through the debugger, Ctrl-F5 to run without the debugger or the Debug->Start Debugging
menu item to run through Visual Studio

## Command line
    dotnet run
Or find the compiled executables in the bin/Debug/net6.0

To run the full Aspire orchestration (SQL Server container + bot) from the command line:

    dotnet run --project GthxNetBot.AppHost

## Docker
To run with environment overrides and network access to localhost and automatic restart, use:

    docker run -d --env-file ./gthx.env --network="host" --restart unless-stopped gthxnetbot:latest

If you have modified the appsettings.json or added environment variables to configure, then you can
leave out the "--env-file ./gthx.env" part. Otherwise, copy "gthx.env.example" to "gthx.env" and edit
it to suit your installation.

Then status can be monitored by way of the configured logging.
