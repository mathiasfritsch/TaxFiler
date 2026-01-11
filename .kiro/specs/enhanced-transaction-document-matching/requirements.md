# Requirements Document

## Introduction

The Enhanced Transaction Document Matching feature provides intelligent matching between financial transactions and supporting documents in the TaxFiler system. This feature helps users automatically identify which documents correspond to specific transactions, improving efficiency in tax document organization and reducing manual matching effort.

## Glossary

- **Transaction**: A financial transaction record containing amount, date, counterparty, and reference information
- **Document**: A tax-relevant document (invoice, receipt, etc.) with parsed metadata including amounts, dates, and vendor information
- **Match_Score**: A numerical value (0.0 to 1.0) indicating the confidence level of a transaction-document match
- **Document_Matcher**: The system component responsible for finding and ranking document matches for transactions
- **Matching_Criteria**: The set of rules and algorithms used to determine document-transaction compatibility

## Requirements

### Requirement 1: Core Document Matching

**User Story:** As a tax filer, I want the system to automatically suggest the best matching documents for each transaction, so that I can quickly associate transactions with their supporting documentation.

#### Acceptance Criteria

1. WHEN a transaction is provided to the Document_Matcher, THE system SHALL return a ranked list of matching documents
2. WHEN multiple documents match a transaction, THE system SHALL order them by Match_Score in descending order
3. WHEN no documents meet the minimum matching threshold, THE system SHALL return an empty list
4. THE system SHALL calculate Match_Score based on multiple matching criteria with configurable weights
5. THE system SHALL support matching for both incoming and outgoing transactions

### Requirement 2: Amount-Based Matching

**User Story:** As a tax filer, I want transactions to match documents with similar amounts, so that I can identify the correct supporting documentation based on financial values.

#### Acceptance Criteria

1. WHEN transaction amount matches document total exactly, THE system SHALL assign maximum amount score

### Requirement 3: Date-Based Matching

**User Story:** As a tax filer, I want transactions to match documents with nearby dates, so that I can identify documents that correspond to transactions occurring around the same time period.

#### Acceptance Criteria

1. WHEN transaction date matches document invoice date exactly, THE system SHALL assign maximum date score
2. WHEN transaction date is within 7 days of document invoice date, THE system SHALL assign high date score
3. WHEN transaction date is within 30 days of document invoice date, THE system SHALL assign medium date score
4. WHEN transaction date is more than 30 days from document invoice date, THE system SHALL assign low date score
5. THE system SHALL use InvoiceDate as primary date field and InvoiceDateFromFolder as fallback
6. THE system SHALL handle cases where document dates are null or missing

### Requirement 4: Vendor/Counterparty Matching

**User Story:** As a tax filer, I want transactions to match documents from the same vendor or counterparty, so that I can associate payments with the correct business entities.

#### Acceptance Criteria

1. WHEN transaction counterparty exactly matches document vendor name, THE system SHALL assign maximum vendor score
2. WHEN transaction counterparty contains document vendor name as substring, THE system SHALL assign high vendor score
3. WHEN document vendor name contains transaction counterparty as substring, THE system SHALL assign medium vendor score
4. WHEN using fuzzy string matching, similarity above 80% SHALL assign medium vendor score
5. THE system SHALL handle cases where vendor names have different formatting or abbreviations
6. THE system SHALL use both Counterparty and SenderReceiver fields from transactions

### Requirement 5: Reference Number Matching

**User Story:** As a tax filer, I want transactions to match documents with matching reference numbers, so that I can identify documents using invoice numbers or transaction references.

#### Acceptance Criteria

1. WHEN transaction reference exactly matches document invoice number, THE system SHALL assign maximum reference score
2. WHEN transaction reference contains document invoice number as substring, THE system SHALL assign high reference score
3. WHEN document invoice number contains transaction reference as substring, THE system SHALL assign medium reference score
4. THE system SHALL ignore case differences in reference matching
5. THE system SHALL handle null or empty reference fields gracefully

### Requirement 6: Composite Scoring Algorithm

**User Story:** As a system administrator, I want the matching algorithm to combine multiple criteria into a single confidence score, so that the most relevant documents are prioritized appropriately.

#### Acceptance Criteria

1. THE system SHALL calculate composite Match_Score using weighted combination of individual criteria scores
2. THE system SHALL use configurable weights for amount (40%), date (25%), vendor (25%), and reference (10%) matching
3. WHEN any individual criterion scores above 0.9, THE system SHALL apply a bonus multiplier to the composite score
4. THE system SHALL normalize the final Match_Score to a range of 0.0 to 1.0
5. THE system SHALL only return matches with composite Match_Score above 0.3 threshold


### Requirement 8: Configuration and Customization

**User Story:** As a system administrator, I want to configure matching criteria weights and thresholds, so that the matching algorithm can be tuned for different use cases and user preferences.

#### Acceptance Criteria

1. THE system SHALL support configurable weights for each matching criterion
2. THE system SHALL support configurable thresholds for minimum match scores
3. THE system SHALL support configurable tolerance levels for amount and date matching
4. THE system SHALL validate configuration values to ensure they remain within acceptable ranges
5. THE system SHALL apply configuration changes without requiring system restart