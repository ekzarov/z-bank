export type AccountType = 'isa' | 'current' | 'loan' | 'saving' | 'mortgage';
export type AccountStatus = 'active' | 'closed';

export interface Account {
  id: string;
  customerId: string;
  sortCode: string;
  type: AccountType;
  interestRate: number;
  overdraftLimit: number;
  currency: string;
  actualBalance: number;
  availableBalance: number;
  openedOn: string;
  lastStatementOn: string;
  nextStatementOn: string;
  status: AccountStatus;
  sourceSystem: string;
  sourceIdentifier: string | null;
  rawSourceType: string | null;
  version: string;
}

export interface AccountPage {
  items: Account[];
  page: number;
  pageSize: number;
  total: number;
}

export interface AccountMetadata {
  type: AccountType;
  interestRate: number;
  overdraftLimit: number;
  currency: string;
}

export type CashTransactionDirection = 'deposit' | 'withdrawal';

export interface CashTransaction {
  reference: string;
  accountId: string;
  direction: CashTransactionDirection;
  amount: number;
  currency: string;
  actualBalance: number;
  availableBalance: number;
  sourceSystem: string;
  bookedAt: string;
}
