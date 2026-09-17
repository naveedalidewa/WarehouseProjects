# SLICE-LOGIN.md — Login walking skeleton (learn Render + Cloudflare first)

Before building the whole system, build ONE thin end-to-end slice — **login only** — wired from a browser through the API to **Render Postgres**, with the front-ends on **Cloudflare Pages**. It touches every moving part at the smallest scale, so you learn the wiring (and hit the usual gotchas) at low stakes. Once this works, the rest is adding features onto a skeleton you already trust.

**Scope:** login only. No catalog, orders, etc. yet.
**Decisions (settled):** single repo · PostgreSQL on Render · region **US East (Ohio)** · API on Render, admin + storefront on Cloudflare Pages, mobile app via EAS (not hosted).

---

## What you give Claude Code

- The **admin** and **ecommerce** prototypes (static HTML) — as the *visual* reference only. Their login is fake; Claude Code builds a real one.
- `auth-slice-postgres.sql` — the minimal auth tables (admin_users, customers) for this slice.
- Your Render Postgres **connection string** (paste when asked; keep it out of git).

---

## The prompt

> **GOAL:** Build ONE thin end-to-end vertical slice — login only — so I can learn how Render + Cloudflare + the Postgres database fit together before building the full system. Keep scope strictly to login. Do not build catalog, orders, etc. yet.
>
> **CONTEXT**
> - Single GitHub repo, this structure:
>   - `/api` ASP.NET Core (C#) Web API → hosted on Render (Web Service)
>   - `/admin` React + Vite admin portal → Cloudflare Pages
>   - `/ecommerce` React + Vite storefront → Cloudflare Pages
>   - `/db` SQL / schema files
>   - `/docs` handoff docs
> - Database: Render-managed **PostgreSQL** (already created, region US East/Ohio). Use the **Npgsql** EF Core provider. I'll paste the connection string.
> - I'm attaching the admin and ecommerce prototypes (static HTML). Use them ONLY as the visual reference for the login screens — their login is fake; build a real one.
> - Use `/db/auth-slice-postgres.sql` as the auth schema (admin_users, customers). Its password_hash values are placeholders — set real hashes yourself (next point).
>
> **BUILD (login only, end to end)**
> 1. `/api` — ASP.NET Core Web API with:
>    - EF Core + Npgsql connected to PostgreSQL via `ConnectionStrings__Default` (from env var, never hard-coded).
>    - Use the auth tables from `auth-slice-postgres.sql`. Add a **dev-only seeding step** that sets real password hashes for the seeded users using ASP.NET Core's `PasswordHasher` for a demo password `Passw0rd!` — and print the demo credentials on startup.
>    - Endpoints: `POST /api/admin/login` and `POST /api/customer/login` that verify email + password against Postgres and return a **JWT** on success / **401** on failure. Customer login must **reject a customer whose status is not 'Approved'** (return a clear message).
>    - `GET /health` that opens a DB connection and returns 200, so I can confirm the API is really talking to Postgres.
>    - Enable **CORS** for the admin and ecommerce origins. For now allow `http://localhost:5173` and `http://localhost:5174` for local testing; I'll give you the Cloudflare Pages URLs to add.
>    - Include a **Dockerfile** for the API. Read the connection string and JWT signing key from environment variables.
> 2. `/admin` — a real React (Vite) login page styled to match the admin prototype, that calls `POST /api/admin/login`, stores the JWT, shows a simple "signed in" screen on success and an error on failure. API base URL from `VITE_API_URL`.
> 3. `/ecommerce` — same, styled to match the storefront prototype, calling `POST /api/customer/login`. Also show the **not-approved-customer rejection** working.
>
> **DEPLOY + VERIFY (explain each step as you go — this is what I want to learn)**
> Give me exact, numbered steps to:
>   1. Push this to the GitHub repo.
>   2. Create the **Render Web Service** from `/api` (Root Directory = `api`, **same region as my Postgres = Ohio**, Starter plan), set env vars (connection string, JWT key), and confirm `/health` returns 200 against Postgres.
>   3. Deploy `/admin` and `/ecommerce` as **two Cloudflare Pages projects** from the same repo (build root `admin` / `ecommerce`, build `npm run build`, output `dist`), set `VITE_API_URL` to the Render API URL, and update the API's CORS to the Pages URLs.
>   4. Test the full loop: open the Cloudflare-hosted admin site → log in → JWT issued by the API → verified against Render Postgres. Then the storefront login, including the not-approved-customer rejection.
>
> Keep everything minimal and well-commented so I can follow how each piece connects. Decisions already made: single repo, PostgreSQL (Render). For this slice you may create the auth tables directly from `/db/auth-slice-postgres.sql`.

---

## Connection string (Render → Npgsql)

Render gives a URL like `postgresql://USER:PASS@HOST/DBNAME`. Npgsql (the .NET driver) wants key–value form:

```
Host=HOST;Database=DBNAME;Username=USER;Password=PASS;SSL Mode=Require;Trust Server Certificate=true
```

- Use the **Internal Database URL** for the API *running on Render* (same-region, private, free traffic).
- Use the **External Database URL** to load `auth-slice-postgres.sql` from your own PC (psql / DBeaver).

## Demo credentials (after the API's seed step runs)

| Who | Email | Password | Expected |
|---|---|---|---|
| Admin | `admin@stockroom.co` | `Passw0rd!` | logs in |
| Customer (approved) | `mario@mariocafe.com` | `Passw0rd!` | logs in |
| Customer (pending) | `priya@sunrisegrocers.com` | `Passw0rd!` | **rejected** — awaiting approval |

---

## What "done" looks like (verify checklist)

- [ ] `GET /health` on the Render API returns **200** (API ↔ Postgres works).
- [ ] Admin site (Cloudflare `…pages.dev` or your domain) logs in with the admin user → JWT returned.
- [ ] Storefront logs in with the **approved** customer.
- [ ] Storefront **rejects** the **pending** customer with a clear message (approval gate proven).
- [ ] A wrong password returns **401**, not a crash.

---

## Gotchas to expect (this is the learning)

- **CORS is the #1 trip-up.** The front-ends load from Cloudflare's domain but call the API on Render's domain — the browser blocks that unless the API lists those exact origins. If login "does nothing," open the browser dev-tools **Console**; a CORS error says so plainly. Fix = add the Pages URL to the API's allowed origins and redeploy.
- **Same region.** The Render web service must be in **Ohio**, matching the Postgres, or they can't use the fast private network.
- **You don't need your domain to test.** Render's `…onrender.com` URL and Cloudflare's `…pages.dev` URL are enough to prove the loop. Add the custom domain (`api.` / `admin.` / `shop.`) later.
- **Env vars, not hard-coding.** The connection string and JWT key live in Render's environment settings and (for the front-ends) `VITE_API_URL` in Cloudflare Pages — never committed to git.
- **Free Postgres vs Starter.** Use the **Starter (~$7/mo)** Postgres; the free one expires and isn't for real use.

---

## Then what

Once this slice is green and you understand the wiring, the full build proceeds exactly as `development-kickoff-plan.md` lays out — Catalog next, then ordering, etc. — on the same Render + Cloudflare setup. And the full **PostgreSQL** port of `warehouse-db.sql` replaces this tiny auth schema when you're ready (ask me to generate it).
