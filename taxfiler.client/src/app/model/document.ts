export interface Document {
  id: number;
  name: string;
  total: number;
  subTotal: number;
  taxAmount: number;
  taxRate: number;
  skonto: number;
  invoiceDate: Date;
  invoiceNumber: string;
  parsed: boolean;
}
