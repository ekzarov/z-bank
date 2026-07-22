import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CustomerApiService } from './customer-api.service';
import { Customer } from './customer.model';

describe('CustomerApiService', () => {
  let service: CustomerApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(CustomerApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('searches by normalized name through the target resource', () => {
    service.search('Jamie').subscribe(customers => expect(customers).toHaveLength(1));

    const request = http.expectOne(request => request.url === 'api/customers' && request.params.get('name') === 'Jamie');
    expect(request.request.method).toBe('GET');
    request.flush([customer]);
  });

  it('retrieves only the current customer profile from the self resource', () => {
    service.me().subscribe(result => expect(result.id).toBe('1000000001'));

    http.expectOne('api/customers/me').flush(customer);
  });

  it('gets csrf before a customer mutation', () => {
    service.update(customer.id, customer.details, customer.version).subscribe(result => expect(result.version).toBe('new-version'));

    http.expectOne('api/session/csrf').flush({ token: 'token' });
    const update = http.expectOne(`api/customers/${customer.id}`);
    expect(update.request.method).toBe('PUT');
    expect(update.request.body.version).toBe(customer.version);
    update.flush({ ...customer, version: 'new-version' });
  });
});

const customer: Customer = {
  id: '1000000001',
  sortCode: '100000',
  status: 'active',
  creditScore: 720,
  creditReviewDate: '2026-08-12',
  sourceSystem: 'modern',
  sourceIdentifier: 'test',
  createdAt: '2026-07-22T00:00:00Z',
  updatedAt: '2026-07-22T00:00:00Z',
  version: 'version',
  details: {
    title: 'Ms', firstName: 'Jamie', lastName: 'Customer', dateOfBirth: '1990-05-12',
    addressLine1: '1 Test Street', addressLine2: null, city: 'London', region: null,
    postalCode: 'EC1A 1AA', countryCode: 'GB', email: 'customer@example.test', phone: null
  }
};
