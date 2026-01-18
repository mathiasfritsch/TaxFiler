export interface Transaction {
  id: number;
  netAmount: number;
  grossAmount: number;
  senderReceiver: string;
  transactionNote: string;
  documentName: string;
  taxAmount: number;
  transactionDateTime: Date;
  isSalesTaxRelevant: boolean;
  isIncomeTaxRelevant: boolean;
  documentId: number;
  accountId: number;
  accountName: string;
  isTaxMismatch: boolean;
}
