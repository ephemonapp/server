# Ephemon Server

## Overview

Ephemon Server is the server-side reference implementation of the Ephemon protocol, which powers the [Ephemon Messenger client](https://github.com/ephemonapp/client). It provides a protocol-only backend for end-to-end encrypted real-time communication without any content retention or decryption capabilities.

## Self-hosting

[`deployment/install.sh`](deployment/install.sh) brings up a complete instance with Docker Compose — nginx, this server, Redis, a STUN/TURN server, and optionally the web client itself. It generates a VAPID key pair and TURN credentials for you, and if you point a domain at the host it also issues a Let's Encrypt certificate and keeps it renewed.

```bash
curl -sSL https://get.ephemon.app | bash
```

`get.ephemon.app` serves [`deployment/install.sh`](deployment/install.sh) from `main`, so that one line always fetches the current installer.

The script is interactive, but it asks its questions on the terminal rather than on standard input, which is why piping it into a shell works. It does need bash — `| sh` will not do. To read it before running it, save it first:

```bash
curl -sSL https://get.ephemon.app -o install.sh
bash install.sh
```

### The web client

The installer offers to serve the [Ephemon client](https://github.com/ephemonapp/client) web app from the same host, choosing between the latest stable release and the latest pre-release (stable by default). Those builds ship without a server baked in and read their configuration from `/config.json` on the origin they are served from — which is exactly what this installer writes there. So a client hosted this way arrives already pointed at your instance, with no setup screen and nothing to copy by hand.

nginx serves the app at `/` and passes `/api/v1/`, `/signal/v1`, `/health` and `/config.json` through to the right place.

Decline that step to run a protocol-only server. The installer still prints the configuration document and still serves it at `/config.json`, so you can load it into a client hosted elsewhere by dropping the file onto its setup screen or pasting the JSON.

### Requirements

A Linux host you have root or `sudo` on. If Docker or the Compose plugin is missing or too old, the script offers to install them for you.

Ports 80, 3478 and the TURN relay range have to be reachable from the internet — and with a domain, 443 as well, plus port 80 has to *stay* open, because renewal revalidates over it.

Running without a domain works and is the right choice for a local or LAN trial, but browsers only grant service workers and push notifications to secure origins, and a page served over HTTPS cannot call an `http://` server at all. The script spells out exactly what does and does not work before you commit to that path.