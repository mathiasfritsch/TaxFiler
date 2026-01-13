import { Document } from './document';

export interface DocumentMatch {
  document: Document;
  matchScore: number;
  scoreBreakdown: MatchScoreBreakdown;
}

/**
 * Detailed breakdown of match score components.
 * All scores are normalized to a range of 0.0 to 1.0, where:
 * - 0.0 represents no match
 * - 1.0 represents a perfect match
 */
export interface MatchScoreBreakdown {
  /** Score based on amount similarity (0.0 to 1.0) */
  amountScore: number;
  
  /** Score based on date proximity (0.0 to 1.0) */
  dateScore: number;
  
  /** Score based on vendor/counterparty matching (0.0 to 1.0) */
  vendorScore: number;
  
  /** Score based on reference number matching (0.0 to 1.0) */
  referenceScore: number;
  
  /** Final composite score after applying weights and bonuses (0.0 to 1.0) */
  compositeScore: number;
}
