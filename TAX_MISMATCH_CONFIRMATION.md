# Tax Mismatch Confirmation Feature

## Overview
This feature allows users to confirm that a tax calculation discrepancy is acceptable, preventing the warning icon from being displayed again.

## User Experience

### Before Confirmation
When a transaction has a tax amount that doesn't match the expected calculation:
- The "Steuerfehler" column displays a ⚠️ warning icon
- The cell has a red background (#ffebee) with dark red text (#c62828)
- The cursor changes to a pointer when hovering over the icon

### Confirming a Mismatch
1. Click on the ⚠️ warning icon in the "Steuerfehler" column
2. The transaction is immediately updated on the server
3. The grid refreshes automatically
4. The warning icon disappears

### After Confirmation
- The "Steuerfehler" column shows no icon (empty)
- The `IsTaxMismatchConfirmed` flag is set to `true` in the database
- Future loads of this transaction will not show the warning

## Technical Implementation

### Database Schema
- **Field**: `IsTaxMismatchConfirmed` (boolean, default: false)
- **Table**: Transactions
- **Migration**: `20260124130552_AddTaxMismatchConfirmed`

### Backend Logic
The `CalculateTaxMismatch` method in `TransactionMapper.cs` now:
1. First checks if `IsTaxMismatchConfirmed` is true
2. If confirmed, returns false (no mismatch shown)
3. Otherwise, performs the standard tax calculation validation

### Frontend Behavior
The `onCellClicked` handler:
1. Only responds to clicks when `isTaxMismatch` is true
2. Sets `isTaxMismatchConfirmed` to true on the transaction object
3. Sends a POST request to `/api/transactions/updateTransaction`
4. Refreshes the grid to display the updated state

## Tax Calculation Formula
The expected tax amount is calculated using:
```
expectedTaxAmount = grossAmount * taxRate / (100 + taxRate)
```

A tolerance of 0.02 is allowed for rounding differences.

## API Endpoints
- **Update Transaction**: `POST /api/transactions/updateTransaction`
  - Accepts `UpdateTransactionDto` with `IsTaxMismatchConfirmed` field

## Testing
A comprehensive test suite verifies:
- Tax mismatch detection with various amounts and rates
- Confirmed mismatches do not show as errors
- The confirmation flag is preserved in DTOs
- All 209 tests pass successfully
