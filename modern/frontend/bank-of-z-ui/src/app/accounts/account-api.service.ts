import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, switchMap } from 'rxjs';
import {
  Account,
  AccountMetadata,
  AccountPage,
  CashTransaction,
  CashTransactionDirection,
  InternalTransfer,
  TransactionHistoryItem,
  TransactionHistoryPage
} from './account.model';

@Injectable({ providedIn: 'root' })
export class AccountApiService {
  private readonly http = inject(HttpClient);

  list(customerId: string, page = 1, pageSize = 20): Observable<AccountPage> {
    return this.http.get<AccountPage>(`api/customers/${encodeURIComponent(customerId)}/accounts`, {
      params: { page, pageSize }
    });
  }

  find(id: string): Observable<Account> {
    return this.http.get<Account>(`api/accounts/${encodeURIComponent(id)}`);
  }

  create(customerId: string, metadata: AccountMetadata): Observable<Account> {
    return this.withCsrf(() => this.http.post<Account>(
      `api/customers/${encodeURIComponent(customerId)}/accounts`,
      { metadata, sourceSystem: 'modern', sourceIdentifier: null, rawSourceType: null }));
  }

  update(id: string, metadata: AccountMetadata, version: string): Observable<Account> {
    return this.withCsrf(() => this.http.put<Account>(`api/accounts/${encodeURIComponent(id)}`, { metadata, version }));
  }

  close(id: string, version: string): Observable<void> {
    return this.withCsrf(() => this.http.post<void>(`api/accounts/${encodeURIComponent(id)}/close`, { version }));
  }

  bookCash(id: string, direction: CashTransactionDirection, amount: number): Observable<CashTransaction> {
    const endpoint = direction === 'deposit' ? 'deposits' : 'withdrawals';
    return this.withCsrf(() => this.http.post<CashTransaction>(
      `api/accounts/${encodeURIComponent(id)}/${endpoint}`,
      { amount },
      { headers: { 'Idempotency-Key': crypto.randomUUID() } }));
  }

  transfer(sourceAccountId: string, destinationAccountId: string, amount: number): Observable<InternalTransfer> {
    return this.withCsrf(() => this.http.post<InternalTransfer>(
      `api/accounts/${encodeURIComponent(sourceAccountId)}/transfers`,
      { destinationAccountId, amount },
      { headers: { 'Idempotency-Key': crypto.randomUUID() } }));
  }

  history(
    accountId: string,
    from?: string,
    to?: string,
    cursor?: string
  ): Observable<TransactionHistoryPage> {
    const params: Record<string, string | number> = { pageSize: 50 };
    if (from) params['from'] = from;
    if (to) params['to'] = to;
    if (cursor) params['cursor'] = cursor;
    return this.http.get<TransactionHistoryPage>(
      `api/accounts/${encodeURIComponent(accountId)}/transactions`,
      { params });
  }

  transaction(accountId: string, reference: string): Observable<TransactionHistoryItem> {
    return this.http.get<TransactionHistoryItem>(
      `api/accounts/${encodeURIComponent(accountId)}/transactions/${encodeURIComponent(reference)}`);
  }

  private withCsrf<T>(request: () => Observable<T>): Observable<T> {
    return this.http.get('api/session/csrf').pipe(switchMap(request));
  }
}
