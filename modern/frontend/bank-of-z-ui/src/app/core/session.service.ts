import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { catchError, map, Observable, of, switchMap, tap } from 'rxjs';
import { LoginRequest, Session } from './session.model';

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly http = inject(HttpClient);
  readonly session = signal<Session | null>(null);

  load(): Observable<Session | null> {
    return this.http.get<Session>('api/session').pipe(
      map(session => session.isAuthenticated ? session : null),
      tap(session => this.session.set(session)),
      catchError(() => {
        this.session.set(null);
        return of(null);
      })
    );
  }

  login(request: LoginRequest): Observable<Session> {
    return this.ensureCsrf().pipe(
      switchMap(() => this.http.post<Session>('api/session/login', request)),
      tap(session => this.session.set(session))
    );
  }

  logout(): Observable<void> {
    return this.ensureCsrf().pipe(
      switchMap(() => this.http.post<void>('api/session/logout', {})),
      tap(() => this.session.set(null))
    );
  }

  private ensureCsrf(): Observable<unknown> {
    return this.http.get('api/session/csrf');
  }
}
