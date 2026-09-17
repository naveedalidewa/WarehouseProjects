# DEPLOYMENT.md — Render + Cloudflare Pages (login slice, verified working)

This is the actual sequence that got the login slice live, including the two mistakes we hit along the way. Repo: `naveedalidewa/WarehouseProjects` (single repo, `/api`, `/admin`, `/ecommerce`, `/db`, `/docs`).

Live URLs (this deployment):
- API: `https://warehouseprojects.onrender.com`
- Admin: `https://warehouse-admin-e43.pages.dev`
- Storefront: `https://warehouse-storefront.pages.dev`

---

## 0. Before you start

- Push to GitHub first — Render and Cloudflare Pages both deploy from the connected repo, not from your local disk.
- Make sure `/api` has a `.gitignore` with `bin/` and `obj/` in it. Without one, Visual Studio/`dotnet build` output gets committed and bloats the repo (this happened on the first push here — fixed by adding `api/.gitignore` and `git rm -r --cached api/bin api/obj`).
- Have your Render Postgres **Internal Database URL** on hand (Postgres instance page in Render) — not the External one. Internal is for the API service talking to the DB inside Render's network; External is only for connecting from your own machine (psql/DBeaver).

---

## 1. Render Web Service (the API)

**New → Web Service → connect the repo.**

| Setting | Value |
|---|---|
| Root Directory | `api` |
| Dockerfile Path | `./Dockerfile` |
| Region | Same as your Postgres instance (Ohio here) |
| Instance Type | Starter (free tier Postgres/web services aren't for real use) |

**Environment variables:**

| Key | Value |
|---|---|
| `ConnectionStrings__Default` | `Host=HOST;Database=DBNAME;Username=USER;Password=PASS;SSL Mode=Require;Trust Server Certificate=true` (converted from the Internal Database URL) |
| `Jwt__Key` | A random 32+ byte secret (e.g. `node -e "console.log(require('crypto').randomBytes(32).toString('base64'))"`) |
| `ASPNETCORE_ENVIRONMENT` | `Development` |
| `Cors__AllowedOrigins__0` | First front-end origin (added in step 3, after Pages URLs exist) |
| `Cors__AllowedOrigins__1` | Second front-end origin |

**Gotcha #1 — Root Directory vs Dockerfile Path.** If Root Directory is left blank while Dockerfile Path is `./Dockerfile`, the build looks for a Dockerfile at the repo root and fails with `failed to read dockerfile: open Dockerfile: no such file or directory`. Root Directory must be set to `api` so the path resolves inside that folder.

**Gotcha #2 — `ASPNETCORE_ENVIRONMENT` is required, not optional.** `Program.cs` only runs the dev-seeder (`DevSeeder.SeedDemoPasswordsAsync`, which stamps real password hashes onto the seeded rows) `if (app.Environment.IsDevelopment())`. Render defaults to `Production`. Without this env var, `/health` will pass (DB reachable) but every login will 401 because no account has a usable password hash. This is intentional for a throwaway login-slice — revisit if this ever needs a real Production mode.

Deploy, then confirm:
```
curl https://<service>.onrender.com/health
# → {"status":"ok"}
```

---

## 2. Cloudflare Pages (two projects, same repo)

**Workers & Pages → Create → Pages → Connect to Git → select the repo.** Repeat for each front-end:

| | Admin | Storefront |
|---|---|---|
| Root directory | `admin` | `ecommerce` |
| Build command | `npm run build` | `npm run build` |
| Build output directory | `dist` | `dist` |
| Env var | `VITE_API_URL=https://<render-service>.onrender.com` | same |

Each deploy gives you a `*.pages.dev` URL.

---

## 3. Wire CORS to the real front-end URLs

Back on the Render service → **Environment**, set:
- `Cors__AllowedOrigins__0` = admin Pages URL
- `Cors__AllowedOrigins__1` = storefront Pages URL

Saving triggers an auto-redeploy (~1-2 min). This **replaces** the CORS list — the `localhost:5173`/`5174` dev origins stop being allowed against this deployed API once you do this (local dev is unaffected; it just means you can't test the deployed API from localhost anymore without adding those origins back).

**Gotcha #3 — this is the #1 trip-up in general.** If login "does nothing" after deploying a front-end, it's almost always CORS: the browser blocks the request because the API doesn't list that exact origin yet. Check DevTools → Console for a CORS error before assuming anything else is broken.

Verify with the actual origin:
```
curl -i -X OPTIONS https://<service>.onrender.com/api/admin/login \
  -H "Origin: https://<pages-url>" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: content-type"
# look for: access-control-allow-origin: https://<pages-url>
```

---

## 4. Full verify checklist

- [ ] `GET /health` on the Render API → 200.
- [ ] Admin site logs in with `admin@stockroom.co` / `Passw0rd!` → JWT returned, signed-in screen shown.
- [ ] Storefront logs in with `mario@mariocafe.com` / `Passw0rd!` (approved) → succeeds.
- [ ] Storefront rejects `priya@sunrisegrocers.com` / `Passw0rd!` (pending) → clear "awaiting approval" message, not a crash.
- [ ] Wrong password on either → clean 401, not a crash.

All five passed on this deployment.

---

## Redeploying later

- Pushing to `main` auto-redeploys both Cloudflare Pages projects and the Render service (all three are connected to the same GitHub repo).
- Changing an env var on Render also triggers a redeploy on its own — no push needed.
- If you rotate the Postgres instance or move regions, update `ConnectionStrings__Default` on Render and make sure the Render service's region still matches the Postgres region.
