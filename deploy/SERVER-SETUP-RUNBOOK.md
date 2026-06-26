# TenderDocs — Server Setup Runbook (Hostinger VPS, step by step)

Follow this top to bottom when you provision. It assumes Ubuntu 22.04/24.04 and the
single-server build (`docker-compose.server.yml`). GitHub Actions auto-deploy is the
**optional Stage 8** — do it only after the manual deploy works.

```
What you'll end up with:
  Internet ──HTTPS──▶ Caddy ──▶ frontend (SPA) + api (.NET, gs+qpdf) ──▶ PostgreSQL
  All four run via `docker compose` on ONE Hostinger VPS. Caddy gets HTTPS automatically.
```

**Before you start, have ready:**
- A domain (or subdomain) you control, e.g. `tenderdocs.yourcompany.com`.
- Your Google OAuth **client id + secret** (only if you want Google sign-in / Drive) — your own Google Cloud project.
- The 3 generated infra secrets already sitting in `deploy/.env` (POSTGRES_PASSWORD, JWT_SECRET, ENCRYPTION_KEY). **Keep a copy in a password manager.**

---

## Stage 1 — Put the code on GitHub (do this now, no VPS needed)

The repo is already initialized and committed locally. Create the remote and push.

1. Create an **empty** repo on github.com (no README/.gitignore), e.g. `tenderdocs`. Copy its URL.
2. From the project root on your machine:
   ```bash
   git branch -M main
   git remote add origin https://github.com/<you>/tenderdocs.git
   git push -u origin main
   ```
   (Private repo over HTTPS will prompt for a GitHub username + a **Personal Access Token** as the password — create one at GitHub → Settings → Developer settings → Tokens.)
3. Open the repo's **Actions** tab. The CI pipeline runs automatically and will:
   - build + test the backend, build the frontend,
   - on `main`, **publish two Docker images to GHCR** (`...-backend`, `...-frontend`).
   The **deploy** job stays skipped until you enable it in Stage 8.

> ✅ Safety: `deploy/.env`, `TenderDocs-Backend/.env`, and `frontend/.env` are gitignored —
> they will **not** be pushed. Only the `.env.example` templates go up.

---

## Stage 2 — Provision the Hostinger VPS

1. Hostinger → **VPS Hosting** → pick a plan. For this app **KVM 4 (4 vCPU / 16 GB / 200 GB)** is the
   sweet spot; KVM 2 for a pilot, KVM 8 for a large archive. Choose an **India data center** if you need
   Indian data residency.
2. OS: **Ubuntu 24.04 (or 22.04) — plain, no panel**.
3. Set the **root password** (or add your SSH key) in the Hostinger panel.
4. Note the server's **public IP**.

---

## Stage 3 — First login + install Docker

SSH in (replace with your IP):
```bash
ssh root@YOUR_SERVER_IP
```

Create a non-root user, give it sudo + docker, and harden a little:
```bash
adduser deploy            # set a password when prompted
usermod -aG sudo deploy
# (optional) copy your SSH key so you can log in as 'deploy':
rsync --archive --chown=deploy:deploy ~/.ssh /home/deploy 2>/dev/null || true
```

Install Docker Engine + Compose plugin (official script), and a firewall:
```bash
curl -fsSL https://get.docker.com | sh
usermod -aG docker deploy
apt-get update && apt-get install -y ufw git
ufw allow OpenSSH && ufw allow 80 && ufw allow 443 && ufw --force enable
```

Log out and back in **as `deploy`** so docker group membership applies:
```bash
exit
ssh deploy@YOUR_SERVER_IP
docker run --rm hello-world   # should print "Hello from Docker!"
```

---

## Stage 4 — Point your domain at the server (DNS)

In your domain registrar's DNS settings, add an **A record**:

| Type | Name | Value |
| --- | --- | --- |
| A | `tenderdocs` (or `@` for the root) | `YOUR_SERVER_IP` |

