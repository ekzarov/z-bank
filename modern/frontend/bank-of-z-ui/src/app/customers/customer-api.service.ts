import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, switchMap } from 'rxjs';
import { CreateCustomerRequest, Customer, CustomerDetails } from './customer.model';

@Injectable({ providedIn: 'root' })
export class CustomerApiService {
  private readonly http = inject(HttpClient);

  search(name: string): Observable<Customer[]> {
    return this.http.get<Customer[]>('api/customers', { params: { name } });
  }

  find(id: string): Observable<Customer> {
    return this.http.get<Customer>(`api/customers/${encodeURIComponent(id)}`);
  }

  me(): Observable<Customer> {
    return this.http.get<Customer>('api/customers/me');
  }

  create(request: CreateCustomerRequest): Observable<Customer> {
    return this.withCsrf(() => this.http.post<Customer>('api/customers', request));
  }

  update(id: string, details: CustomerDetails, version: string): Observable<Customer> {
    return this.withCsrf(() => this.http.put<Customer>(`api/customers/${encodeURIComponent(id)}`, { details, version }));
  }

  retire(id: string, version: string): Observable<void> {
    return this.withCsrf(() => this.http.post<void>(`api/customers/${encodeURIComponent(id)}/retire`, { version }));
  }

  private withCsrf<T>(request: () => Observable<T>): Observable<T> {
    return this.http.get('api/session/csrf').pipe(switchMap(request));
  }
}
