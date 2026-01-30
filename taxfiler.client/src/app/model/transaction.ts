import { Document } from "./document";

export interface Transaction {
  id: number;
  netAmount: number;
  grossAmount: number;
  senderReceiver: string;
  transactionNote: string;
  documentName: string;
  documentNames: string[];
  taxAmount: number;
  transactionDateTime: Date;
  isSalesTaxRelevant: boolean;
  isIncomeTaxRelevant: boolean;
  documentId: number;
  documentIds: number[];
  documents: Document[];
  accountId: number;
  accountName: string;
  isTaxMismatch: boolean;
}
