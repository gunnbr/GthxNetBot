#!/bin/sh
# Creates a private CA and a TLS certificate for the local test IRC server, in ./out (git-ignored).
# The bot trusts the CA through IrcInfo__TrustedCaCertificate; nothing else trusts it.
#
#   ./make-certs.sh [server IP] [server name]
set -eu
IP=${1:-10.0.0.125}
NAME=${2:-irc.gunnbr.org}
cd "$(dirname "$0")"
mkdir -p out
cd out
if [ -e server.key ]; then
    echo "out/ already has certificates; delete it to make new ones." >&2
    exit 1
fi

openssl req -x509 -new -nodes -newkey rsa:3072 -sha256 -days 3650 \
    -keyout ca.key -out ca.pem -subj "/CN=Gthx Test CA" \
    -addext "basicConstraints=critical,CA:TRUE" -addext "keyUsage=critical,keyCertSign,cRLSign"

openssl req -new -nodes -newkey rsa:3072 -sha256 -keyout server.key -out server.csr -subj "/CN=$NAME"
openssl x509 -req -in server.csr -CA ca.pem -CAkey ca.key -CAcreateserial -sha256 -days 825 -out server.pem \
    -extfile /dev/stdin <<EXT
basicConstraints=critical,CA:FALSE
keyUsage=critical,digitalSignature,keyEncipherment
extendedKeyUsage=serverAuth
subjectAltName=IP:$IP,DNS:$NAME
EXT
rm server.csr
chmod 600 ca.key server.key
openssl x509 -in server.pem -noout -subject -ext subjectAltName -enddate
