export type StatementDirection = 'deposit' | 'withdrawal';

export interface StatementTransaction {
  bookedAt: string;
  direction: StatementDirection;
  reference: string;
  description: string;
  amount: number;
}

export interface StatementView {
  generationId: string;
  accountId: string;
  customerId: string;
  year: number;
  month: number;
  periodStartUtc: string;
  periodEndUtc: string;
  statementDate: string;
  generatedAt: string;
  dataAsOf: string;
  customerName: string;
  customerAddress: string;
  customerPhone: string | null;
  sortCode: string;
  accountType: string;
  currency: string;
  interestRate: number;
  overdraftLimit: number;
  openingBalance: number;
  totalCredits: number;
  totalDebits: number;
  closingBalance: number;
  availableBalance: number;
  transactionCount: number;
  transactions: StatementTransaction[];
}

export interface BulkStatementAccountResult {
  accountId: string;
  succeeded: boolean;
  generationId: string | null;
  reused: boolean;
  error: string | null;
}

export interface BulkStatementResult {
  year: number;
  month: number;
  total: number;
  succeeded: number;
  failed: number;
  accounts: BulkStatementAccountResult[];
}
