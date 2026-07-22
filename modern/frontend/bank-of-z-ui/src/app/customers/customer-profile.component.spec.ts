import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { CustomerApiService } from './customer-api.service';
import { CustomerProfileComponent } from './customer-profile.component';

describe('CustomerProfileComponent', () => {
  it('shows the profile returned by the self endpoint', () => {
    TestBed.configureTestingModule({
      imports: [CustomerProfileComponent],
      providers: [{ provide: CustomerApiService, useValue: { me: () => of(customer) } }]
    });
    const fixture = TestBed.createComponent(CustomerProfileComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Jamie Customer');
    expect(fixture.nativeElement.textContent).toContain('1000000001');
  });

  it('shows a safe unavailable state when self lookup fails', () => {
    TestBed.configureTestingModule({
      imports: [CustomerProfileComponent],
      providers: [{ provide: CustomerApiService, useValue: { me: () => throwError(() => new Error('failed')) } }]
    });
    const fixture = TestBed.createComponent(CustomerProfileComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('unavailable');
  });
});

const customer = {
  id: '1000000001', sortCode: '100000', status: 'active', creditScore: 720,
  creditReviewDate: '2026-08-12', sourceSystem: 'modern', sourceIdentifier: null,
  createdAt: '2026-07-22T00:00:00Z', updatedAt: '2026-07-22T00:00:00Z', version: 'version',
  details: {
    title: 'Ms', firstName: 'Jamie', lastName: 'Customer', dateOfBirth: '1990-05-12',
    addressLine1: '1 Test Street', addressLine2: null, city: 'London', region: null,
    postalCode: 'EC1A 1AA', countryCode: 'GB', email: 'customer@example.test', phone: null
  }
} as const;
