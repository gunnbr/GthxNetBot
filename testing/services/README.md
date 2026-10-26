# Local IRC services for testing

Anope 2.0 services (NickServ, ChanServ, SASL) linked to the local InspIRCd 4 test server on sakura
(`10.0.0.125:6667`). This lets the bot's SASL login be tested without connecting a test
bot to a public network.

## Setup

1. Create the settings file with a random link password, and the matching InspIRCd config:

       cp anope.env.example anope.env                # set ANOPE_UPLINK_PASSWORD
       sed "s/<link password>/<same password>/g" inspircd-services.example.conf > inspircd-services.conf

   Both files are git-ignored.

2. Install the InspIRCd side (needs sudo, since `/etc/inspircd` belongs to `irc`):

       sudo install -o irc -g irc -m 640 inspircd-services.conf /etc/inspircd/services.conf
       # then add this line to /etc/inspircd/inspircd.conf:
       #   <include file="/etc/inspircd/services.conf">
       sudo systemctl restart inspircd

   InspIRCd refuses to start if a module is loaded twice, so remove any `<module>` line from
   `services.conf` that `inspircd.conf` already has. Check with `journalctl -u inspircd`.

3. Start services and confirm the link:

       docker compose up -d
       docker compose logs -f anope        # look for "Successfully linked"

## Registering a nick for the bot

NickServ needs an email address (it isn't confirmed), and the nick must be connected for 30 seconds before
it can register. From an IRC client using the bot's nick:

    /msg NickServ REGISTER <password> <email>

Then set `IrcInfo__SaslPassword=<password>` in the bot's env file (for example `gthxtestbot.env`).
