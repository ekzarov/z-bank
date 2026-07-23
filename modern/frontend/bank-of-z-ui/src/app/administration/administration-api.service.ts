import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, switchMap } from 'rxjs';
import {
  AdministrationRole,
  AdministrationUser,
  AdministrationUserPage,
  CreateAdministrationUserRequest,
  SecurityAuditFilters,
  SecurityAuditPage
} from './administration.model';

@Injectable({ providedIn: 'root' })
export class AdministrationApiService {
  private readonly http = inject(HttpClient);

  searchUsers(query = '', page = 1, pageSize = 20): Observable<AdministrationUserPage> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (query.trim()) params = params.set('query', query.trim());
    return this.http.get<AdministrationUserPage>('api/administration/users', { params });
  }

  createUser(request: CreateAdministrationUserRequest): Observable<AdministrationUser> {
    return this.withCsrf(() => this.http.post<AdministrationUser>('api/administration/users', request));
  }

  changeRole(
    id: string,
    role: AdministrationRole,
    customerId: string | null,
    version: string
  ): Observable<AdministrationUser> {
    return this.withCsrf(() => this.http.put<AdministrationUser>(
      `api/administration/users/${encodeURIComponent(id)}/role`,
      { role, customerId, version }
    ));
  }

  changeLockout(id: string, locked: boolean, version: string): Observable<AdministrationUser> {
    return this.withCsrf(() => this.http.put<AdministrationUser>(
      `api/administration/users/${encodeURIComponent(id)}/lockout`,
      { locked, version }
    ));
  }

  searchSecurityAudit(filters: SecurityAuditFilters, page = 1, pageSize = 20): Observable<SecurityAuditPage> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (filters.eventName.trim()) params = params.set('eventName', filters.eventName.trim());
    if (filters.actorOrSubject.trim()) params = params.set('actorOrSubject', filters.actorOrSubject.trim());
    if (filters.succeeded) params = params.set('succeeded', filters.succeeded);
    if (filters.from) params = params.set('from', new Date(filters.from).toISOString());
    if (filters.to) params = params.set('to', new Date(filters.to).toISOString());
    return this.http.get<SecurityAuditPage>('api/administration/security-audit', { params });
  }

  private withCsrf<T>(request: () => Observable<T>): Observable<T> {
    return this.http.get('api/session/csrf').pipe(switchMap(request));
  }
}
