import { Document } from './document';

export interface DocumentMatch {
  document: Document;
  matchScore: number;
  scoreBreakdown: MatchScoreBreakdown;
}

export interface MatchScoreBreakdown {
  amountScore: number;
  dateScore: number;
  vendorScore: number;
  referenceScore: number;
  compositeScore: number;
}