Wait for it to resolve (usually minutes):
```bash
dig +short tenderdocs.yourcompany.com   # should print YOUR_SERVER_IP
```
HTTPS is automatic once DNS is live — Caddy fetches a Let's Encrypt cert for the domain.

---

## Stage 5 — Get the app + secrets onto the server

Clone your repo (use the token for a private repo):
```bash
cd ~
git clone https://github.com/<you>/tenderdocs.git
cd tenderdocs/deploy
```

Create the production env. Easiest: securely copy the `deploy/.env` you already generated.
**From your laptop** (new terminal):
```bash
scp deploy/.env deploy@YOUR_SERVER_IP:~/tenderdocs/deploy/.env
```
Then **on the server**, edit the three not-yet-filled values:
```bash
cd ~/tenderdocs/deploy
nano .env
#   DOMAIN=tenderdocs.yourcompany.com
#   PUBLIC_URL=https://tenderdocs.yourcompany.com
#   GOOGLE_CLIENT_ID / GOOGLE_CLIENT_SECRET / GOOGLE_REDIRECT_URI / GOOGLE_DRIVE_FOLDER_ID
#   VITE_GOOGLE_CLIENT_ID   (same value as GOOGLE_CLIENT_ID — required; baked into the SPA build)
#   SEED_ADMIN_EMAIL        (your real Google account — becomes the Admin on first boot)
chmod 600 .env
```

**Google sign-in is the only way to log in.** `VITE_GOOGLE_CLIENT_ID` in `.env` is passed into the
frontend image automatically at build time — there is no separate frontend env file to create.

In the **Google Cloud Console** OAuth client, add:
- Authorized JavaScript origin: `https://tenderdocs.yourcompany.com`
- Authorized redirect URI: `https://tenderdocs.yourcompany.com/api/google-drive/callback`

---

## Stage 6 — Launch

```bash
cd ~/tenderdocs/deploy
docker compose -f docker-compose.server.yml up -d --build
```
First build takes a few minutes (it installs ghostscript + qpdf into the API image).

Verify:
```bash
docker compose -f docker-compose.server.yml ps      # all services Up
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8080/api/documents   # 401 = API alive
```
Then open `https://tenderdocs.yourcompany.com` in a browser — valid padlock, app loads.

---

## Stage 7 — First sign-in as the Admin (CRITICAL)

`.env` ships with `SEED_ENABLED=true` and `SEED_ADMIN_EMAIL`. The **first** boot, against an empty
database, seeds the permission catalog and exactly **one** user — that email — as **Admin**. There
are no passwords and no demo data; sign-in is Google-only, and unknown Google accounts are rejected.

1. Confirm `SEED_ADMIN_EMAIL` in `.env` is the real Google account you'll administer with, and that
   the OAuth client allows it (JavaScript origin set, and the email added under **Test users** unless
   the app is published — see `PRE-DEPLOYMENT-CHECKLIST.md` §3).
2. Open `https://tenderdocs.yourcompany.com` and **Sign in with Google** as that account → you land
   as Admin and see the **Administration** section in the sidebar.
3. Go to **Administration → Users** to create the rest of your team (Uploader / Approver / Viewer),
   and **Administration → Project Access** to scope who sees which projects. Add each new user's email
   to the Google OAuth **Test users** list too (or publish the app) so they can sign in.

The seeder is idempotent — once a user exists it never recreates the admin, so you can leave
`SEED_ENABLED=true`. `/swagger` is disabled in production (`SWAGGER_ENABLED=false`).

(See `PRE-DEPLOYMENT-CHECKLIST.md` §3–§4 for the OAuth + access checklist.)

---

## Stage 8 — (Optional) GitHub Actions auto-deploy on every push

Once the manual deploy works, wire CI so `git push` to `main` redeploys automatically. This
switches the server to the **prebuilt-image** compose (`docker-compose.prod.yml`, pulls from GHCR).

