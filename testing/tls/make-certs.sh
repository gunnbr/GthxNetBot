#!/bin/sh
# Creates a private CA (once) and a TLS certificate for the local test IRC server, in ./out (git-ignored).
# The bot trusts the CA through IrcInfo__TrustedCaCertificate; nothing else trusts it.
#
#   ./make-certs.sh <server IP> <server name> [more server names...]
#
# Running it again keeps the CA and replaces the server certificate (the old one is kept as *.old), so
# clients that trust the CA keep working. Pass any extra public names the server is reached by as
# arguments rather than writing them into this file. (The server's names aren't kept in the repository.)
set -eu
if [ $# -lt 2 ]; then
    echo "usage: $0 <server IP> <server name> [more server names...]" >&2
    exit 2
fi
IP=$1
NAME=$2
shift 2
SAN="IP:$IP,DNS:$NAME"
for extra in "$@"; do
    SAN="$SAN,DNS:$extra"
done
cd "$(dirname "$0")"
mkdir -p out
cd out

if [ ! -e ca.key ]; then
    openssl req -x509 -new -nodes -newkey rsa:3072 -sha256 -days 3650 \
        -keyout ca.key -out ca.pem -subj "/CN=Gthx Test CA" \
        -addext "basicConstraints=critical,CA:TRUE" -addext "keyUsage=critical,keyCertSign,cRLSign"
    chmod 600 ca.key
fi

if [ -e server.key ]; then
    mv server.key server.key.old
    mv server.pem server.pem.old
fi
openssl req -new -nodes -newkey rsa:3072 -sha256 -keyout server.key -out server.csr -subj "/CN=$NAME"
openssl x509 -req -in server.csr -CA ca.pem -CAkey ca.key -CAcreateserial -sha256 -days 825 -out server.pem \
    -extfile /dev/stdin <<EXT
basicConstraints=critical,CA:FALSE
keyUsage=critical,digitalSignature,keyEncipherment
extendedKeyUsage=serverAuth
subjectAltName=$SAN
EXT
rm server.csr
chmod 600 server.key
openssl x509 -in server.pem -noout -subject -ext subjectAltName -enddate
