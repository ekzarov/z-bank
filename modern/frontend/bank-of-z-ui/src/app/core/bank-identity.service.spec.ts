import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { BankIdentityService } from './bank-identity.service';

describe('BankIdentityService', () => {
  let service: BankIdentityService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(BankIdentityService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads the validated runtime bank identity', () => {
    service.load();

    const request = http.expectOne('api/configuration/bank');
    expect(request.request.method).toBe('GET');
    request.flush({ displayName: 'Migration Demo Bank', sortCode: '654321' });

    expect(service.displayName()).toBe('Migration Demo Bank');
    expect(service.sortCode()).toBe('654321');
  });

  it('keeps safe defaults when runtime configuration is unavailable', () => {
    service.load();
    http.expectOne('api/configuration/bank').flush({}, { status: 503, statusText: 'Unavailable' });

    expect(service.displayName()).toBe('Bank of Z');
    expect(service.sortCode()).toBe('100000');
  });
});
