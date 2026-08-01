# Design: Exclude Split Transactions from Untagged Counts

Date: 2026-08-01

## Problem

When a transaction is marked as split, the original transaction keeps
`VirtualAccountId = null` but gets `SplitFrom` set. As a result, it is still
counted as "untagged" in some places, even though the Tag page no longer shows
it.

## Current Behavior

"Untagged" is defined two ways:

- Tag page / `TransactionRepository.GetUntaggedAsync` excludes split
  transactions: `VirtualAccountId == null && SplitFrom == null`.
- Dashboard count (`Pages/Index.cshtml.cs`) counts `VirtualAccountId == null`
  without excluding splits.
- Transactions list "Untagged" filter (`Pages/Transactions/Index.cshtml.cs`)
  also filters on `VirtualAccountId == null` without excluding splits.

This inconsistency causes split originals to appear in the dashboard's Untagged
count and in the Transactions list Untagged filter.

## Change

Define "untagged" consistently everywhere as:

    VirtualAccountId == null && SplitFrom == null

1. `Pages/Index.cshtml.cs` — dashboard count:
   `UntaggedCount = transactions.Count(t => t.VirtualAccountId == null && t.SplitFrom == null);`

2. `Pages/Transactions/Index.cshtml.cs` — untagged filter:
   `allTransactions.Where(t => t.VirtualAccountId == null && t.SplitFrom == null)`

No change needed to `TransactionRepository.GetUntaggedAsync` — it already
excludes splits.

Split children and overflow transactions already have `VirtualAccountId` set,
so they are unaffected by this change.

## Testing

- Update `IndexModelTests.OnGetAsync_CountsUntaggedTransactions` to include a
  tagged, an untagged, and a split-original transaction; assert only the truly
  untagged one is counted.
- Add a test for `Transactions/IndexModel` verifying the untagged filter
  excludes split originals.
