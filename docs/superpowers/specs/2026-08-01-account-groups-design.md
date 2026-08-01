# Piggy - Account Groups

## Overview

Virtual accounts can be assigned to a managed group (e.g. "Savings", "Monthly") so related accounts are tracked together. Each group shows its combined balance as a card on the dashboard. Groups are managed on a dedicated page: create, rename, and delete. Deletion is blocked while any account is still assigned to the group.

## Decisions

- Groups are a **managed list** (separate management screen), not a fixed enum or freeform string.
- Each virtual account belongs to **at most one group** (or none).
- Groups are **optional** — accounts without a group are simply not shown in any group card.
- Group totals are displayed as **summary cards above the account grid** on the dashboard.
- Deleting a group with assigned accounts is **blocked**; the accounts are never deleted or silently unassigned.
- Groups have **name + color** (color used for the card accent, matching account color styling).

## Data Model

### New: AccountGroup

- `Id` (string, ObjectId): MongoDB identifier
- `Name` (string): Display name (e.g. "Savings", "Monthly")
- `Color` (string): Hex color for the card accent, default `#6c757d`
- `CreatedAt` (DateTime): When the group was created
- `UpdatedAt` (DateTime): Last modification time

Mongo element names: `name`, `color`, `createdAt`, `updatedAt`. Collection name: `accountGroups`.

### Changed: VirtualAccount

- `GroupId` (string?, nullable, `[BsonElement("groupId")]`, `[BsonIgnoreIfNull]`): Link to an `AccountGroup`. Null/absent means unassigned.

## Data Layer

### MongoDbContext

- Add `IMongoCollection<AccountGroup> AccountGroups` (`accountGroups`).
- Include `accountGroups` in `ExportAsync` and `ImportAsync` so backups round-trip.

### New: IAccountGroupRepository / AccountGroupRepository

Same shape as `IVirtualAccountRepository`:
- `GetAllAsync()`, `GetByIdAsync(id)`, `CreateAsync(group)`, `UpdateAsync(group)`, `DeleteAsync(id)`

### Changed: IVirtualAccountRepository / VirtualAccountRepository

- Add `CountByGroupAsync(string groupId)` returning the number of accounts assigned to the group. Used to block group deletion.

### DI

- Register `IAccountGroupRepository` → `AccountGroupRepository` in `Program.cs`.

## Pages

### New: /Groups

Follows the Accounts pages pattern (`Pages/Groups/`):

- **Index**: table of groups — color swatch, name, count of assigned accounts, Edit/Delete actions.
  - Delete posts to a `Delete` handler. If `CountByGroupAsync > 0`, return the page with an error (no redirect, no deletion). Otherwise delete and redirect.
- **Create**: form with Name + Color. Validates required Name.
- **Edit**: form with Name + Color, prefilled. Validates required Name.

### Changed: /Accounts

- **Create** and **Edit**: add a "Group" dropdown (`<select>`) listing all groups plus an empty "No group" option. On Edit, the account's current group is preselected.
- **Index**: show the account's group name as a small badge next to each account (resolved from `GroupId`).

### Navigation

- Add a "Groups" link to `_Layout.cshtml` nav bar.

## Dashboard (/)

- `IndexModel` loads groups alongside accounts/transactions.
- Computes each group's total = sum of `CurrentBalance` over accounts whose `GroupId` matches the group. Empty groups show `0.00`.
- New row of group total cards above the "Virtual Accounts" grid: one card per group — group name with color accent and the group total (positive/negative coloring per existing conventions).
- The existing account grid and overall total are unchanged. Unassigned accounts appear only in the account grid.

## Testing

- `AccountGroupRepositoryTests`: create / read / update / delete, mirroring `VirtualAccountRepositoryTests`.
- `VirtualAccountRepositoryTests`: add `CountByGroupAsync` cases (0 and N accounts in a group).
- `IndexModelTests`: group totals — accounts in a group sum into that group's total; unassigned accounts excluded; empty groups total 0.
- `Groups/Index` delete block: handler returns the page with an error when accounts are assigned, and does not delete.

## Verification

- `dotnet build` and `dotnet test` pass.
