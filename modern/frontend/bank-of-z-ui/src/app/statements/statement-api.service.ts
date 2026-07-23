import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, switchMap } from 'rxjs';
import { BulkStatementResult, StatementView } from './statement.model';

@Injectable({ providedIn: 'root' })
export class StatementApiService {
  private readonly http = inject(HttpClient);

  generate(accountId: string, year: number, month: number): Observable<StatementView> {
    return this.withCsrf(() => this.http.post<StatementView>(
      `api/accounts/${encodeURIComponent(accountId)}/statements`,
      { year, month }));
  }

  find(accountId: string, generationId: string): Observable<StatementView> {
    return this.http.get<StatementView>(
      `api/accounts/${encodeURIComponent(accountId)}/statements/${encodeURIComponent(generationId)}`);
  }

  generateBulk(year: number, month: number, accountIds?: string[]): Observable<BulkStatementResult> {
    return this.withCsrf(() => this.http.post<BulkStatementResult>(
      'api/statements/bulk',
      { year, month, accountIds: accountIds?.length ? accountIds : null }));
  }

  private withCsrf<T>(request: () => Observable<T>): Observable<T> {
    return this.http.get('api/session/csrf').pipe(switchMap(request));
  }
}
