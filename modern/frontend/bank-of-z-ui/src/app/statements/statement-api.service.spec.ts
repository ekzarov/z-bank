import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { StatementApiService } from './statement-api.service';

describe('StatementApiService', () => {
  let service: StatementApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(StatementApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('generates an account statement after obtaining csrf', () => {
    service.generate('10000001', 2026, 6).subscribe(statement =>
      expect(statement.generationId).toBe('11111111-1111-1111-1111-111111111111'));

    http.expectOne('api/session/csrf').flush({ token: 'token' });
    const request = http.expectOne('api/accounts/10000001/statements');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ year: 2026, month: 6 });
    request.flush({ generationId: '11111111-1111-1111-1111-111111111111' });
  });

  it('retrieves an immutable statement without csrf', () => {
    service.find('10000001', '11111111-1111-1111-1111-111111111111').subscribe();

    const request = http.expectOne(
      'api/accounts/10000001/statements/11111111-1111-1111-1111-111111111111');
    expect(request.request.method).toBe('GET');
    request.flush({});
  });

  it('limits a retry to the supplied failed accounts', () => {
    service.generateBulk(2026, 6, ['10000002', '10000003']).subscribe();

    http.expectOne('api/session/csrf').flush({ token: 'token' });
    const request = http.expectOne('api/statements/bulk');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      year: 2026,
      month: 6,
      accountIds: ['10000002', '10000003']
    });
    request.flush({ accounts: [] });
  });
});
