# ShopRecorder 🛍️

A simple, modern **Shop / POS Sales Recorder** for small shops in Accra — built as a single monolithic **.NET 8 Blazor Web App (Interactive Server)** with **SQLite** (sqlite-net-pcl). No separate API, one project, ready to deploy to **Azure or any .NET host — no Docker needed**.

## ✨ Features

| Page | What it does |
|---|---|
| `/` **Dashboard** | Today's total sales (GHS), number of sales today, low-stock alerts (red badge) and the recent 5 sales |
| `/products` **Products** | Add / edit / delete / search products, low-stock rows highlighted red, low-stock threshold per product (default 5) |
| `/pos` **Sell (POS)** | Searchable product tiles, cart with quantity +/−, total, customer payment, change calculation, stock validation, "Complete sale" reduces stock and saves the sale |
| `/sales` **Sales History** | All sales with paid/change/total and a details modal per sale |
| `/receipt/{saleId}` **Receipt** | Realistic 58mm thermal receipt (monospace, dashed rules, CSS barcode) with **Print** (`window.print()`) and **Download PDF** (jsPDF) |

Extra touches: seed sample products on first run, sample CSV-free DB file auto-created if missing, health endpoint at `/health`, timezone-aware "today" (default `Africa/Accra`) so daily totals are right even on UTC servers.

## 🧱 Tech stack

- .NET 8 Blazor Web App — global `InteractiveServer` render mode
- SQLite via `sqlite-net-pcl` (+ `SQLitePCLRaw.bundle_green` natives)
- Bootstrap 5 + Bootstrap Icons + Inter font (CDN)
- jsPDF + html2canvas (CDN) for receipt PDF export

## 🚀 Run locally

```bash
cd ShopRecorder
dotnet run
```

Open `http://localhost:5000` (or the URL printed in the console). The SQLite database `shoprecorder.db` is created automatically with a few sample products.

> **Requirements:** the **.NET 8 SDK** (pinned by `global.json`). Check with `dotnet --version` inside the project — it must print `8.0.x`. Blazor interactivity relies on `_framework/blazor.web.js`, which the .NET 8 runtime serves automatically; the app references it in `Components/App.razor`. A newer SDK alone is not enough: it ships .NET 10 web assets, which cannot serve the client script to a net8 app.

## ⚙️ Configuration (environment variables — all optional)

| Variable | Default | Purpose |
|---|---|---|
| `PORT` | — | PaaS port binding (Azure/Render set this automatically) |
| `DATA_DIR` | app folder | Where `shoprecorder.db` is stored (mount a disk here for persistence) |
| `SHOP_NAME` | `MY SHOP` | Shop name printed on the receipt |
| `APP_TIMEZONE` | `Africa/Accra` | Timezone used for "today" totals and receipt times |

## ☁️ Deploy to Azure App Service (native .NET 8 — no Docker)

**Quickest — Azure CLI:**

```bash
cd ShopRecorder
az login
az webapp up --name shoprecorder-<yourname> --runtime "DOTNETCORE:8.0" --os-type Linux --sku F1
```

> If your CLI rejects that runtime string, run `az webapp list-runtimes --os linux` and use the value it prints for .NET 8 (some CLI versions call it `DOTNET:8.0`).

**Zip deploy to an existing App Service:**

```bash
dotnet publish -c Release -o ./publish
cd publish && zip -r ../app.zip . && cd ..
az webapp deploy --resource-group <resource-group> --name <app-name> --src-path app.zip --type zip
```

**GitHub Actions (no container):** App Service → **Deployment Center → GitHub** generates a workflow that runs `dotnet publish` and deploys the build on every push.

Then in **Configuration → Application settings** add:

| Setting | Value |
|---|---|
| `SHOP_NAME` | your shop's name (printed on receipts) |
| `APP_TIMEZONE` | `Africa/Accra` |
| `DATA_DIR` | *optional* — folder to keep `shoprecorder.db` in |

On App Service the database is created in the app folder under `/home`, which is writable and persistent. Health check path: `/health`.

## 🌍 Other hosts (no Docker)

Render's native runtimes don't include .NET, so Render would require Docker — which this project deliberately avoids. Hosts that run .NET 8 natively instead:

- **Azure App Service** — free F1 tier for testing (steps above)
- **MonsterASP.NET / Somee / SmarterASP.NET** — publish and upload via Web Deploy or FTP (the publish output includes `web.config` for IIS)
- **Any Linux VPS** — `dotnet publish -c Release`, copy the output, then run `dotnet ShopRecorder.dll` (optionally behind nginx, as a systemd service)

## 📁 Project structure

```
ShopRecorder/
├── ShopRecorder.sln            # Solution — open this in Rider / Visual Studio
├── global.json                 # Pins the .NET 8 SDK
├── Properties/launchSettings.json  # Development env, runs on http://localhost:5000
├── Program.cs                  # App bootstrap, DB init, /health
├── Data/
│   ├── Product.cs              # Product model (LowStockThreshold = 5)
│   ├── Sale.cs                 # Sale model (ItemsJson) + SaleItem.cs
│   ├── SQLiteService.cs        # All data access, transactional stock check
│   ├── ShopClock.cs            # Accra-aware "today" helper
│   └── Format.cs               # GHS/date formatting, shop name
├── Components/
│   ├── App.razor / Routes.razor / _Imports.razor
│   ├── Layout/MainLayout.razor # Dark minimal sidebar + mobile topbar
│   └── Pages/                  # Home, Products, Pos, Sales, Receipt
└── wwwroot/
    ├── css/app.css             # Modern minimal design system + print CSS
    ├── js/app.js               # Modals, toasts, print, PDF download
    └── favicon.svg
```
