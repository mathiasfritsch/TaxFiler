# Requirements Document

## Introduction

The Skonto-Aware Transaction Matching feature enhances the TaxFiler system's ability to match financial transactions with supporting documents when early payment discounts (Skonto) are applied. Currently, when a Skonto is present, the transaction amount is less than the document amount, causing matching failures. This feature ensures accurate matching by considering Skonto adjustments in the amount comparison logic.

## Glossary

- **Skonto**: An early payment discount offered by vendors, typically 2-3% reduction for payment within a specified period (e.g., 14 days)
- **Transaction**: A financial transaction record with the actual paid amount (after Skonto deduction)
- **Document**: A tax-relevant document (invoice, receipt) containing the original amount and Skonto terms
- **Adjusted_Amount**: The document amount after applying Skonto discount for comparison purposes
- **Amount_Matcher**: The system component responsible for comparing transaction and document amounts
- **Skonto_Calculator**: Component that determines the expected payment amount when Skonto is applied

## Requirements

### Requirement 1: Skonto-Aware Amount Matching

**User Story:** As a tax filer, I want transactions to match documents even when I took advantage of early payment discounts, so that I can automatically associate discounted payments with their original invoices.

#### Acceptance Criteria

1. WHEN a document has Skonto percentage, THE system SHALL calculate the discounted amount and use it for matching
2. WHEN a document has Skonto percentage, THE system SHALL calculate discount as percentage of document total
3. WHEN a document has no Skonto terms, THE system SHALL use standard amount matching logic with full document amount
4. WHEN transaction amount matches Skonto-adjusted amount within tolerance, THE system SHALL assign high amount match score
5. THE system SHALL handle cases where Skonto percentage is null or zero