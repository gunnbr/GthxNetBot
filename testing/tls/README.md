# TLS for the local test IRC server

Adds a TLS listener on `10.0.0.125:6697` to the local InspIRCd 4 server, so the bot can be tested with
TLS and SASL the way it connects to Libera. The certificate comes from a private test CA that only the
bot is told to trust, so the bot's certificate check is exercised for real.

## Setup

1. Make the CA and server certificate (written to `out/`, which is git-ignored):

       ./make-certs.sh                                     # 10.0.0.125 and irc.gunnbr.org
       ./make-certs.sh 10.0.0.125 irc.gunnbr.org <other public name>...

   If the server is also reachable from outside under another name (for a bot running in Azure), pass
   that name as an extra argument. Running the script again keeps the CA and replaces only the server
   certificate, so the bots that trust the CA keep working; reinstall `server.pem` and `server.key` (step 2)
   and restart InspIRCd.

2. Install them and the config (needs sudo, since `/etc/inspircd` belongs to `irc`):

       sudo install -d -o irc -g irc -m 750 /etc/inspircd/tls
       sudo install -o irc -g irc -m 640 out/server.pem /etc/inspircd/tls/server.pem
       sudo install -o irc -g irc -m 600 out/server.key /etc/inspircd/tls/server.key
       sudo install -o irc -g irc -m 640 inspircd-tls.conf /etc/inspircd/tls.conf
       # then add this line to /etc/inspircd/inspircd.conf:
       #   <include file="/etc/inspircd/tls.conf">
       sudo systemctl restart inspircd

   If InspIRCd doesn't start, check `journalctl -u inspircd`. The usual cause is `ssl_gnutls` being
   loaded twice; remove the `<module>` line from `tls.conf`.

3. Check the listener:

       openssl s_client -connect 10.0.0.125:6697 -CAfile out/ca.pem -brief </dev/null

   It should end with `Verification: OK`.

## Bot settings

Mount `out/ca.pem` into the container and point the bot at it (see `compose.gthx.yaml`):

    IrcInfo__Server=10.0.0.125
    IrcInfo__Port=6697
    IrcInfo__UseSsl=true
    IrcInfo__TrustedCaCertificate=/certs/ca.pem

Leave `IrcInfo__TrustedCaCertificate` unset for Libera, which has a publicly trusted certificate.
