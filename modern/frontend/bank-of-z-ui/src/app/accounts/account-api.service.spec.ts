import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AccountApiService } from './account-api.service';

describe('AccountApiService', () => {
  let service: AccountApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AccountApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('requests a complete bounded customer portfolio page', () => {
    service.list('1000000001', 2, 50).subscribe(page => expect(page.total).toBe(51));
    const request = http.expectOne(request => request.url.includes('/accounts'));
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('50');
    request.flush({ items: [], page: 2, pageSize: 50, total: 51 });
  });

  it('gets csrf before account creation', () => {
    service.create('1000000001', { type: 'current', interestRate: 0, overdraftLimit: 500, currency: 'GBP' }).subscribe();
    http.expectOne('api/session/csrf').flush({ token: 'token' });
    const create = http.expectOne('api/customers/1000000001/accounts');
    expect(create.request.method).toBe('POST');
    expect(create.request.body.metadata.currency).toBe('GBP');
    create.flush({});
  });

  it('books a withdrawal with csrf and an idempotency key', () => {
    const reference = '0123456789abcdef0123456789abcdef';
    service.bookCash('10000001', 'withdrawal', 25.5).subscribe(result => expect(result.reference).toBe(reference));
    http.expectOne('api/session/csrf').flush({ token: 'token' });
    const request = http.expectOne('api/accounts/10000001/withdrawals');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ amount: 25.5 });
    expect(request.request.headers.get('Idempotency-Key')).toMatch(/^[0-9a-f-]{36}$/);
    request.flush({ reference });
  });
});
