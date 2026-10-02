# StoreKit 2 Migration ADR

**Status:** Accepted (planned)  
**Date:** 2026-10-02  
**Related:** #376

## Context

`StoreKitBillingService` uses **StoreKit 1** (`SKPaymentQueue`, `SKProductsRequest`) with `CA1422` suppressions. StoreKit 1 remains functional on current iOS/macOS, but Apple steers new work toward StoreKit 2 (`Product`, `Transaction`, `AppStore`).

.NET MAUI / Mac Catalyst bindings for StoreKit 2 APIs are incomplete or awkward versus Swift; the existing SK1 path matches Microsoft MAUI samples.

## Decision

1. **Now:** Keep StoreKit 1; ensure the payment observer is removed on `Dispose` and `InitializeAsync` is idempotent (#376).
2. **Next:** Migrate to StoreKit 2 when:
   - Product/Transaction APIs are usable from C# without a custom Swift bridge, **or**
   - We add a thin Swift PM / ObjC bridge similar to CloudKitSyncBridge.

## Consequences

- Short-term: continued `CA1422` suppressions scoped to the billing service file.
- Migration work is tracked under #376; not a ship-blocker for current Store builds.
