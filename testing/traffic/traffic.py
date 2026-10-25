"""Synthetic channel traffic for soak-testing the bot on the local test IRC server.

Three simulated users take turns doing what real channel users do: factoid lookups and edits, tell,
seen, status, google, URLs, actions, chatter, and parting/rejoining. Each action that should get an
answer is timed. Missing answers are logged as "NO REPLY", link mention counts that fail to go up as
"COUNT DID NOT INCREASE", and a summary is logged every hour.

Never point this at a public network.
"""
import os
import random
import re
import socket
import threading
import time
from datetime import datetime, timezone

HOST = os.environ.get("IRC_HOST", "10.0.0.125")
PORT = int(os.environ.get("IRC_PORT", "6667"))
CHANNEL = os.environ.get("IRC_CHANNEL", "#gthxtestchannel")
BOT = os.environ.get("BOT_NICK", "gthxtestbot")
MIN_GAP = float(os.environ.get("MIN_GAP_SECONDS", "60"))
MAX_GAP = float(os.environ.get("MAX_GAP_SECONDS", "240"))
REPLY_TIMEOUT = 30
# Links the bot already has titles for, so repeats are answered from the database without fetching
# anything from YouTube; each repeat should raise the video's "IRC mentions" count by one.
# (Thingiverse links are left out: thingiverse.com answers the bot's title fetch with 403 Forbidden.)
YOUTUBE_IDS = ["sQGxT2JgNgY", "kPnN4IE_a98", "k3oySviXuCQ"]
MENTIONS_RE = re.compile(r"=> (\d+) IRC mentions")

if HOST.endswith("libera.chat"):
    raise SystemExit("Refusing to generate test traffic on Libera.")


def log(msg):
    print(f"{datetime.now(timezone.utc):%Y-%m-%d %H:%M:%S} {msg}", flush=True)


class Client:
    def __init__(self, nick, observer=False):
        self.nick = nick
        self.observer = observer  # the observer records the bot's channel messages
        self.in_channel = threading.Event()
        self.bot_messages = []  # (time, text)
        self.lock = threading.Lock()
        self.sock = None
        threading.Thread(target=self._run, daemon=True).start()

    def _run(self):
        while True:
            try:
                self._session()
            except OSError as e:
                log(f"{self.nick}: connection error: {e}")
            self.in_channel.clear()
            time.sleep(30)

    def _session(self):
        self.sock = socket.create_connection((HOST, PORT), timeout=10)
        self.sock.settimeout(1)
        self.send(f"NICK {self.nick}")
        self.send(f"USER {self.nick} 0 * :soak test user")
        buf = ""
        while True:
            try:
                data = self.sock.recv(4096)
            except socket.timeout:
                continue
            if not data:
                log(f"{self.nick}: disconnected")
                return
            buf += data.decode(errors="replace")
            while "\n" in buf:
                line, buf = buf.split("\n", 1)
                self._handle(line.rstrip("\r"))

    def _handle(self, line):
        if line.startswith("PING"):
            self.send("PONG" + line[4:])
        elif " 001 " in line:
            self.send(f"JOIN {CHANNEL}")
        elif " 366 " in line:
            self.in_channel.set()
        elif line.startswith(f":{BOT}!") and " PRIVMSG " in line:
            target, text = line.split(" PRIVMSG ", 1)[1].split(" :", 1)
            if self.observer and target == CHANNEL:
                with self.lock:
                    self.bot_messages.append((time.time(), text))

    def send(self, line):
        self.sock.sendall((line + "\r\n").encode())

    def say(self, text):
        self.send(f"PRIVMSG {CHANNEL} :{text}")

    def action(self, text):
        self.send(f"PRIVMSG {CHANNEL} :\x01ACTION {text}\x01")


