# Piggy

A self-hosted web app for tracking where your money goes. Import transactions from your bank via CSV, tag them into virtual accounts (like "Groceries", "House", or "Car"), and see at a glance how much of your balance belongs to each pot — envelope budgeting without touching your actual bank accounts.

## Features

- **Virtual accounts** – divide your real bank balance into named pots with starting balances and colors
- **Account groups** – organize related accounts (e.g. "Savings", "Monthly") with combined balance cards on the dashboard
- **CSV import** – upload transaction exports from your bank; duplicates are detected and skipped
- **Guided tagging** – work through untagged transactions one at a time, with undo and skip support
- **Auto-matching** – match strings on each account suggest the right pot based on the transaction description
- **Splits** – divide a single transaction across multiple accounts, with reusable split templates for recurring expenses
- **Backup & restore** – export and import all data as JSON from the built-in Data page
- **Login via OIDC** with optional TOTP two-factor authentication

## Requirements

- Docker with the Compose plugin
- An OIDC provider for authentication (e.g. Authentik, Keycloak, Pocket ID, Nextcloud)

## Installation

The `docker-compose.yml` file is not tracked in this repo. Create your own with this structure:

```yaml
services:
  piggy:
    build: .
    ports:
      - "5000:8080"
    environment:
      - MongoDb__ConnectionString=mongodb://mongo:27017
      - MongoDb__DatabaseName=piggy
      - Oidc__Authority=https://your-oidc-provider
      - Oidc__ClientId=your-client-id
      - Oidc__ClientSecret=your-client-secret
      # Uncomment to disable 2FA (e.g. if you lose access to your authenticator app):
      # - Disable2FA=true
      # Uncomment when running behind a reverse proxy that terminates TLS:
      # - ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
    depends_on:
      - mongo

  mongo:
    image: mongo:7
    ports:
      - "27017:27017"
    volumes:
      - mongo-data:/data/db

volumes:
  mongo-data:
```

Then start it:

```sh
docker compose up -d --build
```

Piggy is now available at `http://localhost:5000`.

### OIDC setup

Register Piggy as a client with your OIDC provider and set the redirect URI to `https://your-piggy-domain/signin-oidc`. Copy the authority URL, client ID, and client secret into the environment variables above.

## Configuration

All configuration is done through environment variables:

| Variable | Description |
| --- | --- |
| `MongoDb__ConnectionString` | MongoDB connection string |
| `MongoDb__DatabaseName` | Database name (default: `piggy`) |
| `Oidc__Authority` | Base URL of your OIDC provider |
| `Oidc__ClientId` | OIDC client ID |
| `Oidc__ClientSecret` | OIDC client secret |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | Set to `true` when running behind a reverse proxy that terminates TLS |
| `Disable2FA` | Set to `true` to bypass two-factor authentication (recovery only) |

## Getting started

1. Log in through your OIDC provider.
2. Set up two-factor authentication under **Settings → Two-Factor Auth**.
3. Create your virtual accounts under **Accounts** (optionally organize them into **Groups**).
4. Export your transactions from your bank as CSV and upload the file on the **Import** page.
5. Tag your transactions under **Transactions → Tag**.

## Reverse proxy

Piggy works fine behind a reverse proxy (Traefik, nginx, Caddy, …). If the proxy terminates TLS, set `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` so the app generates correct HTTPS redirect URLs for the OIDC flow.

## Two-Factor Authentication

After logging in via OIDC, you'll be prompted for a TOTP code from your authenticator app.

**Setup:** Go to **Settings → Two-Factor Auth**, scan the QR code, and confirm with a code.

**Reset if you lose access:** Add `Disable2FA=true` to the piggy environment, restart the container, reconfigure your authenticator from **Settings → Two-Factor Auth**, then remove the env var and restart.

## Backup and restore

All data lives in MongoDB. The easiest way to back up is the built-in export: **Settings → Data → Export** downloads everything (accounts, transactions, split templates, settings) as a JSON file. Use the Import function on the same page to restore it — note that restoring replaces all existing data.

## Updating

```sh
git pull
docker compose up -d --build
```

## Development

Piggy is an ASP.NET Core 10 (Razor Pages) application backed by MongoDB.

```sh
# Requires a local MongoDB on localhost:27017
dotnet run --project src/Piggy

# Run the test suite
dotnet test tests/Piggy.Tests
```
