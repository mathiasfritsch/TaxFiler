export interface Transaction {
  id: number;
  netAmount: number;
  grossAmount: number;
  senderReceiver: string;
  transactionNote: string;
  taxAmount: number;
  transactionDateTime: Date;
  isSalesTaxRelevant: boolean;
  isIncomeTaxRelevant: boolean;
  accountId: number;
  accountName: string;
  isTaxMismatch: boolean;
  isTaxMismatchConfirmed: boolean;

  /** IDs of all documents linked to this transaction, ordered by sequence. */
  documentIds: number[];
  /** Names of all documents linked to this transaction, ordered by sequence. */
  documentNames: string[];
  /** ID of the primary (first) document (backward-compatible). */
  documentId: number | null;
  /** Name of the primary (first) document (backward-compatible). */
  documentName: string | null;
}

/** A link between a transaction and a document. */
export interface TransactionDocumentLink {
  id: number;
  transactionId: number;
  documentId: number;
  document?: DocumentRef;
  sequence: number;
  assignedAt: Date;
  assignedBy?: string;
  notes?: string;
}

interface DocumentRef {
  id: number;
  name: string;
}