**a) Create an SSH key for the action** (on your laptop):
```bash
ssh-keygen -t ed25519 -C "gh-actions-deploy" -f gh_deploy_key   # no passphrase
```
Put the **public** half on the server:
```bash
ssh-copy-id -i gh_deploy_key.pub deploy@YOUR_SERVER_IP
```

**b) Prepare the server for the prebuilt path:**
```bash
ssh deploy@YOUR_SERVER_IP
cd ~/tenderdocs/deploy
echo "GHCR_REPO=<you>/tenderdocs" >> .env     # used by docker-compose.prod.yml image names
```

**c) Add repo secrets** at GitHub → repo → **Settings → Secrets and variables → Actions → Secrets**:

| Secret | Value |
| --- | --- |
| `VPS_HOST` | `YOUR_SERVER_IP` |
| `VPS_USER` | `deploy` |
| `VPS_SSH_KEY` | contents of the **private** `gh_deploy_key` file |
| `VPS_APP_DIR` | `/home/deploy/tenderdocs/deploy` |

**d) Add repo variables** (same screen → **Variables** tab):

| Variable | Value |
| --- | --- |
| `DEPLOY_ENABLED` | `true` |
| `VITE_GOOGLE_CLIENT_ID` | your client id (so the CI-built SPA shows the Google button) |

**e) Push** — the `deploy` job now SSHes in, pulls the new GHCR images, and restarts:
```bash
git commit --allow-empty -m "trigger deploy" && git push
```
Watch it in the repo's **Actions** tab.

> Don't want CI deploys? Skip Stage 8 entirely. To update manually you just:
> `cd ~/tenderdocs && git pull && cd deploy && docker compose -f docker-compose.server.yml up -d --build`

---

## Stage 9 — Backups (set up before real data lands)

Two things to back up: the **database** and the **uploaded files** volume.

```bash
# Nightly DB dump + storage archive into ~/backups, kept 14 days. Run `crontab -e` and add:
0 2 * * * cd /home/deploy/tenderdocs/deploy && \
  docker compose -f docker-compose.server.yml exec -T postgres pg_dump -U tenderdocs tenderdocs | gzip > /home/deploy/backups/db-$(date +\%F).sql.gz && \
  docker run --rm -v deploy_storage:/s -v /home/deploy/backups:/b alpine tar czf /b/storage-$(date +\%F).tar.gz -C /s . && \
  find /home/deploy/backups -mtime +14 -delete
```
Then copy `~/backups` off-box (provider snapshot, or `rclone` to object storage). **Test a restore once.**

---

## Stage 10 — Day-2 operations

```bash
cd ~/tenderdocs/deploy
docker compose -f docker-compose.server.yml ps                 # status
docker compose -f docker-compose.server.yml logs -f api        # tail API logs
docker compose -f docker-compose.server.yml restart api        # restart a service
docker compose -f docker-compose.server.yml down               # stop all (data persists in volumes)
```
**Scale up** = resize the VPS in the Hostinger panel (minutes), then `docker compose ... up -d`.

---

## Troubleshooting

| Symptom | Check |
| --- | --- |
| HTTPS not issued | DNS A record resolves to the server? Ports 80+443 open (`ufw status`)? `docker compose logs caddy` |
| Upload 500 | `docker compose logs api` — if `invalid_client`, the Google secret is wrong (see PRE-DEPLOYMENT-CHECKLIST §3) |
| Google button missing | `frontend/.env` `VITE_GOOGLE_CLIENT_ID` set before build? (Stage 5) |
| Google button missing on login | set `VITE_GOOGLE_CLIENT_ID` in `.env` (= `GOOGLE_CLIENT_ID`), then `up -d --build` |
| "This account isn't authorized" | the email isn't a provisioned user — add it in Administration → Users (and Google Test users) |
| Actions deploy fails | secret `VPS_SSH_KEY` is the **private** key; `VPS_APP_DIR` correct; `DEPLOY_ENABLED=true` |