class Soak:
    def __init__(self):
        self.alice = Client("soakalice", observer=True)
        self.bob = Client("soakbob")
        self.carol = Client("soakcarol")
        self.users = [self.alice, self.bob, self.carol]
        self.counter = 0
        self.tells = 0
        self.mentions = {}  # link id -> last "IRC mentions" count
        self.stats = {"sent": 0, "replied": 0, "missing": 0, "latencies": []}
        self.hour_start = time.time()

    def wait_ready(self):
        for user in self.users:
            user.in_channel.wait()

    def expect(self, user, text, label, contains=None, as_action=False):
        """Sends text and waits for the bot's reply in the channel. Returns the reply or None."""
        with self.alice.lock:
            self.alice.bot_messages.clear()
        sent = time.time()
        (user.action if as_action else user.say)(text)
        self.stats["sent"] += 1
        while time.time() - sent < REPLY_TIMEOUT:
            with self.alice.lock:
                for t, msg in self.alice.bot_messages:
                    if contains is None or contains in msg:
                        latency = t - sent
                        self.stats["replied"] += 1
                        self.stats["latencies"].append(latency)
                        log(f"{label}: {latency:.2f}s <{user.nick}> {text} -> {msg[:100]}")
                        return msg
            time.sleep(0.1)
        self.stats["missing"] += 1
        log(f"NO REPLY {label}: <{user.nick}> {text}")
        return None

    def other(self, user):
        return random.choice([u for u in self.users if u is not user])

    # Actions -------------------------------------------------------------

    def factoid_get(self, u):
        name = random.choice(["reprap", "extruder", "hotend", "reprap"])
        self.expect(u, f"{name}?", "factoid-get")

    def factoid_cycle(self, u):
        self.counter += 1
        name = f"soaktest{self.counter}"
        self.expect(u, f"{BOT}: {name} is a soak test value", "factoid-set", contains="Okay")
        self.expect(u, f"{name}?", "factoid-get-new", contains=name)
        self.expect(u, f"{BOT}: {name} is also updated later", "factoid-also", contains="Okay")
        self.expect(u, f"{BOT}: forget {name}", "factoid-forget", contains="forgotten")

    def info(self, u):
        self.expect(u, "info reprap", "info")

    def tell(self, u):
        target = self.other(u)
        self.tells += 1
        self.expect(u, f"{BOT}: tell {target.nick} soak test message {self.tells}", "tell", contains="pass that on")
        time.sleep(random.uniform(10, 40))
        self.expect(target, random.choice(["back again", "hi all", "anyone around"]), "tell-delivery", contains=f"tell {target.nick}")

    def seen(self, u):
        self.expect(u, f"seen {self.other(u).nick}?", "seen")

    def status(self, u):
        self.expect(u, f"{BOT}: status?", "status", contains="OK")

    def google(self, u):
        self.expect(u, f"google reprap extruder for {self.other(u).nick}", "google", contains="lmgtfy")

    def youtube(self, u):
        video = random.choice(YOUTUBE_IDS)
        reply = self.expect(u, f"check this https://www.youtube.com/watch?v={video}", "youtube", contains="IRC mentions")
        self.check_mentions(f"youtube:{video}", reply)

    def check_mentions(self, key, reply):
        """Each link should raise the stored mention count (by one, unless someone else also posted it)."""
        match = MENTIONS_RE.search(reply or "")
        if not match:
            return
        count = int(match.group(1))
        last = self.mentions.get(key)
        if last is not None and count <= last:
            log(f"COUNT DID NOT INCREASE {key}: was {last}, now {count}")
        else:
            log(f"mentions {key}: {count}")
        self.mentions[key] = count

    def emote(self, u):
        u.action(random.choice(["waves", "tightens the belts", "levels the bed"]))

    def chatter(self, u):
        u.say(random.choice([
            "the first layer looks good now",
            "I think my nozzle is clogged",
            "PETG strings so much",
            "upgraded the firmware last night",
        ]))

    def part_join(self, u):
        user = self.carol
        user.send(f"PART {CHANNEL} :brb")
        time.sleep(random.uniform(5, 20))
        user.in_channel.clear()
        user.send(f"JOIN {CHANNEL}")
        user.in_channel.wait(30)

    # Main loop -----------------------------------------------------------

    def summary(self):
        lat = sorted(self.stats["latencies"])
        if lat:
            p50, worst = lat[len(lat) // 2], lat[-1]
            lat_text = f"median {p50:.2f}s, max {worst:.2f}s"
        else:
            lat_text = "no replies"
        log(f"SUMMARY last hour: sent {self.stats['sent']}, replied {self.stats['replied']}, "
            f"missing {self.stats['missing']}, {lat_text}")
        self.stats = {"sent": 0, "replied": 0, "missing": 0, "latencies": []}
        self.hour_start = time.time()

    def run(self):
        log(f"Soak traffic for {BOT} in {CHANNEL} on {HOST}:{PORT}")
        actions = [
            (self.factoid_get, 5), (self.factoid_cycle, 2), (self.info, 1), (self.tell, 2),
            (self.seen, 2), (self.status, 1), (self.google, 1), (self.youtube, 2),
            (self.emote, 1), (self.chatter, 3), (self.part_join, 1),
        ]
        funcs, weights = zip(*actions)
        while True:
            self.wait_ready()
            action = random.choices(funcs, weights)[0]
            user = random.choice([self.alice, self.bob])
            try:
                action(user)
            except OSError as e:
                log(f"action {action.__name__} failed: {e}")
            if time.time() - self.hour_start >= 3600:
                self.summary()
            time.sleep(random.uniform(MIN_GAP, MAX_GAP))


if __name__ == "__main__":
    Soak().run()
