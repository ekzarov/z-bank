import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, switchMap } from 'rxjs';
import { Account, AccountMetadata, AccountPage, CashTransaction, CashTransactionDirection } from './account.model';

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

  private withCsrf<T>(request: () => Observable<T>): Observable<T> {
    return this.http.get('api/session/csrf').pipe(switchMap(request));
  }
}
