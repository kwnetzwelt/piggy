# Piggy - Virtual Account Transaction Manager

## Overview

A web application that connects to N26 bank via the Go CLI library (guitmz/n26) and allows users to map transactions to local virtual accounts. The app provides a simple interface for tracking spending across categories like ISP, house, car, etc.

## Architecture

- **Backend**: ASP.NET Core 10.0 Razor Pages
- **Database**: MongoDB
- **Bank Integration**: Go CLI tool (guitmz/n26) executed from the app
- **Deployment**: Docker Compose with single container + MongoDB

## Data Model

### VirtualAccount
- `Id` (ObjectId): MongoDB identifier
- `Name` (string): Display name (e.g., "ISP", "House", "Car")
- `StartingBalance` (decimal): Initial balance when account was created
- `CurrentBalance` (decimal): Computed = StartingBalance + sum of tagged transactions
- `CreatedAt` (DateTime): When the account was created
- `UpdatedAt` (DateTime): Last modification time

### Transaction
- `Id` (ObjectId): MongoDB identifier
- `N26Id` (string): N26 transaction ID
- `Amount` (decimal): Transaction amount (negative = expense, positive = income)
- `Currency` (string): EUR
- `Date` (DateTime): Transaction date
- `Description` (string): Transaction description/merchant name
- `SpaceId` (string): N26 space ID (empty string for main account)
- `SpaceName` (string): N26 space name
- `VirtualAccountId` (ObjectId?, nullable): Link to virtual account (null = untagged)
- `ImportedAt` (DateTime): When this was imported into the system
- `IsProcessed` (bool): Whether this transaction has been tagged

### N26Settings
- `Id` (string): Always "default"
- `Username` (string): N26 email
- `Password` (string): N26 password (encrypted)
- `DeviceToken` (string): UUID for device token
- `LastSyncAt` (DateTime?): Last successful sync time

## Features

### 1. Dashboard (/)
- Summary cards showing each virtual account with its current balance
- Quick stats: total transactions, untagged count, last sync time
- Recent transactions preview (last 10)

### 2. Virtual Accounts Management (/accounts)
- List all virtual accounts with current balance
- Create new account with name and starting balance
- Edit account (update name or starting balance)
- Delete account (with confirmation)

### 3. Transactions (/transactions)
- Table showing all imported transactions
- Filter by: date range, space, tagged/untagged status
- Dropdown in each row to assign virtual account
- Search by description
- Pagination for large datasets

### 4. Import from N26 (/import)
- Button to trigger sync from N26
- Shows progress/status
- Handles authentication (2FA approval required on phone)
- Only imports new transactions (skips duplicates)

### 5. Settings (/settings)
- Configure N26 credentials (username, password, device token)
- Test connection button
- View last sync status

## Docker Setup

### Dockerfile
```dockerfile
# Multi-stage build
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
# Install Go
FROM golang:1.22 AS go-builder
RUN go install github.com/guitmz/n26/cmd/n26@latest

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
# Copy Go binary
COPY --from=go-builder /go/bin/n26 /usr/local/bin/n26
# Copy .NET app
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Piggy.dll"]
```

### docker-compose.yml
```yaml
services:
  piggy:
    build: .
    ports:
      - "5000:8080"
    environment:
      - MongoDb__ConnectionString=mongodb://mongo:27017
      - MongoDb__DatabaseName=piggy
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

## Pages

1. **/** - Dashboard with summary cards
2. **/accounts** - List virtual accounts
3. **/accounts/create** - Create virtual account form
4. **/accounts/{id}/edit** - Edit virtual account form
5. **/transactions** - Transaction list with tagging
6. **/import** - Import from N26
7. **/settings** - N26 credential configuration

## UI Design

- Clean, minimal design
- Bootstrap 5 for styling
- Responsive layout
- Color coding: green for positive, red for negative amounts
- Badges for account status (tagged/untagged)

## N26 Integration Flow

1. User clicks "Import from N26" on /import page
2. App executes: `n26 transactions json -o /tmp/transactions.json`
3. User approves 2FA on N26 phone app (within 60 seconds)
4. App parses JSON output
5. Deduplicates against existing transactions in MongoDB
6. Inserts new transactions
7. Shows import summary

## Error Handling

- MongoDB connection failures: Show error page with retry
- N26 authentication failures: Show clear error with retry option
- N26 API rate limits: Implement exponential backoff
- Invalid credentials: Clear error message on settings page

## Future Considerations (Not in MVP)

- Authentication/login
- Recurring transaction patterns
- Budget tracking per virtual account
- Charts/analytics
- Export to CSV/PDF
