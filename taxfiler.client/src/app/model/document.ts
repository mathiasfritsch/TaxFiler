export interface Document {
  id: number;
  name: string;
  total: number | null;
  subTotal: number | null;
  taxAmount: number | null;
  taxRate: number | null;
  skonto: number | null;
  invoiceDate: Date;
  invoiceNumber: string;
  parsed: boolean;
  unconnected: boolean;
  invoiceDateFromFolder?: Date;
  /** IDs of the transactions this document is linked to. */
  transactionIds?: number[];
}
