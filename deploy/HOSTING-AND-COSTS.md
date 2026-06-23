# TenderDocs — Hosting Options & 500 GB Cost (per annum)

A practical, cost-focused guide for hosting the **complete app** (API + SPA + PostgreSQL +
HTTPS proxy) on **one server with ~500 GB storage**. All four services run from a single
`docker compose` (`deploy/docker-compose.server.yml`), so any plain Linux VPS/cloud server works.

> **Prices are 2026 list estimates** to verify on the provider's site at purchase time.
> FX used for ₹: **$1 ≈ ₹85**, **€1 ≈ ₹92**. Currency/cycle and promos shift these ±15%.

---

## How much server does this app actually need?

The app is **light on CPU/RAM** and **heavy on disk** — storage is the cost driver, not compute.

- **Compute:** 2–4 vCPU / 8 GB RAM comfortably runs Postgres + API + SPA + Caddy for a team.
- **Storage:** the 500 GB is for documents (+ DB + OS). Thanks to the built-in compression
  (≈50–85% smaller files), **500 GB effectively holds ~1–2.5 TB of original tender documents.**
  You can safely **start at 200–300 GB and grow** the disk later if you want to spend less now.

---

## Annual cost comparison (≈500 GB, ~8 GB RAM)

| Provider / plan | India region? | Storage model | ~Monthly | **~Per annum** |
| --- | --- | --- | --- | --- |
| **Hetzner** CPX (4 vCPU/8 GB) + 500 GB volume | No (DE/FI/US) | €0.044/GB volume | **~€28–35** | **~₹31,000–39,000** (~$360–460) |
| **Contabo** Cloud VPS (8 vCPU/24 GB, ~400–600 GB) | No (closest: Singapore) | Bundled SSD/NVMe | **~$15–40** | **~₹15,000–41,000** |
| **Hostinger** KVM 8 (8 vCPU/32 GB/400 GB NVMe) | **Yes (India DC)** | Bundled NVMe | **~$20–30** | **~₹20,000–31,000** |
| **AWS Lightsail** Mumbai (8 GB inst. + block storage) | **Yes (Mumbai)** | $0.10/GB block | **~$55–65** | **~₹56,000–66,000** (~$660–780) |
| **E2E Networks** (Indian cloud, 8 GB + 500 GB block) | **Yes (Mumbai/Delhi/Chennai)** | ~₹3.5/GB block | **~₹5,000–6,500** | **~₹60,000–78,000** |
| **DigitalOcean** Bangalore (8 GB Droplet + 500 GB vol) | **Yes (BLR1)** | $0.10/GB volume | **~$98** | **~₹1,00,000** (~$1,180) |
| **GoDaddy** VPS (8 GB) | Yes (India DC) | Bundled (≤~400 GB) | **~$30–45** | **~₹30,000–46,000** |

---

## Recommendation — pick by the data-residency question

**The deciding factor for tender documents is: must the data stay in India?** Government/PSU
tenders frequently require Indian data residency; private tender management usually does not.

### Option 1 — Cheapest & most reliable (residency *not* required)
**Hetzner CPX + a 500 GB volume — ~₹31,000–39,000/yr.**
Best price-to-reliability on the market; runs the exact `docker compose` you already use.
Servers are in Germany/Finland/US (≈120–220 ms to India — perfectly fine for a document app).

### Option 2 — Best value *with* India data residency (recommended for Indian tender work)
**Hostinger KVM (India DC) — ~₹20,000–31,000/yr**, or **E2E Networks — ~₹60,000–78,000/yr**
if you need a compliance-grade Indian cloud (E2E is MeitY-empanelled and used for govt workloads).
Hostinger is the cheapest India-resident option but is a managed VPS, not a compliance cloud;
E2E/AWS Mumbai are the choices when a tender mandates Indian-cloud compliance.

### Option 3 — Enterprise / hyperscaler comfort with India residency
**AWS Lightsail Mumbai (~₹56,000–66,000/yr)** or **DigitalOcean Bangalore (~₹1,00,000/yr)** —
pay more for polished dashboards, snapshots, and brand trust the client may already require.

> **Quote-ready summary for the client:** a 500 GB India-resident server runs about
> **₹20,000–31,000/yr (Hostinger)**, **₹56,000–78,000/yr (AWS Mumbai / E2E, compliance-grade)**;
> non-India (cheapest reliable) about **₹31,000–39,000/yr (Hetzner)**. Plus a one-time setup of
> a few hours and a domain (~₹800–1,500/yr).

---

## What's included vs extra

**Included** in the prices above: the server, OS, bundled/added storage, and bandwidth
(all listed providers include generous traffic for a document app).

**Extra (small):**
- **Domain name** — ~₹800–1,500/yr (HTTPS itself is free via Caddy/Let's Encrypt).
- **Backups** — provider snapshots ~₹100–300/mo, or free with `pg_dump` + `rclone` to cheap
  object storage. **Budget for this — it's not optional.**
- **Email** (optional notifications) — a transactional SMTP free tier usually suffices.

---

## Sizing cheat-sheet

| Users / volume | Suggested server | Disk |
| --- | --- | --- |
| Pilot / single team (<10 users) | 2 vCPU / 4 GB | 100–200 GB (grow later) |
| Standard (10–50 users) | 4 vCPU / 8 GB | 300–500 GB |
| Heavy (50+ users, large archive) | 4–8 vCPU / 16 GB | 500 GB–1 TB |

Scale **vertically first** (resize the VPS in minutes); move Postgres to a managed instance
and add API replicas only if you outgrow one box.

---

## Sources (verify current pricing before purchase)
- Hetzner Cloud pricing & volumes — https://www.hetzner.com/cloud/pricing/
- Contabo VPS pricing — https://contabo.com/en/vps-server/
- Hostinger VPS pricing — https://www.hostinger.com/vps-hosting
- AWS Lightsail pricing — https://aws.amazon.com/lightsail/pricing/
- E2E Networks pricing calculator — https://calculator.e2enetworks.com/
- DigitalOcean pricing — https://www.digitalocean.com/pricing
