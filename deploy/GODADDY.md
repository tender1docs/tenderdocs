# Deploying TenderDocs on GoDaddy

TenderDocs is a Docker app (a .NET API + a React site + PostgreSQL). **GoDaddy's
shared/cPanel/WordPress hosting cannot run it** — those only run PHP/static sites.
You need a server you control: a **GoDaddy VPS** (Ubuntu). Your GoDaddy _domain_ is
fine; it's the _hosting_ that must be a VPS.

> If you already bought shared hosting, you can keep the domain at GoDaddy and just
> point it at a VPS (from GoDaddy or any provider). The steps below are the same.

---

## 1. Get a GoDaddy VPS

- GoDaddy → **VPS Hosting** → a plan with **≥ 2 GB RAM** (4 GB is comfortable).
- OS: **Ubuntu 22.04 (64‑bit)**.
- After it's created, note the **public IP address** and the **root password** (or SSH key).

## 2. Point your domain at the VPS

In GoDaddy → your domain → **DNS / Manage DNS**, create:

- **A** record: Host `@` → Value `YOUR_VPS_IP`
- **A** record: Host `www` → Value `YOUR_VPS_IP` (or a CNAME to `@`)

(If you'll use a subdomain like `app.yourdomain.com`, add an **A** record: Host `app` → `YOUR_VPS_IP`.)

DNS can take a few minutes to a couple of hours to propagate.

## 3. Connect to the VPS and install Docker

From your Mac terminal:

```bash
ssh root@YOUR_VPS_IP
```

Then on the server:

```bash
curl -fsSL https://get.docker.com | sh        # installs Docker + Compose plugin
docker --version && docker compose version     # verify
```

Open the firewall for web traffic (if ufw is on):

```bash
ufw allow OpenSSH && ufw allow 80 && ufw allow 443 && ufw --force enable
```

Also make sure GoDaddy's VPS firewall (in their control panel) allows **80** and **443**.

## 4. Put the code on the server

Easiest is Git. From the server:

```bash
mkdir -p /opt && cd /opt
git clone <your-repo-url> tenderdocs        # or scp the TendeerDocs_done folder up
cd tenderdocs/deploy
```

(No repo? From your Mac: `scp -r /Users/prasad/Desktop/TendeerDocs_done root@YOUR_VPS_IP:/opt/tenderdocs`)

## 5. Configure secrets

```bash
cp .env.example .env
nano .env
```

Fill in:

- `DOMAIN` = `yourdomain.com` (or `app.yourdomain.com`) — **no https://**
- `PUBLIC_URL` = `https://yourdomain.com`
- `POSTGRES_PASSWORD`, `JWT_SECRET`, `ENCRYPTION_KEY` — generate each with `openssl rand -base64 32`
- `GOOGLE_CLIENT_ID` — the same id already in `TenderDocs-Frontend/frontend/.env`
- `GOOGLE_REDIRECT_URI` = `https://yourdomain.com/api/google-drive/callback`

Update the `{$DOMAIN}` line in `Caddyfile` if you want `www` too (e.g. `yourdomain.com, www.yourdomain.com`).

## 6. Allow your domain in Google sign‑in

In **Google Cloud Console → APIs & Services → Credentials → your OAuth client**:

- **Authorized JavaScript origins**: add `https://yourdomain.com`
- (If using Drive) **Authorized redirect URIs**: add `https://yourdomain.com/api/google-drive/callback`
- Keep the OAuth consent screen in **Testing** with `tender1docs@gmail.com` (and any other allowed testers) under **Test users**. See the security note at the bottom before publishing.

## 7. Launch

```bash
docker compose -f docker-compose.server.yml up -d --build
```

First run builds the images and Caddy fetches the HTTPS certificate automatically.
Watch progress:

```bash
docker compose -f docker-compose.server.yml logs -f caddy   # cert issuance
docker compose -f docker-compose.server.yml ps              # all "Up"
```

Then open **https://yourdomain.com** → sign in with Google → pick a role.

---

## Day‑2 operations

- **Update after code changes:**
  ```bash
  cd /opt/tenderdocs && git pull
  cd deploy && docker compose -f docker-compose.server.yml up -d --build
  ```
  (DB migrations apply automatically on API start; the Postgres volume keeps your data.)
- **Back up the database:**
  ```bash
  docker compose -f docker-compose.server.yml exec postgres \
    pg_dump -U tenderdocs tenderdocs > backup_$(date +%F).sql
  ```
- **Logs:** `docker compose -f docker-compose.server.yml logs -f api`
- **Uploaded files** live in the `storage` Docker volume (and the DB in `pgdata`). Back both up before major changes.

## Security note (important before going multi‑user)

The role picker lets a signed‑in user choose Uploader/Approver/Viewer themselves — fine
while the Google app is in **Testing** (only listed test users can sign in). If you
**Publish** the OAuth app so anyone with a Google account can log in, anyone could grant
themselves Approver. Before that, restrict who can sign in (allow‑list of emails) and/or
have an admin assign roles instead of self‑service. Ask and I can add that.

Buy a GoDaddy VPS — Ubuntu 22.04, ≥ 2 GB RAM. Note its IP.
Point DNS — in GoDaddy DNS, add an A record for @ (and www) → your VPS IP.
SSH in + install Docker: curl -fsSL https://get.docker.com | sh, and open ports 80/443.
Get the code on the server — git clone it, or scp the TendeerDocs_done folder up to /opt/tenderdocs.
Configure deploy/.env — set your domain, and generate JWT_SECRET / ENCRYPTION_KEY / POSTGRES_PASSWORD with openssl rand -base64 32. Put in the Google client id.
Allow your domain in Google — Cloud Console → your OAuth client → add https://yourdomain.com to Authorized JavaScript origins.
Launch:
cd /opt/tenderdocs/deploy
docker compose -f docker-compose.server.yml up -d --build
Open https://yourdomain.com → sign in with Google. HTTPS is automatic.
Updates later are just git pull + the same up -d --build (your data stays).

What you'll need to provide
A GoDaddy VPS (or any Linux VPS) + the domain DNS pointed at it.
Add your production domain to the Google OAuth "Authorized origins".
Keep the Google OAuth app in Testing mode with tender1docs@gmail.com as a test user for now.
One security flag before real users
The "switch role" picker lets a signed‑in user pick Approver (full access) themselves. That's safe while the Google app is in Testing (only your listed test users can even log in). Before you "Publish" the OAuth app so the public can sign in, you'd want an email allow‑list or admin‑assigned roles instead — say the word and I'll add that.

When you've got the VPS and domain ready, I can walk through the launch with you live (or troubleshoot the first up -d --build). Want me to also add a tiny deploy.sh one‑liner script and an email allow‑list for sign‑in now, or leave those until you're on the server?
